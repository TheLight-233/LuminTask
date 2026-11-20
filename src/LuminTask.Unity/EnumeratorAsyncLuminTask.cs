using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using LuminThread;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Unity;
using LuminThread.Interface;
using UnityEngine;

public static class EnumeratorAsyncLuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerator Core(IEnumerator inner, MonoBehaviour coroutineRunner, ulong handle,
        CancellationToken destroyToken, CancellationTokenRegistration registration)
    {
        bool finished = false;
        try
        {
            yield return coroutineRunner.StartCoroutine(inner);
            finished = true;
        }
        finally
        {
            registration.Dispose();
            if (finished && !destroyToken.IsCancellationRequested) LuminTaskSource.SetResult(handle);
            else LuminTaskSource.SetCanceled(handle, destroyToken);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask GetAwaiter<T>(this T enumerator)
        where T : IEnumerator
    {
        var e = (IEnumerator)enumerator;
        
        if (e == null)
            throw new ArgumentNullException(nameof(e));

        return EnumeratorPromise.Create(e, PlayerLoopTiming.Update, CancellationToken.None).GetAwaiter();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask WithCancellation(this IEnumerator enumerator, CancellationToken cancellationToken)
    {
        if (enumerator == null)
            throw new ArgumentNullException(nameof(enumerator));
        
        return EnumeratorPromise.Create(enumerator, PlayerLoopTiming.Update, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask ToLuminTask(this IEnumerator enumerator, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default(CancellationToken))
    {
        if (enumerator == null)
            throw new ArgumentNullException(nameof(enumerator));
        
        return EnumeratorPromise.Create(enumerator, timing, cancellationToken);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask ToLuminTask(this IEnumerator enumerator, MonoBehaviour coroutineRunner)
    {
        if (enumerator == null) throw new ArgumentNullException(nameof(enumerator));
        if (coroutineRunner == null) throw new ArgumentNullException(nameof(coroutineRunner));
        // Unity can discard a native coroutine without resuming its outer iterator on destruction.
#if UNITY_2022_2_OR_NEWER
        var destroyToken = coroutineRunner.destroyCancellationToken;
#else
        var destroyToken = coroutineRunner.GetOnDestroyCancellationToken();
#endif
        var task = LuminTaskSource.Create(out ulong handle);
        CancellationTokenRegistration registration = default;
        try
        {
            registration = UnityLoopWork.RegisterCancellation(handle, destroyToken);
            if (coroutineRunner.StartCoroutine(Core(enumerator, coroutineRunner, handle, destroyToken, registration)) == null)
            {
                registration.Dispose();
                LuminTaskSource.SetCanceled(handle, destroyToken);
            }
        }
        catch
        {
            registration.Dispose();
            LuminTaskSource.SetCanceled(handle, destroyToken);
            task.Forget();
            throw;
        }
        return task;
    }
    
    public struct EnumeratorPromise
    {
        private struct Cursor
        {
            internal int InitialFrame;
            internal IEnumerator Root, Child;
        }

        public static LuminTask Create(IEnumerator innerEnumerator, PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return LuminTask.FromCanceled(cancellationToken);
            var cursor = new Cursor { InitialFrame = -1, Root = innerEnumerator };
            if (!Step(ref cursor, false, out var error))
                return error == null ? LuminTask.CompletedTask() : LuminTask.FromException(error);

            var task = LuminTaskSource.Create(out ulong handle);
            var state = new LuminTaskState(handle, cancellationToken, StateTuple.Create(cursor));
            UnityLoopWork.Add(timing, state, MoveNextAction, Release);
            return task;
        }

        private static readonly MoveNext MoveNextAction = MoveNext;
        private static readonly Action<object> Release = state =>
        {
            var box = (StateTuple<Cursor>)state;
            try { DisposeEnumerators(ref box.Item); }
            finally { box.Dispose(); }
        };

        private static void DisposeEnumerators(ref Cursor cursor)
        {
            try { (cursor.Child as IDisposable)?.Dispose(); }
            finally { (cursor.Root as IDisposable)?.Dispose(); }
        }

        public static bool MoveNext(in LuminTaskState state)
        {
            var box = (StateTuple<Cursor>)state.State;
            bool canceled = state.CancellationToken.IsCancellationRequested;
            if (Step(ref box.Item, canceled, out var error)) return true;
            box.Dispose();
            if (error != null) LuminTaskSource.SetException(state.Handle, error);
            else if (canceled) LuminTaskSource.SetCanceled(state.Handle, state.CancellationToken);
            else LuminTaskSource.SetResult(state.Handle);
            return false;
        }

        private static bool Step(ref Cursor cursor, bool canceled, out Exception error)
        {
            error = null;
            try
            {
                if (!canceled)
                {
                    if (cursor.InitialFrame < 0)
                    {
                        if (PlayerLoopHelper.IsMainThread) cursor.InitialFrame = Time.frameCount;
                    }
                    else if (cursor.InitialFrame == Time.frameCount) return true;
                    if (Advance(ref cursor)) return true;
                }
            }
            catch (Exception ex) { error = ex; }

            try { DisposeEnumerators(ref cursor); }
            catch (Exception cleanup) { error = error == null ? cleanup : new AggregateException(error, cleanup); }
            return false;
        }

        // Flat coroutines use the caller's enumerator directly, including the common yield-null path.
        private static bool Advance(ref Cursor cursor)
        {
            while (true)
            {
                var child = cursor.Child;
                if (child != null)
                {
                    if (child.MoveNext()) return true;
                    cursor.Child = null;
                    (child as IDisposable)?.Dispose();
                }
                if (!cursor.Root.MoveNext()) return false;
                object current = cursor.Root.Current;
                if (current == null) return true;
                cursor.Child = Unwrap(current);
                if (cursor.Child == null) return true;
            }
        }

        private static IEnumerator Unwrap(object current)
        {
            if (current is CustomYieldInstruction custom) return custom;
            if (current is AsyncOperation operation) return UnwrapWaitAsyncOperation(operation);
            if (current is WaitForSeconds seconds) return UnwrapWaitForSeconds(seconds);
            if (current is IEnumerator nested) return ConsumeNested(nested);
            Debug.LogWarning($"yield {current.GetType().Name} is not supported on await IEnumerator or IEnumerator.ToLuminTask(), please use ToLuminTask(MonoBehaviour coroutineRunner) instead.");
            return null;
        }

        private static IEnumerator ConsumeNested(IEnumerator root)
        {
            var nested = new Cursor { Root = root };
            try { while (Advance(ref nested)) yield return null; }
            finally { DisposeEnumerators(ref nested); }
        }

        static IEnumerator UnwrapWaitForSeconds(WaitForSeconds waitForSeconds)
        {
            var second = Unsafe.As<WaitForSeconds, WaitForSecondsView>(ref waitForSeconds).m_Seconds;
            var elapsed = 0.0f;
            while (true)
            {
                yield return null;

                elapsed += Time.deltaTime;
                if (elapsed >= second)
                {
                    break;
                }
            };
        }

        static IEnumerator UnwrapWaitAsyncOperation(AsyncOperation asyncOperation)
        {
            while (!asyncOperation.isDone)
            {
                yield return null;
            }
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private sealed class WaitForSecondsView : YieldInstruction
        {
            internal float m_Seconds;
        }
    }
    
}