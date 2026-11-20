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
    internal TStateMachine Machine = default!;
    private StateMachineBox() : base(Unsafe.SizeOf<TStateMachine>()) { }

    internal static StateMachineBox<TStateMachine> Rent()
    {
        var runner = LuminTaskIdleCache.TakeRunner<StateMachineBox<TStateMachine>>() ?? new StateMachineBox<TStateMachine>();
        runner.Acquire();
        return runner;
    }

    protected override void Advance() => Machine.MoveNext();
    protected override void Clear() => Machine = default!;
}
