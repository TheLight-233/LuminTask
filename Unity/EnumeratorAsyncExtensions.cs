using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using LuminThread;
using LuminThread.TaskSource;
using LuminThread.Utility;
using UnityEngine;

public static unsafe class EnumeratorAsyncExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerator Core(IEnumerator inner, MonoBehaviour coroutineRunner, IntPtr source)
    {
        yield return coroutineRunner.StartCoroutine(inner);
        Warp(source);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Warp(IntPtr source)
        {
            LuminTaskSourceCore<AsyncUnit>.TrySetResult(source.ToPointer());
            LuminTaskSourceCore<AsyncUnit>.Dispose(source.ToPointer());
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTaskAwaiter GetAwaiter<T>(this T enumerator)
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
        var source = LuminTaskSourceCore<AsyncUnit>.Create();
        coroutineRunner.StartCoroutine(Core(enumerator, coroutineRunner, new IntPtr(source)));
        return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, source, source->Id);
    }
    
    public struct EnumeratorPromise
    {
        struct InfoWarp
        {
            public int InitialFrame;
            public bool LoopRunning;
            public bool CalledGetResult;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask Create(IEnumerator innerEnumerator, PlayerLoopTiming timing, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return LuminTask.FromCanceled(cancellationToken);
            }
            
            var core = LuminTaskSourceCore<AsyncUnit>.Create();
            
            IEnumerator enumerator = ConsumeEnumerator(innerEnumerator);
            
            var infos = new InfoWarp()
            {
                CalledGetResult = false,
                LoopRunning = true,
                InitialFrame = -1
            };

            var state = new LuminTaskState(core, cancellationToken, enumerator, StateTuple.Create(infos));
            
            // run immediately.
            if (MoveNext(state))
            {
                
                PlayerLoopHelper.AddAction(timing, state, &MoveNext);
            }
            
            return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTable, core, core->Id);
        }

        public static bool MoveNext(in LuminTaskState state)
        {
            ref LuminTaskState stateRef = ref Unsafe.AsRef(state);
            IEnumerator innerEnumerator = Unsafe.As<IEnumerator>(state.State);
            StateTuple<InfoWarp> infos = Unsafe.As<StateTuple<InfoWarp>>(state.StateTuple);
            
            if (infos.Item1.CalledGetResult)
            {
                infos.Item1.LoopRunning = false;
                Dispose(ref stateRef);
                return false;
            }

            if (innerEnumerator == null) // invalid status, returned but loop running?
            {
                return false;
            }

            if (state.CancellationToken.IsCancellationRequested)
            {
                infos.Item1.LoopRunning = false;
                LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                infos.Dispose();
                Dispose(ref stateRef);
                return false;
            }

            if (infos.Item1.InitialFrame == -1)
            {
                // Time can not touch in threadpool.
                if (PlayerLoopHelper.IsMainThread)
                {
                    infos.Item1.InitialFrame = Time.frameCount;
                }
            }
            else if (infos.Item1.InitialFrame == Time.frameCount)
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
                infos.Item1.LoopRunning = false;
                LuminTaskSourceCore<AsyncUnit>.TrySetException(state.Source, ex);
                infos.Dispose();
                Dispose(ref stateRef);
                return false;
            }

            infos.Item1.LoopRunning = false;
            LuminTaskSourceCore<AsyncUnit>.TrySetResult(state.Source);
            infos.Dispose();
            Dispose(ref stateRef);
            return false;
        }
        
        static void Dispose(ref LuminTaskState state)
        {
            if (state.Source != null)
            {
                LuminTaskSourceCore<AsyncUnit>.Dispose(state.Source);
            }
            
            state = default;
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
                UnityEngine.Debug.LogWarning($"yield {current.GetType().Name} is not supported on await IEnumerator or IEnumerator.ToUniTask(), please use ToUniTask(MonoBehaviour coroutineRunner) instead.");
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