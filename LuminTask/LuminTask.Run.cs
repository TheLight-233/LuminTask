namespace LuminThread;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public readonly partial struct LuminTask
{

    /// <summary>Run action on the threadPool and return to current SynchronizationContext if configureAwait = true.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask Run(Action action, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            
            await SwitchToThreadPool();
            try
            {
                action();
            }
            finally
            {
                if (current != null)
                {
                    await LuminTask.SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            action();
        }
    }

    /// <summary>Run action on the threadPool and return to current SynchronizationContext if configureAwait = true.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask Run(Action<object> action, object state, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            await SwitchToThreadPool();
            try
            {
                action(state);
            }
            finally
            {
                if (current != null)
                {
                    await LuminTask.SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            action(state);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask Run<TState>(Action<TState> action, TState state, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            await SwitchToThreadPool();
            try
            {
                action(state);
            }
            finally
            {
                if (current != null)
                {
                    await LuminTask.SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            action(state);
        }
    }

    /// <summary>Run action on the threadPool and return to current SynchronizationContext if configureAwait = true.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask<T> Run<T>(Func<T> func, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            await SwitchToThreadPool();
            try
            {
                return func();
            }
            finally
            {
                if (current != null)
                {
                    await SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            return func();
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask<T> Run<T>(Func<LuminTask<T>> func, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            await SwitchToThreadPool();
            try
            {
                return await func();
            }
            finally
            {
                if (current != null)
                {
                    await SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            return await func();
        }
    }

    /// <summary>Run action on the threadPool and return to current SynchronizationContext if configureAwait = true.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask<T> Run<T>(Func<object, T> func, object state, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            await SwitchToThreadPool();
            try
            {
                return func(state);
            }
            finally
            {
                if (current != null)
                {
                    await SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            return func(state);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask<T> Run<TState, T>(Func<TState, T> func, TState state, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            await SwitchToThreadPool();
            try
            {
                return func(state);
            }
            finally
            {
                if (current != null)
                {
                    await SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            return func(state);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask<T> Run<T>(Func<object, LuminTask<T>> func, object state, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            await SwitchToThreadPool();
            try
            {
                return await func(state);
            }
            finally
            {
                if (current != null)
                {
                    await SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            return await func(state);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async LuminTask<T> Run<TState, T>(Func<TState, LuminTask<T>> func, TState state, bool configureAwait = true)
    {
        if (configureAwait)
        {
            var current = SynchronizationContext.Current;
            await SwitchToThreadPool();
            try
            {
                return await func(state);
            }
            finally
            {
                if (current != null)
                {
                    await SwitchToSynchronizationContext(current);
                }
            }
        }
        else
        {
            await SwitchToThreadPool();
            return await func(state);
        }
    }
    
    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async LuminTask RunOnThreadPool(Action action, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                action();
            }
            finally
            {
                await LuminTask.Yield();
            }
        }
        else
        {
            action();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async LuminTask RunOnThreadPool(Action<object> action, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                action(state);
            }
            finally
            {
                await LuminTask.Yield();
            }
        }
        else
        {
            action(state);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
    
    public static async LuminTask RunOnThreadPool<TState>(Action<TState> action, TState state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                action(state);
            }
            finally
            {
                await LuminTask.Yield();
            }
        }
        else
        {
            action(state);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
    

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async LuminTask<T> RunOnThreadPool<T>(Func<T> func, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return func();
            }
            finally
            {
                await LuminTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            return func();
        }
    }
    
    public static async LuminTask<T> RunOnThreadPool<T>(Func<LuminTask<T>> func, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return await func();
            }
            finally
            {
                await LuminTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            return await func();
        }
    }

    /// <summary>Run action on the threadPool and return to main thread if configureAwait = true.</summary>
    public static async LuminTask<T> RunOnThreadPool<T>(Func<object, T> func, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return func(state);
            }
            finally
            {
                await LuminTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            return func(state);
        }
    }
    
    public static async LuminTask<T> RunOnThreadPool<TState, T>(Func<TState, T> func, TState state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return func(state);
            }
            finally
            {
                await LuminTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            return func(state);
        }
    }
    
    public static async LuminTask<T> RunOnThreadPool<T>(Func<object, LuminTask<T>> func, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return await func(state);
            }
            finally
            {
                await LuminTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            return await func(state);
        }
    }
    
    public static async LuminTask<T> RunOnThreadPool<TState, T>(Func<TState, LuminTask<T>> func, TState state, bool configureAwait = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await LuminTask.SwitchToThreadPool();

        cancellationToken.ThrowIfCancellationRequested();

        if (configureAwait)
        {
            try
            {
                return await func(state);
            }
            finally
            {
                await LuminTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            return await func(state);
        }
    }
}