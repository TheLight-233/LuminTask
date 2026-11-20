
// AsyncInstantiateOperation was added since Unity 2022.3.20 / 2023.3.0b7
#if UNITY_2022_3 && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10 || UNITY_2022_3_11 || UNITY_2022_3_12 || UNITY_2022_3_13 || UNITY_2022_3_14 || UNITY_2022_3_15 || UNITY_2022_3_16 || UNITY_2022_3_17 || UNITY_2022_3_18 || UNITY_2022_3_19)
#define UNITY_2022_SUPPORT
#endif

#if UNITY_2022_SUPPORT || UNITY_2023_3_OR_NEWER

using System;
using System.Threading;
using UnityEngine;
using LuminThread;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static class AsyncInstantiateLuminTask
    {
        // AsyncInstantiateOperation<T> has GetAwaiter so no need to impl
        // public static LuminTask<T[]> GetAwaiter<T>(this AsyncInstantiateOperation<T> operation) where T : Object

        public static LuminTask<UnityEngine.Object[]> WithCancellation(this AsyncInstantiateOperation asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityEngine.Object[]> WithCancellation(this AsyncInstantiateOperation asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityEngine.Object[]> ToLuminTask(this AsyncInstantiateOperation asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled<UnityEngine.Object[]>(cancellationToken);
            if (asyncOperation.isDone) 
                return LuminTask.FromResult(asyncOperation.Result);
            return UnityAsyncLuminTask.AwaitInstantiation(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

        public static LuminTask<T[]> WithCancellation<T>(this AsyncInstantiateOperation<T> asyncOperation, CancellationToken cancellationToken)
            where T : UnityEngine.Object
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<T[]> WithCancellation<T>(this AsyncInstantiateOperation<T> asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
            where T : UnityEngine.Object
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<T[]> ToLuminTask<T>(this AsyncInstantiateOperation<T> asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
            where T : UnityEngine.Object
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested) 
                return LuminTask.FromCanceled<T[]>(cancellationToken);
            if (asyncOperation.isDone) 
                return LuminTask.FromResult(UnityAsyncLuminTask.ReadInstances<T>(asyncOperation));
            return UnityAsyncLuminTask.AwaitInstantiation<T>(asyncOperation, timing, progress, cancellationToken, cancelImmediately);
        }

    }

    public static partial class UnityAsyncLuminTask
    {
        internal static LuminTask<UnityEngine.Object[]> AwaitInstantiation(AsyncInstantiateOperation operation,
            PlayerLoopTiming timing, IProgress<float> progress, CancellationToken token, bool immediate)
        {
            var handle = AsyncOperationPromise.Create(operation, timing, progress, token, immediate,
                InstantiateWriter.Write, PlainCanceledWriter.Write);
            return LuminTaskSource.ToTask<UnityEngine.Object[]>(handle);
        }

        internal static LuminTask<T[]> AwaitInstantiation<T>(AsyncInstantiateOperation<T> operation,
            PlayerLoopTiming timing, IProgress<float> progress, CancellationToken token, bool immediate) where T : UnityEngine.Object
        {
            var handle = AsyncOperationPromise.Create(operation, timing, progress, token, immediate,
                InstantiateWriter<T>.Write, PlainCanceledWriter.Write);
            return LuminTaskSource.ToTask<T[]>(handle);
        }

        // Unity 2022 can expose an Object[] through its T[] property by reinterpreting
        // the reference. Return an array whose actual runtime element type supports T[].
        internal static T[] ReadInstances<T>(AsyncInstantiateOperation operation) where T : UnityEngine.Object
        {
            var result = operation.Result;
            if (result == null || result is T[]) return (T[])result;
            if (result.Length == 0) return Array.Empty<T>();
            var typed = new T[result.Length];
            Array.Copy(result, typed, result.Length);
            return typed;
        }

        private static class InstantiateWriter
        {
            internal static readonly Action<AsyncOperationState> Write = state =>
                state.Published = LuminTaskSource.SetResult(state.Handle, ((AsyncInstantiateOperation)state.Operation).Result);
        }

        private static class InstantiateWriter<T> where T : UnityEngine.Object
        {
            internal static readonly Action<AsyncOperationState> Write = state =>
                state.Published = LuminTaskSource.SetResult(state.Handle, ReadInstances<T>((AsyncInstantiateOperation)state.Operation));
        }
    }
}

#endif
