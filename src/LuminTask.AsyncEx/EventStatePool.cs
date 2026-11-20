using LuminThread.Utility;

namespace LuminThread.AsyncEx;

internal interface IEventPoolableState : ILuminCacheNode
{
    int Charge { get; }
    void ResetForReuse();
    void BumpGeneration();
}

internal static class EventStatePool<TState> where TState : class, IEventPoolableState, new()
{
    internal static TState Rent() => (TState?)LuminTaskIdleCache.Take(typeof(TState)) ?? new TState();

    internal static void Return(TState state)
    {
        state.ResetForReuse();
        state.BumpGeneration();
        LuminTaskIdleCache.Return(state, state.Charge);
    }
}
