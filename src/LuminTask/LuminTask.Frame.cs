using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread;

public readonly partial struct LuminTask
{
    private static readonly MoveNext DelayFrameStepAction = DelayFrameStep;

    /// <summary>Yields to the selected player-loop timing for the requested number of frames.</summary>
    public static LuminTask DelayFrame(int frameCount, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default)
    {
        if (frameCount < 0) throw new ArgumentOutOfRangeException(nameof(frameCount));
        if (cancellationToken.IsCancellationRequested) return FromCanceled(cancellationToken);
        if (frameCount == 0) return CompletedTask();

        var handle = RentSlot();
        ref var item = ref GetRentedSlot(handle, out var cold, out int index);

        // The remaining frame count gets its own unmanaged block; the slot only holds the pointer.
        unsafe
        {
            void* scratch = MemoryHelper.Alloc(sizeof(int));
            UnsafeMemory.Write(scratch, frameCount);
            cold.SetPayload(index, (nint)scratch);
        }

        PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken), DelayFrameStepAction);

        return new LuminTask(handle);
    }

    private static bool DelayFrameStep(in LuminTaskState state)
    {
        ref var item = ref LuminTaskCoreSource.TryResolve(state.Handle, out var cold, out int index);

        // The slot was already consumed and recycled (for example by cancelImmediately): drop the item.
        if (Unsafe.IsNullRef(ref item))
            return false;

        if (state.CancellationToken.IsCancellationRequested)
        {
            LuminTaskCoreSource.TrySetCanceled(state.Handle, state.CancellationToken);
            return false;
        }

        nint scratch = cold.PayloadAt(index);

        // Released already (the slot was consumed): drop the item.
        if (scratch == 0)
            return false;

        unsafe
        {
            ref int remaining = ref Unsafe.AsRef<int>((void*)scratch);

            if (--remaining > 0)
                return true;
        }

        LuminTaskCoreSource.TrySetResult(state.Handle);
        return false;
    }
}