namespace LuminThread;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public partial struct LuminTask
{
    // Every overload in this file is one of two things: one of the five async workers below, or a
    // non-async forwarder onto one of them. A signature whose state is already an object
    // (Action<object>, Func<object, T>, Func<object, LuminTask<T>>) is served by the same worker with
    // TState = object, and a parameterless delegate is served by a cached adapter over its own
    // instance - so nine state machines disappear without adding a runner per adapter, which is what
    // would grow the AOT instance count instead of shrinking it. The adapters are static readonly
    // fields, so forwarding costs a delegate call, never an allocation.
    private static class RunAdapters
    {
        internal static readonly Action<object> Action = static state => ((Action)state)();
        internal static readonly Func<object, LuminTask> Func = static state => ((Func<LuminTask>)state)();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Run(Action action, bool configureAwait = true)
        => Run<object>(RunAdapters.Action, action, configureAwait);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Run(Action<object> action, object state, bool configureAwait = true)
        => Run<object>(action, state, configureAwait);

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
    public static LuminTask<LuminTask> Run(Func<LuminTask> func, bool configureAwait = true)
        => Run<object, LuminTask>(RunAdapters.Func, func, configureAwait);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> Run<T>(Func<object, T> func, object state, bool configureAwait = true)
        => Run<object, T>(func, state, configureAwait);

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
    public static LuminTask<T> Run<T>(Func<object, LuminTask<T>> func, object state, bool configureAwait = true)
        => Run<object, T>(func, state, configureAwait);

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

    public static LuminTask RunOnThreadPool(Action action, bool configureAwait = true, CancellationToken cancellationToken = default)
        => RunOnThreadPool<object>(RunAdapters.Action, action, configureAwait, cancellationToken);

    public static LuminTask RunOnThreadPool(Action<object> action, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
        => RunOnThreadPool<object>(action, state, configureAwait, cancellationToken);

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

    public static LuminTask<T> RunOnThreadPool<T>(Func<object, T> func, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
        => RunOnThreadPool<object, T>(func, state, configureAwait, cancellationToken);

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

    public static LuminTask<T> RunOnThreadPool<T>(Func<object, LuminTask<T>> func, object state, bool configureAwait = true, CancellationToken cancellationToken = default)
        => RunOnThreadPool<object, T>(func, state, configureAwait, cancellationToken);

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