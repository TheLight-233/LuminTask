using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Lumin.Threading.Interface;
using Lumin.Threading.Source;
using Lumin.Threading.Tasks.Utility;
using Lumin.Threading.Unity;
using Lumin.Threading.Utility;

namespace Lumin.Threading.Tasks;

public readonly ref partial struct LuminTask
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitUntil(Func<bool> condition, PlayerLoopTiming timing = PlayerLoopTiming.DotNet, CancellationToken cancellationToken = default, bool cancelImmediately = false)
    {
        return new LuminTask(WaitUntilPromise.Create(condition, timing, cancellationToken, cancelImmediately, out var token), token);
    }
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitUntil<T>(T state, Func<T, bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.DotNet, CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        return new LuminTask(WaitUntilPromise<T>.Create(state, predicate, timing, cancellationToken, cancelImmediately, out var token), token);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitWhile(Func<bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.DotNet, CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        return new LuminTask(WaitWhilePromise.Create(predicate, timing, cancellationToken, cancelImmediately, out var token), token);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitWhile<T>(T state, Func<T, bool> predicate, PlayerLoopTiming timing = PlayerLoopTiming.DotNet, CancellationToken cancellationToken = default(CancellationToken), bool cancelImmediately = false)
    {
        return new LuminTask(WaitWhilePromise<T>.Create(state, predicate, timing, cancellationToken, cancelImmediately, out var token), token);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WaitUntilCanceled(CancellationToken cancellationToken, PlayerLoopTiming timing = PlayerLoopTiming.DotNet, bool completeImmediately = false)
    {
        return new LuminTask(WaitUntilCanceledPromise.Create(cancellationToken, timing, completeImmediately, out var token), token);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<U> WaitUntilValueChanged<T, U>(
        T target, 
        Func<T, U> monitorFunction, 
        PlayerLoopTiming monitorTiming = PlayerLoopTiming.DotNet, 
        IEqualityComparer<U> equalityComparer = default, 
        CancellationToken cancellationToken = default, 
        bool cancelImmediately = false)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return LuminTask.FromCanceled<U>(cancellationToken);
        }

        if (typeof(T).IsValueType)
        {
            return new LuminTask<U>(WaitUntilValueChangedValuePromise<T, U>.Create(
                target, monitorFunction, equalityComparer, monitorTiming,
                cancellationToken, cancelImmediately, out var token), token);
        }
        else
        {
            var targetRef = Unsafe.As<T, object>(ref target); // 将T视为object
            return new LuminTask<U>(WaitUntilValueChangedRefPromise<object, U>.Create(
                targetRef, 
                obj => monitorFunction(Unsafe.As<object, T>(ref obj)), // 转回T
                equalityComparer, monitorTiming, cancellationToken, cancelImmediately, out var token), token);
        }
    }
    
    sealed class WaitUntilPromise : ILuminTaskSource, Threading.Utility.ITaskPoolNode<WaitUntilPromise>, IPlayLoopItem
    {
        static Threading.Utility.TaskPool<WaitUntilPromise> pool;
        WaitUntilPromise nextNode;
        public ref WaitUntilPromise NextNode => ref nextNode;

        static WaitUntilPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilPromise), static () => pool.Size);
        }

        Func<bool> predicate;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;

        LuminTaskCompletionSourceCore<object> core;

        private WaitUntilPromise() { }

        public static ILuminTaskSource Create(Func<bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitUntilPromise();
            }

            result.predicate = predicate;
            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (WaitUntilPromise)state;
                    promise.core.TrySetCanceled(promise.cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            try
            {
                if (!predicate())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return false;
            }

            core.TrySetResult(null);
            return false;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            predicate = default;
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            cancelImmediately = default;
            return pool.TryPush(this);
        }
    }
    
    sealed class WaitUntilPromise<T> : ILuminTaskSource, IPlayLoopItem, Threading.Utility.ITaskPoolNode<WaitUntilPromise<T>>
    {
        static Threading.Utility.TaskPool<WaitUntilPromise<T>> pool;
        WaitUntilPromise<T> nextNode;
        public ref WaitUntilPromise<T> NextNode => ref nextNode;

        static WaitUntilPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilPromise<T>), () => pool.Size);
        }

        Func<T, bool> predicate;
        T argument;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;

        LuminTaskCompletionSourceCore<object> core;

        private WaitUntilPromise() { }

        public static ILuminTaskSource Create(T argument, Func<T, bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitUntilPromise<T>();
            }

            result.predicate = predicate;
            result.argument = argument;
            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (WaitUntilPromise<T>)state;
                    promise.core.TrySetCanceled(promise.cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            try
            {
                if (!predicate(argument))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return false;
            }

            core.TrySetResult(null);
            return false;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            predicate = default;
            argument = default;
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            cancelImmediately = default;
            return pool.TryPush(this);
        }
    }

    sealed class WaitWhilePromise : ILuminTaskSource, IPlayLoopItem, Threading.Utility.ITaskPoolNode<WaitWhilePromise>
    {
        static Threading.Utility.TaskPool<WaitWhilePromise> pool;
        WaitWhilePromise nextNode;
        public ref WaitWhilePromise NextNode => ref nextNode;

        static WaitWhilePromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitWhilePromise), () => pool.Size);
        }

        Func<bool> predicate;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;

        LuminTaskCompletionSourceCore<object> core;

        private WaitWhilePromise() { }

        public static ILuminTaskSource Create(Func<bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitWhilePromise();
            }

            result.predicate = predicate;
            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (WaitWhilePromise)state;
                    promise.core.TrySetCanceled(promise.cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            try
            {
                if (predicate())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return false;
            }

            core.TrySetResult(null);
            return false;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            predicate = default;
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            cancelImmediately = default;
            return pool.TryPush(this);
        }
    }

    sealed class WaitWhilePromise<T> : ILuminTaskSource, IPlayLoopItem, Threading.Utility.ITaskPoolNode<WaitWhilePromise<T>>
    {
        static Threading.Utility.TaskPool<WaitWhilePromise<T>> pool;
        WaitWhilePromise<T> nextNode;
        public ref WaitWhilePromise<T> NextNode => ref nextNode;

        static WaitWhilePromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitWhilePromise<T>), () => pool.Size);
        }

        Func<T, bool> predicate;
        T argument;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;

        LuminTaskCompletionSourceCore<object> core;

        private WaitWhilePromise() { }

        public static ILuminTaskSource Create(T argument, Func<T, bool> predicate, PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitWhilePromise<T>();
            }

            result.predicate = predicate;
            result.argument = argument;
            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (WaitWhilePromise<T>)state;
                    promise.core.TrySetCanceled(promise.cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            try
            {
                if (predicate(argument))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return false;
            }

            core.TrySetResult(null);
            return false;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            predicate = default;
            argument = default;
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            cancelImmediately = default;
            return pool.TryPush(this);
        }
    }

    sealed class WaitUntilCanceledPromise : ILuminTaskSource, IPlayLoopItem, Threading.Utility.ITaskPoolNode<WaitUntilCanceledPromise>
    {
        static Threading.Utility.TaskPool<WaitUntilCanceledPromise> pool;
        WaitUntilCanceledPromise nextNode;
        public ref WaitUntilCanceledPromise NextNode => ref nextNode;

        static WaitUntilCanceledPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilCanceledPromise), () => pool.Size);
        }

        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;

        LuminTaskCompletionSourceCore<object> core;

        private WaitUntilCanceledPromise() { }

        public static ILuminTaskSource Create(CancellationToken cancellationToken, PlayerLoopTiming timing, bool cancelImmediately, out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitUntilCanceledPromise();
            }

            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (WaitUntilCanceledPromise)state;
                    promise.core.TrySetResult(null);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);

            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public void GetResult(short token)
        {
            try
            {
                core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        public LuminTaskStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public LuminTaskStatus UnsafeGetStatus()
        {
            return core.UnsafeGetStatus();
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetResult(null);
                return false;
            }

            return true;
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            cancelImmediately = default;
            return pool.TryPush(this);
        }
    }

    sealed class WaitUntilValueChangedRefPromise<T, U> : 
        ILuminTaskSource<U>, IPlayLoopItem, 
        Threading.Utility.ITaskPoolNode<WaitUntilValueChangedRefPromise<T, U>> 
        where T : class
    {
        static Threading.Utility.TaskPool<WaitUntilValueChangedRefPromise<T, U>> pool;
        WaitUntilValueChangedRefPromise<T, U> nextNode;
        public ref WaitUntilValueChangedRefPromise<T, U> NextNode => ref nextNode;

        static WaitUntilValueChangedRefPromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedRefPromise<T, U>), () => pool.Size);
        }

        WeakReference<T> targetRef;
        U currentValue;
        Func<T, U> monitorFunction;
        IEqualityComparer<U> equalityComparer;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;

        LuminTaskCompletionSourceCore<U> core;

        private WaitUntilValueChangedRefPromise() { }

        public static ILuminTaskSource<U> Create(
            T target, Func<T, U> monitorFunction, IEqualityComparer<U> equalityComparer, 
            PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, 
            out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitUntilValueChangedRefPromise<T, U>();
            }

            result.targetRef = new WeakReference<T>(target, trackResurrection: false);
            result.monitorFunction = monitorFunction;
            result.currentValue = monitorFunction(target);
            result.equalityComparer = equalityComparer ?? EqualityComparer<U>.Default;
            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (WaitUntilValueChangedRefPromise<T, U>)state;
                    promise.core.TrySetCanceled(promise.cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);
            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public U GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        void ILuminTaskSource.GetResult(short token) => GetResult(token);

        public LuminTaskStatus GetStatus(short token) => core.GetStatus(token);
        public LuminTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested || 
                !targetRef.TryGetTarget(out var target))
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            try
            {
                var nextValue = monitorFunction(target);
                if (equalityComparer.Equals(currentValue, nextValue))
                {
                    return true;
                }
                
                core.TrySetResult(nextValue);
                return false;
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return false;
            }
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            targetRef = null;
            currentValue = default;
            monitorFunction = null;
            equalityComparer = null;
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            cancelImmediately = default;
            return pool.TryPush(this);
        }
    }
    
    sealed class WaitUntilValueChangedValuePromise<T, U> : 
        ILuminTaskSource<U>, IPlayLoopItem, 
        Threading.Utility.ITaskPoolNode<WaitUntilValueChangedValuePromise<T, U>>
    {
        static Threading.Utility.TaskPool<WaitUntilValueChangedValuePromise<T, U>> pool;
        WaitUntilValueChangedValuePromise<T, U> nextNode;
        public ref WaitUntilValueChangedValuePromise<T, U> NextNode => ref nextNode;

        static WaitUntilValueChangedValuePromise()
        {
            TaskPool.RegisterSizeGetter(typeof(WaitUntilValueChangedValuePromise<T, U>), () => pool.Size);
        }

        T target;
        U currentValue;
        Func<T, U> monitorFunction;
        IEqualityComparer<U> equalityComparer;
        CancellationToken cancellationToken;
        CancellationTokenRegistration cancellationTokenRegistration;
        bool cancelImmediately;

        LuminTaskCompletionSourceCore<U> core;

        private WaitUntilValueChangedValuePromise() { }

        public static ILuminTaskSource<U> Create(
            T target, Func<T, U> monitorFunction, IEqualityComparer<U> equalityComparer, 
            PlayerLoopTiming timing, CancellationToken cancellationToken, bool cancelImmediately, 
            out short token)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return AutoResetLuminTaskCompletionSource<U>.CreateFromCanceled(cancellationToken, out token);
            }

            if (!pool.TryPop(out var result))
            {
                result = new WaitUntilValueChangedValuePromise<T, U>();
            }

            result.target = target;
            result.monitorFunction = monitorFunction;
            result.currentValue = monitorFunction(target);
            result.equalityComparer = equalityComparer ?? EqualityComparer<U>.Default;
            result.cancellationToken = cancellationToken;
            result.cancelImmediately = cancelImmediately;

            if (cancelImmediately && cancellationToken.CanBeCanceled)
            {
                result.cancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(state =>
                {
                    var promise = (WaitUntilValueChangedValuePromise<T, U>)state;
                    promise.core.TrySetCanceled(promise.cancellationToken);
                }, result);
            }

            TaskTracker.TrackActiveTask(result, 3);
            PlayerLoopHelper.AddAction(timing, result);

            token = result.core.Version;
            return result;
        }

        public U GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                if (!(cancelImmediately && cancellationToken.IsCancellationRequested))
                {
                    TryReturn();
                }
                else
                {
                    TaskTracker.RemoveTracking(this);
                }
            }
        }

        void ILuminTaskSource.GetResult(short token) => GetResult(token);

        public LuminTaskStatus GetStatus(short token) => core.GetStatus(token);
        public LuminTaskStatus UnsafeGetStatus() => core.UnsafeGetStatus();

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            core.OnCompleted(continuation, state, token);
        }

        public bool MoveNext()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                core.TrySetCanceled(cancellationToken);
                return false;
            }

            try
            {
                var nextValue = monitorFunction(target);
                if (equalityComparer.Equals(currentValue, nextValue))
                {
                    return true;
                }
                
                core.TrySetResult(nextValue);
                return false;
            }
            catch (Exception ex)
            {
                core.TrySetException(ex);
                return false;
            }
        }

        bool TryReturn()
        {
            TaskTracker.RemoveTracking(this);
            core.Reset();
            target = default;
            currentValue = default;
            monitorFunction = null;
            equalityComparer = null;
            cancellationToken = default;
            cancellationTokenRegistration.Dispose();
            cancelImmediately = default;
            return pool.TryPush(this);
        }
    }
}