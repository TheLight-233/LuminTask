using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using LuminThread;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;
using UnityEngine;

public static class EnumeratorAsyncLuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerator Core(IEnumerator inner, MonoBehaviour coroutineRunner, ulong handle)
    {
        yield return coroutineRunner.StartCoroutine(inner);
        LuminTaskSource.SetResult(handle);
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
        var task = LuminTaskSource.Create(out ulong handle);
        coroutineRunner.StartCoroutine(Core(enumerator, coroutineRunner, handle));
        return task;
    }
    
    public struct EnumeratorPromise
    {
        struct InfoWarp
        {
            public int InitialFrame;
            public bool LoopRunning;
            public bool CalledGetResult;
            public IEnumerator Enumerator;
            public ulong Handle;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask Create(IEnumerator innerEnumerator, PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled(cancellationToken);
            }
            
            var task = LuminTaskSource.Create(out ulong handle);
            
            IEnumerator enumerator = ConsumeEnumerator(innerEnumerator);
            
            var infos = new InfoWarp()
            {
                CalledGetResult = false,
                LoopRunning = true,
                InitialFrame = -1,
                Enumerator = enumerator,
                Handle = handle
            };

            var state = new LuminTaskState(handle, cancellationToken, StateTuple.Create(infos));
            
            // run immediately.
            if (MoveNext(state))
            {
                
                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);
            }
            
            return task;
        }

        private static readonly MoveNext MoveNextAction = MoveNext;

        public static bool MoveNext(in LuminTaskState state)
        {
            var infos = (StateTuple<InfoWarp>)state.State!;
            IEnumerator innerEnumerator = infos.Item.Enumerator;
            
            if (infos.Item.CalledGetResult)
            {
                infos.Item.LoopRunning = false;
                infos.Dispose();
                return false;
            }

            if (innerEnumerator == null) // invalid status, returned but loop running?
            {
                return false;
            }

            if (state.CancellationToken.IsCancellationRequested)
            {
                infos.Item.LoopRunning = false;
                LuminTaskSource.SetCanceled(infos.Item.Handle, state.CancellationToken);
                infos.Dispose();
                return false;
            }

            if (infos.Item.InitialFrame == -1)
            {
                // Time can not touch in threadpool.
                if (PlayerLoopHelper.IsMainThread)
                {
                    infos.Item.InitialFrame = Time.frameCount;
                }
            }
            else if (infos.Item.InitialFrame == Time.frameCount)
            {
                return true; // already executed in first frame, skip.
            }

            try
            {
                if (innerEnumerator.MoveNext())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                infos.Item.LoopRunning = false;
                LuminTaskSource.SetException(infos.Item.Handle, ex);
                infos.Dispose();
                return false;
            }

            infos.Item.LoopRunning = false;
            LuminTaskSource.SetResult(infos.Item.Handle);
            infos.Dispose();
            return false;
        }
        

        static IEnumerator ConsumeEnumerator(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                if (current == null)
                {
                    yield return null;
                }
                else if (current is CustomYieldInstruction cyi)
                {
                    // WWW, WaitForSecondsRealtime
                    while (cyi.keepWaiting)
                    {
                        yield return null;
                    }
                }
                else if (current is YieldInstruction)
                {
                    IEnumerator innerCoroutine = null;
                    switch (current)
                    {
                        case AsyncOperation ao:
                            innerCoroutine = UnwrapWaitAsyncOperation(ao);
                            break;
                        case WaitForSeconds wfs:
                            innerCoroutine = UnwrapWaitForSeconds(wfs);
                            break;
                    }
                    if (innerCoroutine != null)
                    {
                        while (innerCoroutine.MoveNext())
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        goto WARN;
                    }
                }
                else if (current is IEnumerator e3)
                {
                    var e4 = ConsumeEnumerator(e3);
                    while (e4.MoveNext())
                    {
                        yield return null;
                    }
                }
                else
                {
                    goto WARN;
                }

                continue;

                WARN:
                // WaitForEndOfFrame, WaitForFixedUpdate, others.
                UnityEngine.Debug.LogWarning($"yield {current.GetType().Name} is not supported on await IEnumerator or IEnumerator.ToLuminTask(), please use ToLuminTask(MonoBehaviour coroutineRunner) instead.");
                yield return null;
            }
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