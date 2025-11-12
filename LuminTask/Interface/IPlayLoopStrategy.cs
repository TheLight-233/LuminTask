using System;

namespace LuminThread.Interface;

public interface IPlayLoopStrategy : IDisposable
{
    void AddAction(IPlayLoopItem item);
    
    void AddAction(LuminTaskState state, MoveNext item);
    
    void AddContinuation(Action action);
    
    void RunCore();
}