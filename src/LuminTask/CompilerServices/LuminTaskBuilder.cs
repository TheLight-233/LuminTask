using System.Runtime.CompilerServices;
using LuminThread.TaskSource;

namespace LuminThread.CompilerServices
{
    // The box half of the async method builder, which now lives on LuminTask / LuminTask<T> itself.
    //
    // Both task types call this single method, so the plumbing that used to be copied into every
    // AsyncLuminTaskMethodBuilder[<T>].AwaitOnCompleted instantiation exists once per TStateMachine
    // instead of once per (T, TAwaiter, TStateMachine). The builder's own state - the box and a
    // synchronously raised exception - is never kept in the task struct: the box is parked in the
    // slot (LuminTaskCoreSource.SetBox) and the exception is recorded in a slot of its own, so a
    // LuminTask stays 8 bytes and LuminTask<T> stays 8 + sizeof(T).
    internal static class LuminTaskBuilder
    {
        // Returns the box that owns this state machine, renting one on the first suspension. A
        // second suspension reads the existing box back through the handle; when the handle is stale
        // (an abandoned task whose slot was already consumed) the box is rented anyway so the method
        // can keep running, and the collector reclaims it instead of the pool.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IStateMachineBox GetOrCreateBox<TStateMachine>(ref LuminTaskHandle handle, ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (!handle.IsNull())
            {
                var existing = LuminTaskCoreSource.BoxAt(handle);

                if (existing != null)
                    return existing;
            }
            else
            {
                handle = LuminTask.RentSlot();
            }

            var box = StateMachineBox<TStateMachine>.Rent();

            LuminTaskCoreSource.SetBox(handle, box);

            // The copy is taken AFTER the handle is written, so the state machine in the box carries
            // the handle (and, on a first suspension, the box itself) for its later resumes.
            box.StateMachine = stateMachine;

            return box;
        }

        // Returns the box of a completed state machine to its pool and releases the slot's reference
        // to it. Must be called before the completion is published: it zeroes the state machine that
        // hosts the builder, so the caller has to keep everything it still needs (handle, result) in
        // locals first.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReturnBox(LuminTaskHandle handle)
        {
            LuminTaskCoreSource.BoxAt(handle)?.Return();
        }
    }
}