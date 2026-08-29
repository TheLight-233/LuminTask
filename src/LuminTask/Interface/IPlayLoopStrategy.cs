using System;

namespace LuminThread.Interface;

public interface IPlayLoopStrategy : IDisposable
{
    void AddAction(IPlayLoopItem item);

    void AddAction(LuminTaskState state, MoveNext item);

    void AddContinuation(Action action);

    void RunCore();
}

// Function pointers are an optional execution optimization. External runtimes may implement
// this interface when they can safely invoke unmanaged function pointers (Unity IL2CPP does so
// through its player-loop strategy).
public interface IFunctionPointerPlayLoopStrategy
{
    void AddAction(LuminTaskState state, IntPtr functionPointer);
}
