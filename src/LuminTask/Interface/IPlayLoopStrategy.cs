using System;

namespace LuminThread.Interface;

public interface IPlayLoopStrategy : IDisposable
{
    void AddAction(IPlayLoopItem item);

    void AddAction(LuminTaskState state, MoveNext item);

    void AddContinuation(Action action);

    void RunCore();
}

// Function pointers are an optional execution optimization. Keeping this marker internal
// ensures external runtimes (notably Unity/IL2CPP) use the managed MoveNext fallback unless
// the in-box .NET strategy explicitly opts into calli-based dispatch.
internal interface IFunctionPointerPlayLoopStrategy
{
    void AddAction(LuminTaskState state, IntPtr functionPointer);
}
