using System;

namespace LuminThread.Interface;

public interface IPlayLoopStrategy : IDisposable
{
    void AddAction(IPlayLoopItem item);
    
    void AddAction(LuminTaskState state, MoveNext item);
    
    unsafe void AddAction(LuminTaskState state, delegate*<in LuminTaskState, bool> item);
    
    void AddContinuation(Action action);
    
    void RunCore();
}