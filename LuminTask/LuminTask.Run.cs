using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lumin.Threading.Source;

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
}