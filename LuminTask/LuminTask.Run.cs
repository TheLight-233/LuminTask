using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lumin.Threading.Tasks;

public readonly ref partial struct LuminTask
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
    
}