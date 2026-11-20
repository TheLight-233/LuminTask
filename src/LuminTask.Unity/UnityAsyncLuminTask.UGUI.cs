using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static partial class UnityAsyncLuminTask
    {
        #region UnityEvent 扩展

        public static AsyncUnityLuminTaskEventHandler GetAsyncEventHandler(this UnityEvent unityEvent, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler(unityEvent, cancellationToken, false);
        }

        public static LuminTask OnInvokeAsync(this UnityEvent unityEvent, CancellationToken cancellationToken)
        {
            return OneShotEvent.Create(unityEvent, cancellationToken);
        }

        public static AsyncUnityLuminTaskEventHandler<T> GetAsyncEventHandler<T>(this UnityEvent<T> unityEvent, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<T>(unityEvent, cancellationToken, false);
        }

        public static LuminTask<T> OnInvokeAsync<T>(this UnityEvent<T> unityEvent, CancellationToken cancellationToken)
        {
            return OneShotEvent<T>.Create(unityEvent, cancellationToken);
        }

        #endregion

        #region Button 扩展

        public static IAsyncClickLuminTaskEventHandler GetAsyncClickEventHandler(this Button button)
        {
            return new AsyncUnityLuminTaskEventHandler(button.onClick, button.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncClickLuminTaskEventHandler GetAsyncClickEventHandler(this Button button, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler(button.onClick, cancellationToken, false);
        }

        public static LuminTask OnClickAsync(this Button button)
        {
            return button.onClick.OnInvokeAsync(button.GetOnDestroyCancellationToken());
        }

        public static LuminTask OnClickAsync(this Button button, CancellationToken cancellationToken)
        {
            return button.onClick.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region Toggle 扩展

        public static IAsyncValueChangedLuminTaskEventHandler<bool> GetAsyncValueChangedEventHandler(this Toggle toggle)
        {
            return new AsyncUnityLuminTaskEventHandler<bool>(toggle.onValueChanged, toggle.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncValueChangedLuminTaskEventHandler<bool> GetAsyncValueChangedEventHandler(this Toggle toggle, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<bool>(toggle.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<bool> OnValueChangedAsync(this Toggle toggle)
        {
            return toggle.onValueChanged.OnInvokeAsync(toggle.GetOnDestroyCancellationToken());
        }

        public static LuminTask<bool> OnValueChangedAsync(this Toggle toggle, CancellationToken cancellationToken)
        {
            return toggle.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region Scrollbar 扩展

        public static IAsyncValueChangedLuminTaskEventHandler<float> GetAsyncValueChangedEventHandler(this Scrollbar scrollbar)
        {
            return new AsyncUnityLuminTaskEventHandler<float>(scrollbar.onValueChanged, scrollbar.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncValueChangedLuminTaskEventHandler<float> GetAsyncValueChangedEventHandler(this Scrollbar scrollbar, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<float>(scrollbar.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<float> OnValueChangedAsync(this Scrollbar scrollbar)
        {
            return scrollbar.onValueChanged.OnInvokeAsync(scrollbar.GetOnDestroyCancellationToken());
        }

        public static LuminTask<float> OnValueChangedAsync(this Scrollbar scrollbar, CancellationToken cancellationToken)
        {
            return scrollbar.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region ScrollRect 扩展

        public static IAsyncValueChangedLuminTaskEventHandler<Vector2> GetAsyncValueChangedEventHandler(this ScrollRect scrollRect)
        {
            return new AsyncUnityLuminTaskEventHandler<Vector2>(scrollRect.onValueChanged, scrollRect.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncValueChangedLuminTaskEventHandler<Vector2> GetAsyncValueChangedEventHandler(this ScrollRect scrollRect, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<Vector2>(scrollRect.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<Vector2> OnValueChangedAsync(this ScrollRect scrollRect)
        {
            return scrollRect.onValueChanged.OnInvokeAsync(scrollRect.GetOnDestroyCancellationToken());
        }

        public static LuminTask<Vector2> OnValueChangedAsync(this ScrollRect scrollRect, CancellationToken cancellationToken)
        {
            return scrollRect.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region Slider 扩展

        public static IAsyncValueChangedLuminTaskEventHandler<float> GetAsyncValueChangedEventHandler(this Slider slider)
        {
            return new AsyncUnityLuminTaskEventHandler<float>(slider.onValueChanged, slider.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncValueChangedLuminTaskEventHandler<float> GetAsyncValueChangedEventHandler(this Slider slider, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<float>(slider.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<float> OnValueChangedAsync(this Slider slider)
        {
            return slider.onValueChanged.OnInvokeAsync(slider.GetOnDestroyCancellationToken());
        }

        public static LuminTask<float> OnValueChangedAsync(this Slider slider, CancellationToken cancellationToken)
        {
            return slider.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region InputField 扩展

        public static IAsyncEndEditLuminTaskEventHandler<string> GetAsyncEndEditEventHandler(this InputField inputField)
        {
            return new AsyncUnityLuminTaskEventHandler<string>(inputField.onEndEdit, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncEndEditLuminTaskEventHandler<string> GetAsyncEndEditEventHandler(this InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<string>(inputField.onEndEdit, cancellationToken, false);
        }

        public static LuminTask<string> OnEndEditAsync(this InputField inputField)
        {
            return inputField.onEndEdit.OnInvokeAsync(inputField.GetOnDestroyCancellationToken());
        }

        public static LuminTask<string> OnEndEditAsync(this InputField inputField, CancellationToken cancellationToken)
        {
            return inputField.onEndEdit.OnInvokeAsync(cancellationToken);
        }

        public static IAsyncValueChangedLuminTaskEventHandler<string> GetAsyncValueChangedEventHandler(this InputField inputField)
        {
            return new AsyncUnityLuminTaskEventHandler<string>(inputField.onValueChanged, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncValueChangedLuminTaskEventHandler<string> GetAsyncValueChangedEventHandler(this InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<string>(inputField.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<string> OnValueChangedAsync(this InputField inputField)
        {
            return inputField.onValueChanged.OnInvokeAsync(inputField.GetOnDestroyCancellationToken());
        }

        public static LuminTask<string> OnValueChangedAsync(this InputField inputField, CancellationToken cancellationToken)
        {
            return inputField.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region Dropdown 扩展

        public static IAsyncValueChangedLuminTaskEventHandler<int> GetAsyncValueChangedEventHandler(this Dropdown dropdown)
        {
            return new AsyncUnityLuminTaskEventHandler<int>(dropdown.onValueChanged, dropdown.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncValueChangedLuminTaskEventHandler<int> GetAsyncValueChangedEventHandler(this Dropdown dropdown, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<int>(dropdown.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<int> OnValueChangedAsync(this Dropdown dropdown)
        {
            return dropdown.onValueChanged.OnInvokeAsync(dropdown.GetOnDestroyCancellationToken());
        }

        public static LuminTask<int> OnValueChangedAsync(this Dropdown dropdown, CancellationToken cancellationToken)
        {
            return dropdown.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region 接口定义

        public interface IAsyncClickLuminTaskEventHandler : IDisposable
        {
            LuminTask OnClickAsync();
        }

        public interface IAsyncValueChangedLuminTaskEventHandler<T> : IDisposable
        {
            LuminTask<T> OnValueChangedAsync();
        }

        public interface IAsyncEndEditLuminTaskEventHandler<T> : IDisposable
        {
            LuminTask<T> OnEndEditAsync();
        }

        #endregion

        // Unity may already have snapshotted a listener when it is removed. Each subscription
        // keeps its own identity so an old Invoke or queued poll can never complete a new wait.
        private abstract class OneShotEventWait
        {
            private long _handle;
            protected readonly CancellationToken Token;
            protected OneShotEventWait(ulong handle, CancellationToken token)
            {
                _handle = unchecked((long)handle);
                Token = token;
            }

            protected abstract void Attach();
            protected abstract void Detach();

            protected ulong Take()
            {
                ulong handle = unchecked((ulong)Interlocked.Exchange(ref _handle, 0));
                if (handle != 0) Detach();
                return handle;
            }

            protected void Start(ulong handle)
            {
                try
                {
                    Attach();
                    PlayerLoopHelper.AddAction(PlayerLoopTiming.Update,
                        new LuminTaskState(handle, Token, this, Release), Poll);
                }
                catch
                {
                    Take();
                    LuminTaskSource.SetCanceled(handle, Token);
                    LuminTaskSource.ToTask(handle).Forget();
                    throw;
                }
            }

            private static readonly Action<object> Release = state => ((OneShotEventWait)state).Take();
            private static readonly MoveNext Poll = PollCancellation;
            private static bool PollCancellation(in LuminTaskState state)
            {
                var wait = (OneShotEventWait)state.State;
                if (Volatile.Read(ref wait._handle) == 0) return false;
                if (!state.CancellationToken.IsCancellationRequested) return true;
                var handle = wait.Take();
                if (handle != 0) LuminTaskSource.SetCanceled(handle, state.CancellationToken);
                return false;
            }
        }

        private sealed class OneShotEvent : OneShotEventWait
        {
            private UnityEvent _event;
            private readonly UnityAction _invoke;
            private OneShotEvent(UnityEvent source, ulong handle, CancellationToken token) : base(handle, token)
            {
                _event = source;
                _invoke = Invoke;
            }
            internal static LuminTask Create(UnityEvent source, CancellationToken token)
            {
                if (source == null) throw new ArgumentNullException("unityEvent");
                if (token.IsCancellationRequested) return LuminTask.FromCanceled(token);
                var task = LuminTaskSource.Create(out ulong handle);
                new OneShotEvent(source, handle, token).Start(handle);
                return task;
            }
            protected override void Attach() => _event.AddListener(_invoke);
            protected override void Detach() { _event.RemoveListener(_invoke); _event = null; }
            private void Invoke()
            {
                var handle = Take();
                if (handle == 0) return;
                if (Token.IsCancellationRequested) LuminTaskSource.SetCanceled(handle, Token);
                else LuminTaskSource.SetResult(handle);
            }
        }

        private sealed class OneShotEvent<T> : OneShotEventWait
        {
            private UnityEvent<T> _event;
            private readonly UnityAction<T> _invoke;
            private OneShotEvent(UnityEvent<T> source, ulong handle, CancellationToken token) : base(handle, token)
            {
                _event = source;
                _invoke = Invoke;
            }
            internal static LuminTask<T> Create(UnityEvent<T> source, CancellationToken token)
            {
                if (source == null) throw new ArgumentNullException("unityEvent");
                if (token.IsCancellationRequested) return LuminTask.FromCanceled<T>(token);
                var task = LuminTaskSource.Create<T>(out ulong handle);
                new OneShotEvent<T>(source, handle, token).Start(handle);
                return task;
            }
            protected override void Attach() => _event.AddListener(_invoke);
            protected override void Detach() { _event.RemoveListener(_invoke); _event = null; }
            private void Invoke(T result)
            {
                var handle = Take();
                if (handle == 0) return;
                if (Token.IsCancellationRequested) LuminTaskSource.SetCanceled(handle, Token);
                else LuminTaskSource.SetResult(handle, result);
            }
        }

        #region AsyncUnityLuminTaskEventHandler（无返回值）

        public sealed class AsyncUnityLuminTaskEventHandler : IAsyncClickLuminTaskEventHandler, IDisposable
        {
            private UnityEvent _unityEvent;
            private readonly CancellationToken _cancellationToken;
            private readonly bool _callOnce;

            private ulong _handle;
            private UnityAction _continuation;
            private CancellationTokenRegistration _registration;
            private bool _disposed;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static AsyncUnityLuminTaskEventHandler Create(UnityEvent unityEvent, CancellationToken cancellationToken, bool callOnce)
            {
                return new AsyncUnityLuminTaskEventHandler(unityEvent, cancellationToken, callOnce);
            }
            
            public AsyncUnityLuminTaskEventHandler(UnityEvent unityEvent, CancellationToken cancellationToken, bool callOnce)
            {
                _unityEvent = unityEvent ?? throw new ArgumentNullException(nameof(unityEvent));
                _cancellationToken = cancellationToken;
                _callOnce = callOnce;
                _handle = 0;
                _continuation = null;
                _registration = default;
                _disposed = false;

                if (_cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _continuation = Invoke;

                unityEvent.AddListener(_continuation);

                if (_cancellationToken.CanBeCanceled)
                {
                    var registration = _cancellationToken.Register(static state =>
                    {
                        var self = (AsyncUnityLuminTaskEventHandler)state;
                        self.DisposeFromCancellation();
                    }, this);
                    lock (this)
                    {
                        if (!_disposed) { _registration = registration; return; }
                    }
                    registration.Dispose();
                }
            }

            public LuminTask OnClickAsync()
            {
                return ToLuminTask();
            }

            public LuminTask ToLuminTask()
            {
                lock (this)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        return LuminTask.FromCanceled(_cancellationToken);
                    }

                    if (_disposed)
                        return LuminTask.FromCanceled(new CancellationToken(canceled: true));

                    if (_handle == 0)
                        LuminTaskSource.Rent(out _handle);

                    return LuminTaskSource.ToTask(_handle);
                }
            }

            private void Invoke()
            {
                ulong handle;
                lock (this)
                {
                    if (_disposed || _cancellationToken.IsCancellationRequested || _handle == 0) return;
                    handle = _handle;
                    _handle = 0;
                    if (_callOnce) _disposed = true;
                }
                if (_callOnce) Detach();
                LuminTaskSource.SetResult(handle);
            }

            public void Dispose() => Close(true);
            private void DisposeFromCancellation() => Close(false);

            private void Close(bool disposeRegistration)
            {
                ulong handle;
                lock (this)
                {
                    if (_disposed) return;
                    _disposed = true;
                    handle = _handle;
                    _handle = 0;
                }
                try { Detach(disposeRegistration); }
                finally { if (handle != 0) LuminTaskSource.SetCanceled(handle, _cancellationToken); }
            }

            private void Detach(bool disposeRegistration = true)
            {
                if (disposeRegistration) _registration.Dispose();
                _registration = default;
                if (_continuation == null) return;
                if (PlayerLoopHelper.IsMainThread) RemoveListener();
                else PlayerLoopHelper.AddContinuation(PlayerLoopTiming.Update, RemoveListener);
            }

            private void RemoveListener()
            {
                if (_continuation != null && _unityEvent != null)
                {
                    _unityEvent.RemoveListener(_continuation);
                    _unityEvent = null;
                    _continuation = null;
                }
            }

            #region LuminTaskSource 实现

            #endregion
        }

        #endregion

        #region AsyncUnityLuminTaskEventHandler<T>（有返回值）

        public sealed partial class AsyncUnityLuminTaskEventHandler<T> : IDisposable,
            IAsyncValueChangedLuminTaskEventHandler<T>, IAsyncEndEditLuminTaskEventHandler<T>
        {
            private UnityEvent<T> _unityEvent;
            private readonly CancellationToken _cancellationToken;
            private readonly bool _callOnce;

            private ulong _handle;
            private UnityAction<T> _continuation;
            private CancellationTokenRegistration _registration;
            private bool _disposed;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static AsyncUnityLuminTaskEventHandler<T> Create(UnityEvent<T> unityEvent, CancellationToken cancellationToken, bool callOnce)
            {
                return new AsyncUnityLuminTaskEventHandler<T>(unityEvent, cancellationToken, callOnce);
            }
            
            public AsyncUnityLuminTaskEventHandler(UnityEvent<T> unityEvent, CancellationToken cancellationToken, bool callOnce)
            {
                _unityEvent = unityEvent ?? throw new ArgumentNullException(nameof(unityEvent));
                _cancellationToken = cancellationToken;
                _callOnce = callOnce;
                _handle = 0;
                _continuation = null;
                _registration = default;
                _disposed = false;

                if (_cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _continuation = Invoke;

                unityEvent.AddListener(_continuation);

                if (_cancellationToken.CanBeCanceled)
                {
                    var registration = _cancellationToken.Register(static state =>
                    {
                        var self = (AsyncUnityLuminTaskEventHandler<T>)state;
                        self.DisposeFromCancellation();
                    }, this);
                    lock (this)
                    {
                        if (!_disposed) { _registration = registration; return; }
                    }
                    registration.Dispose();
                }
            }

            public LuminTask<T> OnValueChangedAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<T> OnEndEditAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<T> OnDeselectAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<T> OnEndTextSelectionAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<T> OnSelectAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<T> OnSubmitAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<T> OnTextSelectionAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<T> ToLuminTask()
            {
                lock (this)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        return LuminTask.FromCanceled<T>(_cancellationToken);
                    }

                    if (_disposed)
                        return LuminTask.FromCanceled<T>(new CancellationToken(canceled: true));

                    if (_handle == 0)
                        LuminTaskSource.Rent<T>(out _handle);

                    return LuminTaskSource.ToTask<T>(_handle);
                }
            }

            private void Invoke(T result)
            {
                ulong handle;
                lock (this)
                {
                    if (_disposed || _cancellationToken.IsCancellationRequested || _handle == 0) return;
                    handle = _handle;
                    _handle = 0;
                    if (_callOnce) _disposed = true;
                }
                if (_callOnce) Detach();
                LuminTaskSource.SetResult(handle, result);
            }

            public void Dispose() => Close(true);
            private void DisposeFromCancellation() => Close(false);

            private void Close(bool disposeRegistration)
            {
                ulong handle;
                lock (this)
                {
                    if (_disposed) return;
                    _disposed = true;
                    handle = _handle;
                    _handle = 0;
                }
                try { Detach(disposeRegistration); }
                finally { if (handle != 0) LuminTaskSource.SetCanceled(handle, _cancellationToken); }
            }

            private void Detach(bool disposeRegistration = true)
            {
                if (disposeRegistration) _registration.Dispose();
                _registration = default;
                if (_continuation == null) return;
                if (PlayerLoopHelper.IsMainThread) RemoveListener();
                else PlayerLoopHelper.AddContinuation(PlayerLoopTiming.Update, RemoveListener);
            }

            private void RemoveListener()
            {
                if (_continuation != null && _unityEvent != null)
                {
                    _unityEvent.RemoveListener(_continuation);
                    _unityEvent = null;
                    _continuation = null;
                }
            }

            #region LuminTaskSource<T> 实现

            #endregion
        }

        #endregion
        
    }
}
