using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Utility;

namespace LuminThread.CompilerServices;

internal abstract class StateMachineBox : SlotProducer, ILuminCacheNode
{
    private int _owners, _published;
    internal readonly Action MoveNextAction;
    public ILuminCacheNode? CacheNext { get; set; }

    protected StateMachineBox()
    {
        MoveNextAction = Resume;
    }

    // The first registration owns its frame and reserves a lease for the callback.
    // Later suspensions reserve their callback before passing the delegate to the awaiter.
    protected void Acquire()
    {
        _published = 0;
        _owners = 2;
    }

    internal void ReserveResume() => Interlocked.Increment(ref _owners);
    internal sealed override void Published() => Volatile.Write(ref _published, 1);

    private void Resume()
    {
        try
        {
            if (Volatile.Read(ref _published) == 0) Advance();
        }
        finally { Exit(); }
    }

    internal void Exit()
    {
        if (Interlocked.Decrement(ref _owners) != 0 || Volatile.Read(ref _published) == 0) return;
        ClearAndReturn();
    }

    protected abstract void Advance();
    protected abstract void ClearAndReturn();
}

internal sealed class StateMachineBox<TStateMachine> : StateMachineBox where TStateMachine : IAsyncStateMachine
{
    private TStateMachine _machine = default!;
    // IL2CPP resolves generic reflection types through its metadata table. Resolve once
    // per state machine, while leaving all idle runner instances in the bounded cache.
    private static readonly Type PoolType = typeof(StateMachineBox<TStateMachine>);
    private static readonly int Charge = (Unsafe.SizeOf<TStateMachine>() + 16 * IntPtr.Size + 7) & ~7;

    internal static StateMachineBox Bind(ulong handle, ref TStateMachine machine, bool first)
    {
        // The producer retains its slot until it publishes a terminal result.
        var page = SlotStore.Find(handle, out int index)!;
        ref var slot = ref page.Slots[index];
        if (!first)
        {
            var active = (StateMachineBox)slot.ProducerState!;
            active.ReserveResume();
            return active;
        }
        var runner = (StateMachineBox<TStateMachine>?)LuminTaskIdleCache.Take(PoolType) ?? new StateMachineBox<TStateMachine>();
        runner.Acquire();
        runner._machine = machine;
        slot.ProducerState = runner;
        return runner;
    }

    protected override void Advance() => _machine.MoveNext();
    protected override void ClearAndReturn()
    {
        _machine = default!;
        LuminTaskIdleCache.Return(this, Charge, PoolType);
    }
}
