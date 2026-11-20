using System.Runtime.CompilerServices;

namespace LuminThread.CompilerServices;

internal static class LuminTaskBuilder
{
    internal static StateMachineBox GetRunner<TStateMachine>(ulong handle, ref TStateMachine machine, bool first)
        where TStateMachine : IAsyncStateMachine
    {
        // The producer is the sole owner of its state machine. No consumer can recycle
        // this slot before that producer publishes a terminal outcome.
        var page = SlotStore.Find(handle, out int index)!;
        ref var slot = ref page.Slots[index];
        if (!first) return (StateMachineBox)slot.ProducerState!;
        var runner = StateMachineBox<TStateMachine>.Rent();
        runner.Machine = machine;
        slot.ProducerState = runner;
        return runner;
    }
}
