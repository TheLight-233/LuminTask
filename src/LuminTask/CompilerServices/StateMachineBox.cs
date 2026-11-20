using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Utility;

namespace LuminThread.CompilerServices;

internal abstract class StateMachineBox : ILuminCacheNode
{
    private readonly int _charge;
    private int _owners, _published;
    internal readonly Action MoveNextAction;
    public ILuminCacheNode? CacheNext { get; set; }

    protected StateMachineBox(int size)
    {
        _charge = (size + 16 * IntPtr.Size + 7) & ~7;
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
    internal void Published() => Volatile.Write(ref _published, 1);

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
        Clear();
        LuminTaskIdleCache.Return(this, _charge);
    }

    protected abstract void Advance();
    protected abstract void Clear();
}

internal sealed class StateMachineBox<TStateMachine> : StateMachineBox where TStateMachine : IAsyncStateMachine
{
    private TStateMachine _machine = default!;
    private StateMachineBox() : base(Unsafe.SizeOf<TStateMachine>()) { }

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
        var runner = LuminTaskIdleCache.TakeRunner<StateMachineBox<TStateMachine>>() ?? new StateMachineBox<TStateMachine>();
        runner.Acquire();
        runner._machine = machine;
        slot.ProducerState = runner;
        return runner;
    }

    protected override void Advance() => _machine.MoveNext();
    protected override void Clear() => _machine = default!;
}
