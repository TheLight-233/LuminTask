using System;

namespace LuminThread.Interface;

// The one dispatch shape every player-loop strategy accepts: the state struct plus the delegate that
// advances it. There is deliberately no object-based item overload and no function-pointer overload -
// the queue only ever carried these two fields, so a single ABI means a single queue, a single active
// list and a single processing loop per strategy instead of three of each.
//
// The delegate is expected to be a cached static delegate (each promise type keeps one), so enqueueing
// costs no allocation.
public delegate bool MoveNext(in LuminTaskState state);

public interface IPlayLoopStrategy : IDisposable
{
    void AddAction(LuminTaskState state, MoveNext moveNext);

    void AddContinuation(Action action);

    void RunCore();
}