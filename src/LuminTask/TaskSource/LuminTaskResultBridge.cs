using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread.TaskSource;

// The only generic seam in the library: writing and reading the result payload.
//
// It needs no vtable slot, no ops table and no function pointer, because both ends already know T
// at compile time - the producer is a generic source (a completion source, a combinator, a
// loop-driven source) and the consumer is LuminTask<T>.GetResult.
//
// Three storage kinds, chosen from per-T compile-time intrinsics so the JIT (and IL2CPP) folds the
// branches away. GC safety is never a mode switch, and no path allocates managed memory on the hot
// path:
//
//   reference type            -> the chunk's Ref array (plain reference assignment)
//   ref-containing value type -> a pooled StateTuple<T> in the chunk's Ref array. The container's
//                                field is a strong-typed T, so assigning the value does NOT box -
//                                that is why a pooled container is required rather than an `object`
//                                slot. The pool is the shared StatePool<T>, so this path adds no new
//                                pool of its own.
//   unmanaged value type      -> a MemoryHelper block sized exactly Unsafe.SizeOf<T>(). Nothing is
//                                reserved in advance, and the block is freed when the slot is
//                                consumed, so there is no per-slot resident footprint.
internal static unsafe class LuminTaskResultBridge
{
    // ---------------------------------------------------------------- write side

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TrySetResult<T>(LuminTaskHandle handle, in T value)
    {
        ref var item = ref LuminTaskCoreSource.TryResolve(handle, out var cold, out int index);

        if (Unsafe.IsNullRef(ref item))
            return false;

        if (!LuminTaskCoreSource.TryBeginCompletion(ref item, handle.Generation()))
            return false;

        Store(cold, index, in value);
        LuminTaskCoreSource.PublishStatus(ref item, LuminTaskStatus.Succeeded);
        LuminTaskCoreSource.Drain(ref item);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Store<T>(LuminTaskCold cold, int index, in T value)
    {
        // RuntimeHelpers.IsReferenceOrContainsReferences and Unsafe.SizeOf are per-T intrinsics,
        // so every branch below folds away at JIT (and IL2CPP) time and no per-T metadata holder
        // is needed.
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (typeof(T).IsValueType)
            {
                // A reference-containing struct cannot be memcpy'd into unmanaged memory (that
                // would be a GC hole) and must not be boxed (that would allocate). StatePool<T>
                // hands out a container whose field is typed `T`: the value is stored into a
                // strong-typed slot without boxing, and the container goes back to the pool when
                // the slot is consumed.
                cold.SetRef(index, StatePool<T>.Create(value));
            }
            else
            {
                cold.SetRef(index, (object?)value);
            }

            return;
        }

        // Unmanaged: allocate exactly what the value needs and remember the pointer. The block is
        // freed by LuminTaskCoreSource.Return when the slot is consumed.
        void* payload = MemoryHelper.Alloc((nuint)Unsafe.SizeOf<T>());
        UnsafeMemory.Write(payload, in value);
        cold.SetPayload(index, (nint)payload);
    }

    // ---------------------------------------------------------------- pending state

    // Loop-driven sources (WaitUntil<TState> and friends) borrow the slot's result storage for
    // their pending state, using the same three storage kinds. Such a task never produces a
    // result, so this cannot collide with the result read path, and Return releases whichever kind
    // was used.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetState<T>(LuminTaskCold cold, int index, in T state) => Store(cold, index, in state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T GetState<T>(LuminTaskCold cold, int index) => Read<T>(cold, index);

    // ---------------------------------------------------------------- read side

    // Reads the result and releases the slot. Only called once the task is known to be drained.
    // Every exit path releases: the exception is captured locally before Return, because Return
    // recycles the slot and a faulted or canceled task must not strand it.
    internal static T Get<T>(LuminTaskHandle handle)
    {
        ref var item = ref LuminTaskCoreSource.Resolve(handle, out var cold, out int index);

        switch (item.Status)
        {
            case LuminTaskStatus.Succeeded:
            {
                T result = Read<T>(cold, index);
                LuminTaskCoreSource.Return(handle);
                return result;
            }

            case LuminTaskStatus.Faulted:
            {
                var exception = (ExceptionDispatchInfo?)cold.ExceptionAt(index);
                LuminTaskCoreSource.Return(handle);
                exception?.Throw();
                LuminTaskExceptionHelper.ThrowInvalidError();
                break;
            }

            case LuminTaskStatus.Canceled:
            {
                CancellationToken cancellationToken = cold.CancellationTokenAt(index);
                LuminTaskCoreSource.Return(handle);
                throw new OperationCanceledException(cancellationToken);
            }

            default:
                LuminTaskExceptionHelper.ThrowNotCompleted();
                break;
        }

        throw new InvalidOperationException("Unreachable code");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Read<T>(LuminTaskCold cold, int index)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var cell = cold.RefAt(index);
            if (LuminTask<T>.IsValueType)
            {
                return ((StateTuple<T>)cell!).Item;
            }
            
            return (T)cell!;
        }
        
        return UnsafeMemory.Read<T>((void*)cold.PayloadAt(index));
    }
}