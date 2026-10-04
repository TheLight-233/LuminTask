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
        //
        // The two branches park the box differently on purpose. A second suspension must validate the
        // handle, because the task value was published after the first suspension and its consumer may
        // have abandoned the task already. The first suspension rented the slot a few instructions
        // earlier and no other thread has the handle yet, so it can address the cold side by id and
        // skip the resolve entirely.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IStateMachineBox GetOrCreateBox<TStateMachine>(ref LuminTaskHandle handle, ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            if (!handle.IsNull())
            {
                var existing = LuminTaskCoreSource.BoxAt(handle);

                if (existing != null)
                    return existing;

                var replacement = StateMachineBox<TStateMachine>.Rent();
                LuminTaskCoreSource.SetBox(handle, replacement);

                // The copy is taken AFTER the handle is written, so the state machine in the box carries
                // the handle (and, on a first suspension, the box itself) for its later resumes.
                replacement.StateMachine = stateMachine;
                return replacement;
            }

            handle = LuminTask.RentSlot();
            var box = StateMachineBox<TStateMachine>.Rent();
            LuminTaskCoreSource.SetBoxForOwnedSlot(handle, box);
            box.StateMachine = stateMachine;
            return box;
        }
    }
}