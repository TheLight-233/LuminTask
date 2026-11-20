using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Utility;

namespace LuminThread.CompilerServices;

internal abstract class StateMachineBox : ILuminCacheNode
{
    private readonly int _charge;
    private int _owners;
    internal readonly Action MoveNextAction;
    public ILuminCacheNode? CacheNext { get; set; }

    protected StateMachineBox(int size)
    {
        _charge = (size + 16 * IntPtr.Size + 7) & ~7;
        MoveNextAction = Resume;
    }

    protected void Acquire() => _owners = 2; // first registration and producer

    private void Resume()
    {
        Interlocked.Increment(ref _owners);
        try { Advance(); }
        finally { Exit(); }
    }

    internal void Exit()
    {
        if (Interlocked.Decrement(ref _owners) != 0) return;
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
