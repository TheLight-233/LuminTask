using System;

namespace Lumin.Threading.Interface;

public interface IPlayLoopStrategy : IDisposable
{
    void AddAction(IPlayLoopItem item);
    
    void AddAction(IntPtr source, MoveNext item);
    
    void AddContinuation(Action action);
    
    void RunCore();
}