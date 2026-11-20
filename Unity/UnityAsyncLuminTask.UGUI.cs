using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using LuminThread.TaskSource;
using LuminThread.Utility;
using System.Runtime.InteropServices;
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
            return AsyncUnityLuminTaskEventPromise.Create(unityEvent, PlayerLoopTiming.Update, cancellationToken, true);
        }

        public static AsyncUnityLuminTaskEventHandler<T> GetAsyncEventHandler<T>(this UnityEvent<T> unityEvent, CancellationToken cancellationToken)
        {
            return new AsyncUnityLuminTaskEventHandler<T>(unityEvent, cancellationToken, false);
        }

        public static LuminTask<T> OnInvokeAsync<T>(this UnityEvent<T> unityEvent, CancellationToken cancellationToken)
        {
            return AsyncUnityLuminTaskEventPromise<T>.Create(unityEvent, PlayerLoopTiming.Update, cancellationToken, true);
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

        #region AsyncUnityLuminTaskEventPromise（无返回值）

        private static class AsyncUnityLuminTaskEventPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public ulong Handle;
            }

            public static LuminTask Create(UnityEvent unityEvent, PlayerLoopTiming timing, CancellationToken cancellationToken, bool callOnce)
            {
                if (unityEvent == null)
                    throw new ArgumentNullException(nameof(unityEvent));
                
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled(cancellationToken);

                var task = LuminTaskSource.Create(out ulong handle);
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false,
                    Handle = handle
                };

                var stateTuple = StateTuple.Create(stateData, unityEvent);
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                UnityAction continuation = null;
                continuation = () =>
                {
                    if (stateData.Completed)
                        return;

                    stateData.Completed = true;
                    stateData.IsCancellationRequested = cancellationToken.IsCancellationRequested;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSource.SetCanceled(handle, cancellationToken);
                    }
                    else
                    {
                        LuminTaskSource.SetResult(handle);
                    }

                    if (continuation != null)
                    {
                        unityEvent.RemoveListener(continuation);
                        continuation = null;
                    }

                    if (callOnce)
                    {
                        stateTuple.Dispose();
                    }
                };

                unityEvent.AddListener(continuation);

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = state.State as StateTuple<(StateData, UnityEvent)>;
                if (stateTuple == null)
                    return false;

                if (stateTuple.Item.Item1.Completed)
                    return false;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    stateTuple.Item.Item1.Completed = true;
                    stateTuple.Item.Item1.IsCancellationRequested = true;
                    LuminTaskSource.SetCanceled(stateTuple.Item.Item1.Handle, state.CancellationToken);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region AsyncUnityLuminTaskEventPromise<T>（有返回值）

        private static class AsyncUnityLuminTaskEventPromise<T>
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
                public ulong Handle;
            }

            public static LuminTask<T> Create(UnityEvent<T> unityEvent, PlayerLoopTiming timing, CancellationToken cancellationToken, bool callOnce)
            {
                if (unityEvent == null)
                    throw new ArgumentNullException(nameof(unityEvent));
                
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled<T>(cancellationToken);

                var task = LuminTaskSource.Create<T>(out ulong handle);
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false,
                    Handle = handle
                };

                var stateTuple = StateTuple.Create(stateData, unityEvent);
                var state = new LuminTaskState(handle, cancellationToken, stateTuple);

                UnityAction<T> continuation = null;
                continuation = (result) =>
                {
                    if (stateData.Completed)
                        return;

                    stateData.Completed = true;
                    stateData.IsCancellationRequested = cancellationToken.IsCancellationRequested;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSource.SetCanceled(handle, cancellationToken);
                    }
                    else
                    {
                        LuminTaskSource.SetResult(handle, result);
                    }

                    if (continuation != null)
                    {
                        unityEvent.RemoveListener(continuation);
                        continuation = null;
                    }

                    if (callOnce)
                    {
                        stateTuple.Dispose();
                    }
                };

                unityEvent.AddListener(continuation);

                PlayerLoopHelper.AddAction(timing, state, MoveNextAction);

                return task;
            }

            private static readonly MoveNext MoveNextAction = MoveNext;

            public static bool MoveNext(in LuminTaskState state)
            {
                var stateTuple = state.State as StateTuple<(StateData, UnityEvent<T>)>;
                if (stateTuple == null)
                    return false;

                if (stateTuple.Item.Item1.Completed)
                    return false;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    stateTuple.Item.Item1.Completed = true;
                    stateTuple.Item.Item1.IsCancellationRequested = true;
                    LuminTaskSource.SetCanceled(stateTuple.Item.Item1.Handle, state.CancellationToken);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region AsyncUnityLuminTaskEventHandler（无返回值）

        public sealed class AsyncUnityLuminTaskEventHandler : IAsyncClickLuminTaskEventHandler, IDisposable
        {
            private readonly UnityEvent _unityEvent;
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
                    if (_disposed)
                        registration.Dispose();
                    else
                        _registration = registration;
                }
            }

            public LuminTask OnClickAsync()
            {
                return ToLuminTask();
            }

            public LuminTask ToLuminTask()
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

            private void Invoke()
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    var handle = _handle;
                    if (handle == 0) return;
                    _handle = 0;

                    if (_callOnce)
                    {
                        _disposed = true;
                        Detach();
                    }

                    LuminTaskSource.SetResult(handle);
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Detach();

                var handle = _handle;
                _handle = 0;
                if (handle != 0)
                    LuminTaskSource.SetCanceled(handle, _cancellationToken);
            }

            private void DisposeFromCancellation()
            {
                if (_disposed) return;
                _disposed = true;
                Detach(disposeRegistration: false);

                var handle = _handle;
                _handle = 0;
                if (handle != 0)
                    LuminTaskSource.SetCanceled(handle, _cancellationToken);
            }

            private void Detach(bool disposeRegistration = true)
            {
                if (_continuation != null && _unityEvent != null)
                {
                    _unityEvent.RemoveListener(_continuation);
                    _continuation = null;
                }

                if (disposeRegistration)
                    _registration.Dispose();
                _registration = default;
            }

            #region LuminTaskSource 实现

            #endregion
        }

        #endregion

        #region AsyncUnityLuminTaskEventHandler<T>（有返回值）

        public sealed class AsyncUnityLuminTaskEventHandler<T> : IDisposable, IAsyncValueChangedLuminTaskEventHandler<T>, IAsyncEndEditLuminTaskEventHandler<T>
        , TextMeshProAsyncLuminTask.IAsyncEndTextSelectionLuminTaskEventHandler<T>, TextMeshProAsyncLuminTask.IAsyncTextSelectionLuminTaskEventHandler<T>, TextMeshProAsyncLuminTask.IAsyncDeselectLuminTaskEventHandler<T>, TextMeshProAsyncLuminTask.IAsyncSelectLuminTaskEventHandler<T>, TextMeshProAsyncLuminTask.IAsyncSubmitLuminTaskEventHandler<T>
        {
            private readonly UnityEvent<T> _unityEvent;
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
                    if (_disposed)
                        registration.Dispose();
                    else
                        _registration = registration;
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

            private void Invoke(T result)
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    var handle = _handle;
                    if (handle == 0) return;
                    _handle = 0;

                    if (_callOnce)
                    {
                        _disposed = true;
                        Detach();
                    }

                    LuminTaskSource.SetResult(handle, result);
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Detach();

                var handle = _handle;
                _handle = 0;
                if (handle != 0)
                    LuminTaskSource.SetCanceled(handle, _cancellationToken);
            }

            private void DisposeFromCancellation()
            {
                if (_disposed) return;
                _disposed = true;
                Detach(disposeRegistration: false);

                var handle = _handle;
                _handle = 0;
                if (handle != 0)
                    LuminTaskSource.SetCanceled(handle, _cancellationToken);
            }

            private void Detach(bool disposeRegistration = true)
            {
                if (_continuation != null && _unityEvent != null)
                {
                    _unityEvent.RemoveListener(_continuation);
                    _continuation = null;
                }

                if (disposeRegistration)
                    _registration.Dispose();
                _registration = default;
            }

            #region LuminTaskSource<T> 实现

            #endregion
        }

        #endregion
        
    }
}
