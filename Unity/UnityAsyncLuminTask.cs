using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static partial class UnityAsyncLuminTask
    {
        #region AsyncOperation

        public static AsyncOperationLuminTaskAwaiter GetAwaiter(this AsyncOperation asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new AsyncOperationLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask WithCancellation(this AsyncOperation asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask WithCancellation(this AsyncOperation asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask ToLuminTask(this AsyncOperation asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.CompletedTask();
            var handle = AsyncOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately,
                AsyncOperationWriters.VoidResult, AsyncOperationWriters.PlainCanceled);
            return LuminTaskSource.ToTask(handle);
        }

        public struct AsyncOperationLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private AsyncOperation _asyncOperation;

            public AsyncOperationLuminTaskAwaiter(AsyncOperation asyncOperation)
            {
                _asyncOperation = asyncOperation;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public void GetResult() { }

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                // The handler removes itself when Unity raises it. A field written here would not be
                // visible to the resumed GetResult: this awaiter is a struct, and the builder copies
                // the state machine - and with it the awaiter - into its box before OnCompleted runs.
                // Self-removal needs no field, so the operation keeps no handler afterwards.
                var operation = _asyncOperation;
                Action<AsyncOperation> handler = null;
                handler = _ => { operation.completed -= handler; continuation(); };
                operation.completed += handler;
            }
        }

        #endregion

        #region ResourceRequest

        public static ResourceRequestLuminTaskAwaiter GetAwaiter(this ResourceRequest asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new ResourceRequestLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<UnityEngine.Object> WithCancellation(this ResourceRequest asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityEngine.Object> WithCancellation(this ResourceRequest asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityEngine.Object> ToLuminTask(this ResourceRequest asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<UnityEngine.Object>(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.FromResult(asyncOperation.asset);
            var handle = AsyncOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately,
                AsyncOperationWriters.ResourceRequestAsset, AsyncOperationWriters.PlainCanceled);
            return LuminTaskSource.ToTask<UnityEngine.Object>(handle);
        }

        public struct ResourceRequestLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private ResourceRequest _asyncOperation;

            public ResourceRequestLuminTaskAwaiter(ResourceRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public UnityEngine.Object GetResult() => _asyncOperation.asset;

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                var operation = _asyncOperation;
                Action<AsyncOperation> handler = null;
                handler = _ => { operation.completed -= handler; continuation(); };
                operation.completed += handler;
            }
        }

        #endregion

        #region AssetBundleRequest

        public static AssetBundleRequestLuminTaskAwaiter GetAwaiter(this AssetBundleRequest asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new AssetBundleRequestLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<UnityEngine.Object> WithCancellation(this AssetBundleRequest asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityEngine.Object> WithCancellation(this AssetBundleRequest asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityEngine.Object> ToLuminTask(this AssetBundleRequest asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<UnityEngine.Object>(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.FromResult(asyncOperation.asset);
            var handle = AsyncOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately,
                AsyncOperationWriters.AssetBundleRequestAsset, AsyncOperationWriters.PlainCanceled);
            return LuminTaskSource.ToTask<UnityEngine.Object>(handle);
        }

        public struct AssetBundleRequestLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private AssetBundleRequest _asyncOperation;

            public AssetBundleRequestLuminTaskAwaiter(AssetBundleRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public UnityEngine.Object GetResult() => _asyncOperation.asset;

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                var operation = _asyncOperation;
                Action<AsyncOperation> handler = null;
                handler = _ => { operation.completed -= handler; continuation(); };
                operation.completed += handler;
            }
        }

        #endregion

        #region AssetBundleCreateRequest

        public static AssetBundleCreateRequestLuminTaskAwaiter GetAwaiter(this AssetBundleCreateRequest asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new AssetBundleCreateRequestLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<AssetBundle> WithCancellation(this AssetBundleCreateRequest asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<AssetBundle> WithCancellation(this AssetBundleCreateRequest asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<AssetBundle> ToLuminTask(this AssetBundleCreateRequest asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<AssetBundle>(cancellationToken);
            if (asyncOperation.isDone)
                return LuminTask.FromResult(asyncOperation.assetBundle);
            var handle = AsyncOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately,
                AsyncOperationWriters.AssetBundleCreateResult, AsyncOperationWriters.PlainCanceled);
            return LuminTaskSource.ToTask<AssetBundle>(handle);
        }

        public struct AssetBundleCreateRequestLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private AssetBundleCreateRequest _asyncOperation;

            public AssetBundleCreateRequestLuminTaskAwaiter(AssetBundleCreateRequest asyncOperation)
            {
                _asyncOperation = asyncOperation;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public AssetBundle GetResult() => _asyncOperation.assetBundle;

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                var operation = _asyncOperation;
                Action<AsyncOperation> handler = null;
                handler = _ => { operation.completed -= handler; continuation(); };
                operation.completed += handler;
            }
        }

        #endregion

        #region UnityWebRequestAsyncOperation

        public static UnityWebRequestOperationLuminTaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOperation)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            return new UnityWebRequestOperationLuminTaskAwaiter(asyncOperation);
        }

        public static LuminTask<UnityWebRequest> WithCancellation(this UnityWebRequestAsyncOperation asyncOperation, CancellationToken cancellationToken)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static LuminTask<UnityWebRequest> WithCancellation(this UnityWebRequestAsyncOperation asyncOperation, CancellationToken cancellationToken, bool cancelImmediately)
        {
            return ToLuminTask(asyncOperation, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static LuminTask<UnityWebRequest> ToLuminTask(this UnityWebRequestAsyncOperation asyncOperation, IProgress<float> progress = null, PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default, bool cancelImmediately = false)
        {
            if (asyncOperation == null)
                throw new ArgumentNullException(nameof(asyncOperation));
            if (cancellationToken.IsCancellationRequested)
                return LuminTask.FromCanceled<UnityWebRequest>(cancellationToken);
            if (asyncOperation.isDone)
            {
                if (IsError(asyncOperation.webRequest))
                {
                    return LuminTask.FromException<UnityWebRequest>(new UnityWebRequestLuminTaskException(asyncOperation.webRequest));
                }
                return LuminTask.FromResult(asyncOperation.webRequest);
            }
            var handle = AsyncOperationPromise.Create(asyncOperation, timing, progress, cancellationToken, cancelImmediately,
                AsyncOperationWriters.WebRequestResult, AsyncOperationWriters.WebRequestCanceled);
            return LuminTaskSource.ToTask<UnityWebRequest>(handle);
        }

        public struct UnityWebRequestOperationLuminTaskAwaiter : ICriticalNotifyCompletion
        {
            private UnityWebRequestAsyncOperation _asyncOperation;

            public UnityWebRequestOperationLuminTaskAwaiter(UnityWebRequestAsyncOperation asyncOperation)
            {
                _asyncOperation = asyncOperation;
            }

            public bool IsCompleted => _asyncOperation.isDone;

            public UnityWebRequest GetResult()
            {
                if (IsError(_asyncOperation.webRequest))
                {
                    throw new UnityWebRequestLuminTaskException(_asyncOperation.webRequest);
                }
                return _asyncOperation.webRequest;
            }

            public void OnCompleted(Action continuation)
            {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                var operation = _asyncOperation;
                Action<AsyncOperation> handler = null;
                handler = _ => { operation.completed -= handler; continuation(); };
                operation.completed += handler;
            }
        }

        #endregion

        #region Shared AsyncOperation state

        // One control block for the whole AsyncOperation family (AsyncOperation, ResourceRequest,
        // AssetBundleRequest, AssetBundleCreateRequest, UnityWebRequestAsyncOperation) instead of five
        // identical private StateData copies, and it owns its completed-callback and its cancellation
        // callback: a promise costs no closure and, once the pool is warm, no allocation at all.
        //
        // Ownership. There is exactly one recycler - the loop node (AsyncOperationPromise.MoveNext).
        // The three completion paths only ever write the result:
        //   * the completed callback (main thread; it also unsubscribes and drops the registration),
        //   * the cancellation callback (the cancelling thread; it may not touch the operation or the
        //     registration, so it only writes the cancellation and lets the loop node clean up),
        //   * the loop node itself (main thread; poll for cancellation and for an operation that
        //     finished without raising the event).
        // A path that wins the completion gate publishes the end of its work through EndCompletion, and
        // the loop node recycles only after observing that. So the block can never be handed back to
        // the pool while a completion is still reading it, and a late completed callback can never
        // write into a block that already belongs to someone else.
        //
        // Which thread may touch the Unity object is part of the contract, not a detail: the cancel
        // callback runs on whichever thread cancelled the token and never touches the operation, the
        // registration or the event; the completed callback, the poll and the recycle all run on the
        // main thread and may. Writers take that permission as a parameter instead of guessing from a
        // thread id.
        //
        // Cancellation is decided at completion time, not at request time: a token that is already
        // cancelled when the operation finishes still completes the task as canceled. That is the
        // contract the pre-shared-block adapter had, and it keeps "cancelled before the next poll"
        // from silently turning into a success.
        private sealed class AsyncOperationState : IDisposable
        {
            private readonly Action<AsyncOperation> _onCompleted;
            private readonly Action _onCancelRequested;

            // 0 = running, 1 = a completer is inside its completion, 2 = completion finished.
            private int _phase;
            private int _pooled;

            internal ulong Handle;
            internal AsyncOperation Operation;
            internal IProgress<float> Progress;
            internal CancellationToken Token;
            internal CancellationTokenRegistration Registration;

            // Set by whichever path publishes the cancellation, so the recycle can give the operation
            // its main-thread chance to react (see WriteWebRequestCanceled).
            internal bool Canceled;

            // Set by a writer as soon as the source accepted the completion; the phase exit uses it to
            // tell "published, then a continuation threw" from "threw before publishing".
            internal bool Published;

            private Action<AsyncOperationState> _writeCompletion;
            private Action<AsyncOperationState, bool> _writeCancellation;

            internal AsyncOperationState()
            {
                _onCompleted = OnCompleted;
                _onCancelRequested = OnCancelRequested;
            }

            internal Action<AsyncOperation> CompletedCallback => _onCompleted;

            internal Action CancellationCallback => _onCancelRequested;

            internal bool IsCompleting => Volatile.Read(ref _phase) == 1;

            internal bool IsCompletionFinished => Volatile.Read(ref _phase) == 2;

            internal void Initialize(ulong handle, AsyncOperation operation, IProgress<float> progress,
                CancellationToken token, Action<AsyncOperationState> writeCompletion, Action<AsyncOperationState, bool> writeCancellation)
            {
                Handle = handle;
                Operation = operation;
                Progress = progress;
                Token = token;
                Registration = default;
                _writeCompletion = writeCompletion;
                _writeCancellation = writeCancellation;
                Canceled = false;
                Published = false;
                _phase = 0;
                _pooled = 0;
            }

            // The first caller becomes the only writer of the handle.
            internal bool TryBeginCompletion() => Interlocked.CompareExchange(ref _phase, 1, 0) == 0;

            internal void EndCompletion() => Volatile.Write(ref _phase, 2);

            internal void WriteCompletion() => _writeCompletion(this);

            internal void WriteCancellation(bool allowOperationAccess)
            {
                Canceled = true;
                _writeCancellation(this, allowOperationAccess);
            }

            // Runs the writer, reports a fault if it failed before publishing anything, and always leaves
            // the completion phase - a continuation registered directly on the task (a legal shape that
            // the compiler does not wrap) may throw while the publish is being delivered, and without the
            // finally the block would stay in phase 1 forever: the loop node would keep returning true and
            // retain the operation, the progress object, the token registration and the block itself,
            // while Bag live stays zero and hides it.
            //
            // A throw after the publish is rethrown rather than swallowed: the slot is complete, so there
            // is nothing left to report to the awaiter, and hiding a user error is worse than surfacing it
            // where it happened.
            private void WriteAndEndCompletion(bool canceled, bool allowOperationAccess)
            {
                try
                {
                    if (canceled)
                        WriteCancellation(allowOperationAccess);
                    else
                        WriteCompletion();
                }
                catch (Exception exception)
                {
                    if (!Published)
                    {
                        // The writer failed before the source accepted anything - a Unity accessor on a
                        // released operation, an abort on a disposed request. Completing as faulted keeps
                        // the awaiter from waiting forever on a task nobody will finish.
                        LuminTaskSource.SetException(Handle, exception);
                    }

                    throw;
                }
                finally
                {
                    EndCompletion();
                }
            }

            // Main-thread completion, shared by the completed event and the poll: the two paths that may
            // touch the operation do the unsubscription, the registration and the phase exit in one
            // place, and both consult the token so a cancelled token always wins over a finished
            // operation.
            internal void CompleteOnMainThread(bool canceled)
            {
                if (!TryBeginCompletion())
                    return;

                Operation.completed -= _onCompleted;
                Registration.Dispose();

                WriteAndEndCompletion(canceled || Token.IsCancellationRequested, allowOperationAccess: true);
            }

            private void OnCompleted(AsyncOperation operation)
                => CompleteOnMainThread(canceled: false);

            private void OnCancelRequested()
            {
                if (!TryBeginCompletion())
                    return;

                // Runs on whichever thread cancelled the token. Unity objects are not thread-safe and
                // disposing our own registration from inside its callback is not allowed, so the
                // unsubscription, the registration and the optional abort wait for a main-thread pass.
                WriteAndEndCompletion(canceled: true, allowOperationAccess: false);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _pooled, 1) != 0)
                    return;

                // A completer is still inside its gate (another thread, or a user exception escaped
                // MoveNext mid-completion): the only safe move is to abandon the block to the GC.
                if (IsCompleting)
                    return;

                try
                {
                    // This pass runs on the loop (main) thread, so a cancellation that could not touch the
                    // request when it was published gets its chance here. Publishing again is a no-op
                    // because a slot completes once, and a failure here is swallowed on purpose: the task
                    // is already complete, so there is no awaiter left to inform, and the request may
                    // legitimately be released by the time the recycle runs.
                    if (Canceled)
                        _writeCancellation(this, true);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"LuminTask Unity adapter: deferred cancellation cleanup failed: {exception}");
                }
                finally
                {
                    if (Operation != null)
                    {
                        Operation.completed -= _onCompleted;
                        Operation = null;
                    }

                    Registration.Dispose();
                    Registration = default;
                    Progress = null;
                    Token = default;
                    Canceled = false;
                    Published = false;
                    _writeCompletion = null;
                    _writeCancellation = null;

                    AsyncOperationStatePool.Return(this);
                }
            }
        }

        // Bounded, non-generic pool: all five adapters share one closed block type, and the blocks keep
        // their cached callbacks warm. Mirrors the WaitNodePool shape used by AsyncEx (fast slot, then
        // a capped chain) so nothing here grows with the peak the process reached.
        private static class AsyncOperationStatePool
        {
            private const int MaxRetained = 16;

            private static AsyncOperationState _fast;
            private static readonly ConcurrentQueue<AsyncOperationState> _overflow = new ConcurrentQueue<AsyncOperationState>();

            internal static AsyncOperationState Rent()
            {
                var state = Interlocked.Exchange(ref _fast, null);
                if (state != null)
                    return state;

                return _overflow.TryDequeue(out state) ? state : new AsyncOperationState();
            }

            internal static void Return(AsyncOperationState state)
            {
                if (Interlocked.CompareExchange(ref _fast, state, null) == null)
                    return;

                if (_overflow.Count >= MaxRetained)
                    return;

                _overflow.Enqueue(state);
            }
        }

        private static class AsyncOperationPromise
        {
            private static readonly MoveNext MoveNextAction = MoveNext;

            internal static ulong Create(AsyncOperation operation, PlayerLoopTiming timing, IProgress<float> progress,
                CancellationToken cancellationToken, bool cancelImmediately,
                Action<AsyncOperationState> writeCompletion, Action<AsyncOperationState, bool> writeCancellation)
            {
                LuminTaskSource.Rent(out var handle);

                var state = AsyncOperationStatePool.Rent();
                state.Initialize(handle, operation, progress, cancellationToken, writeCompletion, writeCancellation);

                // Registered before the operation subscribes: if the token fires during the call the
                // cancellation path still runs first and the loop node removes the leftover handler.
                if (cancelImmediately && cancellationToken.CanBeCanceled)
                {
                    state.Registration = cancellationToken.Register(state.CancellationCallback);
                }

                operation.completed += state.CompletedCallback;

                PlayerLoopHelper.AddAction(timing, new LuminTaskState(handle, cancellationToken, state), MoveNextAction);

                return handle;
            }

            internal static bool MoveNext(in LuminTaskState state)
            {
                if (state.State is not AsyncOperationState asyncOperationState)
                    return false;

                // The completion already ran to its end, so this pass owns the recycle.
                if (asyncOperationState.IsCompletionFinished)
                {
                    asyncOperationState.Dispose();
                    return false;
                }

                // A completer is in flight; keep the item for one more pass instead of racing it.
                if (asyncOperationState.IsCompleting)
                    return true;

                var operation = asyncOperationState.Operation;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    asyncOperationState.CompleteOnMainThread(canceled: true);
                    return true;
                }

                asyncOperationState.Progress?.Report(operation.progress);

                if (operation.isDone)
                {
                    asyncOperationState.CompleteOnMainThread(canceled: false);
                }

                return true;
            }
        }

        // One cached writer per adapter shape. Holding them here keeps the shared block free of any
        // result-kind switch, and the typed SetResult<T> instantiation stays with the adapter that owns
        // the result type. The delegates are built once at type initialisation, never per operation.
        private static class AsyncOperationWriters
        {
            internal static readonly Action<AsyncOperationState> VoidResult = WriteVoidResult;
            internal static readonly Action<AsyncOperationState> ResourceRequestAsset = WriteResourceRequestAsset;
            internal static readonly Action<AsyncOperationState> AssetBundleRequestAsset = WriteAssetBundleRequestAsset;
            internal static readonly Action<AsyncOperationState> AssetBundleCreateResult = WriteAssetBundleCreateResult;
            internal static readonly Action<AsyncOperationState> WebRequestResult = WriteWebRequestResult;
            internal static readonly Action<AsyncOperationState, bool> PlainCanceled = WritePlainCanceled;
            internal static readonly Action<AsyncOperationState, bool> WebRequestCanceled = WriteWebRequestCanceled;

            // Published is only ever set on the false -> true edge: the recycle publishes the cancellation a
            // second time (to give the request its main-thread abort) and that must not clear the flag the
            // phase exit read.
            private static void WriteVoidResult(AsyncOperationState state)
            {
                if (LuminTaskSource.SetResult(state.Handle))
                    state.Published = true;
            }

            private static void WriteResourceRequestAsset(AsyncOperationState state)
            {
                if (LuminTaskSource.SetResult(state.Handle, ((ResourceRequest)state.Operation).asset))
                    state.Published = true;
            }

            private static void WriteAssetBundleRequestAsset(AsyncOperationState state)
            {
                if (LuminTaskSource.SetResult(state.Handle, ((AssetBundleRequest)state.Operation).asset))
                    state.Published = true;
            }

            private static void WriteAssetBundleCreateResult(AsyncOperationState state)
            {
                if (LuminTaskSource.SetResult(state.Handle, ((AssetBundleCreateRequest)state.Operation).assetBundle))
                    state.Published = true;
            }

            private static void WriteWebRequestResult(AsyncOperationState state)
            {
                var request = ((UnityWebRequestAsyncOperation)state.Operation).webRequest;
                bool published = IsError(request)
                    ? LuminTaskSource.SetException(state.Handle, new UnityWebRequestLuminTaskException(request))
                    : LuminTaskSource.SetResult(state.Handle, request);

                if (published)
                    state.Published = true;
            }

            private static void WritePlainCanceled(AsyncOperationState state, bool allowOperationAccess)
            {
                if (LuminTaskSource.SetCanceled(state.Handle, state.Token))
                    state.Published = true;
            }

            private static void WriteWebRequestCanceled(AsyncOperationState state, bool allowOperationAccess)
            {
                // Abort must not run on the cancelling thread: Unity objects are single-threaded. The
                // cancellation callback therefore publishes the cancellation only, and the passes that
                // run on the main thread (the poll, or the recycle that follows) do the abort. Publishing
                // twice is harmless - a slot completes once.
                if (allowOperationAccess)
                    ((UnityWebRequestAsyncOperation)state.Operation).webRequest.Abort();

                if (LuminTaskSource.SetCanceled(state.Handle, state.Token))
                    state.Published = true;
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.ProtocolError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

    }
}
