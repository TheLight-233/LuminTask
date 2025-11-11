using System;
using System.Runtime.CompilerServices;

namespace LuminThread.AsyncEx;

[Flags]
public enum AsyncLazyFlags
{
    /// <summary>
    /// No special flags. The factory method is executed on a thread pool thread, and does not retry initialization on failures (failures are cached).
    /// </summary>
    None = 0x0,

    /// <summary>
    /// Execute the factory method on the calling thread.
    /// </summary>
    ExecuteOnCallingThread = 0x1,

    /// <summary>
    /// If the factory method fails, then re-run the factory method the next time instead of caching the failed task.
    /// </summary>
    RetryOnFailure = 0x2,
}


public readonly struct AsyncLazy<T>
{
    /// <summary>
    /// The synchronization object protecting <c>_instance</c>.
    /// </summary>
    private readonly object _mutex;

    /// <summary>
    /// The factory method to call.
    /// </summary>
    private readonly Func<LuminTask<T>> _factory;

    /// <summary>
    /// The underlying lazy task.
    /// </summary>
    private readonly Lazy<LuminTask<T>> _instance;
    
    public AsyncLazy(Func<LuminTask<T>> factory, AsyncLazyFlags flags = AsyncLazyFlags.None)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));
        _factory = factory;
        if ((flags & AsyncLazyFlags.RetryOnFailure) == AsyncLazyFlags.RetryOnFailure)
            _factory = RetryOnFailure(_factory);
        if ((flags & AsyncLazyFlags.ExecuteOnCallingThread) != AsyncLazyFlags.ExecuteOnCallingThread)
            _factory = RunOnThreadPool(_factory);

        _mutex = new object();
        _instance = new Lazy<LuminTask<T>>(_factory);
    }
    
    public bool IsStarted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            lock (_mutex)
                return _instance.IsValueCreated;
        }
    }
    
    public LuminTask<T> Task
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            lock (_mutex)
                return _instance.Value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Func<LuminTask<T>> RetryOnFailure(Func<LuminTask<T>> factory)
    {
        var mutex = _mutex;
        var instance = _instance;
        var factoryTask = factory();
        return async () =>
        {
            try
            {
                return await factory();
            }
            catch
            {
                lock (mutex)
                {
                    instance = new Lazy<LuminTask<T>>(factoryTask);
                }
                throw;
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<LuminTask<T>> RunOnThreadPool(Func<LuminTask<T>> factory)
    {
        return () => LuminTask.Run(factory);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskAwaiter<T> GetAwaiter()
    {
        return Task.GetAwaiter();
    }
}