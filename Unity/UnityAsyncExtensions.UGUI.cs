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
    public static partial class UnityAsyncExtensions
    {
        #region UnityEvent 扩展

        public static AsyncUnityEventHandler GetAsyncEventHandler(this UnityEvent unityEvent, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler(unityEvent, cancellationToken, false);
        }

        public static LuminTask OnInvokeAsync(this UnityEvent unityEvent, CancellationToken cancellationToken)
        {
            return AsyncUnityEventPromise.Create(unityEvent, PlayerLoopTiming.Update, cancellationToken, true);
        }

        public static AsyncUnityEventHandler<T> GetAsyncEventHandler<T>(this UnityEvent<T> unityEvent, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler<T>(unityEvent, cancellationToken, false);
        }

        public static LuminTask<T> OnInvokeAsync<T>(this UnityEvent<T> unityEvent, CancellationToken cancellationToken)
        {
            return AsyncUnityEventPromise<T>.Create(unityEvent, PlayerLoopTiming.Update, cancellationToken, true);
        }

        #endregion

        #region Button 扩展

        public static IAsyncClickEventHandler GetAsyncClickEventHandler(this Button button)
        {
            return new AsyncUnityEventHandler(button.onClick, button.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncClickEventHandler GetAsyncClickEventHandler(this Button button, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler(button.onClick, cancellationToken, false);
        }

        public static LuminTask OnClickAsync(this Button button)
        {
            return button.onClick.OnInvokeAsync(button.GetCancellationTokenOnDestroy());
        }

        public static LuminTask OnClickAsync(this Button button, CancellationToken cancellationToken)
        {
            return button.onClick.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region Toggle 扩展

        public static IAsyncValueChangedEventHandler<bool> GetAsyncValueChangedEventHandler(this Toggle toggle)
        {
            return new AsyncUnityEventHandler<bool>(toggle.onValueChanged, toggle.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncValueChangedEventHandler<bool> GetAsyncValueChangedEventHandler(this Toggle toggle, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler<bool>(toggle.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<bool> OnValueChangedAsync(this Toggle toggle)
        {
            return toggle.onValueChanged.OnInvokeAsync(toggle.GetCancellationTokenOnDestroy());
        }

        public static LuminTask<bool> OnValueChangedAsync(this Toggle toggle, CancellationToken cancellationToken)
        {
            return toggle.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region Scrollbar 扩展

        public static IAsyncValueChangedEventHandler<float> GetAsyncValueChangedEventHandler(this Scrollbar scrollbar)
        {
            return new AsyncUnityEventHandler<float>(scrollbar.onValueChanged, scrollbar.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncValueChangedEventHandler<float> GetAsyncValueChangedEventHandler(this Scrollbar scrollbar, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler<float>(scrollbar.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<float> OnValueChangedAsync(this Scrollbar scrollbar)
        {
            return scrollbar.onValueChanged.OnInvokeAsync(scrollbar.GetCancellationTokenOnDestroy());
        }

        public static LuminTask<float> OnValueChangedAsync(this Scrollbar scrollbar, CancellationToken cancellationToken)
        {
            return scrollbar.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region ScrollRect 扩展

        public static IAsyncValueChangedEventHandler<Vector2> GetAsyncValueChangedEventHandler(this ScrollRect scrollRect)
        {
            return new AsyncUnityEventHandler<Vector2>(scrollRect.onValueChanged, scrollRect.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncValueChangedEventHandler<Vector2> GetAsyncValueChangedEventHandler(this ScrollRect scrollRect, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler<Vector2>(scrollRect.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<Vector2> OnValueChangedAsync(this ScrollRect scrollRect)
        {
            return scrollRect.onValueChanged.OnInvokeAsync(scrollRect.GetCancellationTokenOnDestroy());
        }

        public static LuminTask<Vector2> OnValueChangedAsync(this ScrollRect scrollRect, CancellationToken cancellationToken)
        {
            return scrollRect.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region Slider 扩展

        public static IAsyncValueChangedEventHandler<float> GetAsyncValueChangedEventHandler(this Slider slider)
        {
            return new AsyncUnityEventHandler<float>(slider.onValueChanged, slider.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncValueChangedEventHandler<float> GetAsyncValueChangedEventHandler(this Slider slider, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler<float>(slider.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<float> OnValueChangedAsync(this Slider slider)
        {
            return slider.onValueChanged.OnInvokeAsync(slider.GetCancellationTokenOnDestroy());
        }

        public static LuminTask<float> OnValueChangedAsync(this Slider slider, CancellationToken cancellationToken)
        {
            return slider.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region InputField 扩展

        public static IAsyncEndEditEventHandler<string> GetAsyncEndEditEventHandler(this InputField inputField)
        {
            return new AsyncUnityEventHandler<string>(inputField.onEndEdit, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncEndEditEventHandler<string> GetAsyncEndEditEventHandler(this InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler<string>(inputField.onEndEdit, cancellationToken, false);
        }

        public static LuminTask<string> OnEndEditAsync(this InputField inputField)
        {
            return inputField.onEndEdit.OnInvokeAsync(inputField.GetCancellationTokenOnDestroy());
        }

        public static LuminTask<string> OnEndEditAsync(this InputField inputField, CancellationToken cancellationToken)
        {
            return inputField.onEndEdit.OnInvokeAsync(cancellationToken);
        }

        public static IAsyncValueChangedEventHandler<string> GetAsyncValueChangedEventHandler(this InputField inputField)
        {
            return new AsyncUnityEventHandler<string>(inputField.onValueChanged, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncValueChangedEventHandler<string> GetAsyncValueChangedEventHandler(this InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler<string>(inputField.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<string> OnValueChangedAsync(this InputField inputField)
        {
            return inputField.onValueChanged.OnInvokeAsync(inputField.GetCancellationTokenOnDestroy());
        }

        public static LuminTask<string> OnValueChangedAsync(this InputField inputField, CancellationToken cancellationToken)
        {
            return inputField.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region Dropdown 扩展

        public static IAsyncValueChangedEventHandler<int> GetAsyncValueChangedEventHandler(this Dropdown dropdown)
        {
            return new AsyncUnityEventHandler<int>(dropdown.onValueChanged, dropdown.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncValueChangedEventHandler<int> GetAsyncValueChangedEventHandler(this Dropdown dropdown, CancellationToken cancellationToken)
        {
            return new AsyncUnityEventHandler<int>(dropdown.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<int> OnValueChangedAsync(this Dropdown dropdown)
        {
            return dropdown.onValueChanged.OnInvokeAsync(dropdown.GetCancellationTokenOnDestroy());
        }

        public static LuminTask<int> OnValueChangedAsync(this Dropdown dropdown, CancellationToken cancellationToken)
        {
            return dropdown.onValueChanged.OnInvokeAsync(cancellationToken);
        }

        #endregion

        #region 接口定义

        public interface IAsyncClickEventHandler : IDisposable
        {
            LuminTask OnClickAsync();
        }

        public interface IAsyncValueChangedEventHandler<T> : IDisposable
        {
            LuminTask<T> OnValueChangedAsync();
        }

        public interface IAsyncEndEditEventHandler<T> : IDisposable
        {
            LuminTask<T> OnEndEditAsync();
        }

        #endregion

        #region AsyncUnityEventPromise（无返回值）

        private static unsafe class AsyncUnityEventPromise
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
            }

            public static LuminTask Create(UnityEvent unityEvent, PlayerLoopTiming timing, CancellationToken cancellationToken, bool callOnce)
            {
                if (unityEvent == null)
                    throw new ArgumentNullException(nameof(unityEvent));
                
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled(cancellationToken);

                var core = LuminTaskSourceCore<AsyncUnit>.Create();
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false
                };

                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, cancellationToken, unityEvent, stateTuple);

                UnityAction continuation = null;
                continuation = () =>
                {
                    if (stateData.Completed)
                        return;

                    stateData.Completed = true;
                    stateData.IsCancellationRequested = cancellationToken.IsCancellationRequested;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(core);
                    }
                    else
                    {
                        LuminTaskSourceCore<AsyncUnit>.TrySetResult(core);
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

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTablePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var unityEvent = state.State as UnityEvent;
                if (unityEvent == null)
                    return false;

                var stateTuple = state.StateTuple as StateTuple<StateData>;
                if (stateTuple == null)
                    return false;

                if (stateTuple.Item1.Completed)
                    return false;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = true;
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region AsyncUnityEventPromise<T>（有返回值）

        private static unsafe class AsyncUnityEventPromise<T>
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct StateData
            {
                public bool Completed;
                public bool IsCancellationRequested;
            }

            public static LuminTask<T> Create(UnityEvent<T> unityEvent, PlayerLoopTiming timing, CancellationToken cancellationToken, bool callOnce)
            {
                if (unityEvent == null)
                    throw new ArgumentNullException(nameof(unityEvent));
                
                if (cancellationToken.IsCancellationRequested)
                    return LuminTask.FromCanceled<T>(cancellationToken);

                var core = LuminTaskSourceCore<T>.Create();
                var stateData = new StateData
                {
                    Completed = false,
                    IsCancellationRequested = false
                };

                var stateTuple = StateTuple.Create(stateData);
                var state = new LuminTaskState(core, cancellationToken, unityEvent, stateTuple);

                UnityAction<T> continuation = null;
                continuation = (result) =>
                {
                    if (stateData.Completed)
                        return;

                    stateData.Completed = true;
                    stateData.IsCancellationRequested = cancellationToken.IsCancellationRequested;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        LuminTaskSourceCore<T>.TrySetCanceled(core);
                    }
                    else
                    {
                        LuminTaskSourceCore<T>.TrySetResult(core, result);
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

                PlayerLoopHelper.AddAction(timing, state, &MoveNext);

                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTablePtr, core, core->Id);
            }

            public static bool MoveNext(in LuminTaskState state)
            {
                var unityEvent = state.State as UnityEvent<T>;
                if (unityEvent == null)
                    return false;

                var stateTuple = state.StateTuple as StateTuple<StateData>;
                if (stateTuple == null)
                    return false;

                if (stateTuple.Item1.Completed)
                    return false;

                if (state.CancellationToken.IsCancellationRequested)
                {
                    stateTuple.Item1.Completed = true;
                    stateTuple.Item1.IsCancellationRequested = true;
                    LuminTaskSourceCore<T>.TrySetCanceled(state.Source);
                    stateTuple.Dispose();
                    return false;
                }

                return true;
            }
        }

        #endregion

        #region AsyncUnityEventHandler（无返回值）

        public unsafe sealed class AsyncUnityEventHandler : IAsyncClickEventHandler, IDisposable
        {
            private readonly UnityEvent _unityEvent;
            private readonly CancellationToken _cancellationToken;
            private readonly bool _callOnce;

            private LuminTaskSourceCore<AsyncUnit>* _core;
            private UnityAction _continuation;
            private CancellationTokenRegistration _registration;
            private bool _disposed;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static AsyncUnityEventHandler Create(UnityEvent unityEvent, CancellationToken cancellationToken, bool callOnce)
            {
                return new AsyncUnityEventHandler(unityEvent, cancellationToken, callOnce);
            }
            
            public AsyncUnityEventHandler(UnityEvent unityEvent, CancellationToken cancellationToken, bool callOnce)
            {
                _unityEvent = unityEvent ?? throw new ArgumentNullException(nameof(unityEvent));
                _cancellationToken = cancellationToken;
                _callOnce = callOnce;
                _core = default;
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
                        var self = (AsyncUnityEventHandler)state;
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

                if (_core == null)
                    _core = LuminTaskSourceCore<AsyncUnit>.Create();

                return new LuminTask(LuminTaskSourceCore<AsyncUnit>.MethodTablePtr, _core, _core->Id);
            }

            private void Invoke()
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    var core = _core;
                    if (core == null) return;
                    _core = null;

                    if (_callOnce)
                    {
                        _disposed = true;
                        Detach();
                    }

                    LuminTaskSourceCore<AsyncUnit>.TrySetResult(core);
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Detach();

                var core = _core;
                _core = null;
                if (core != null)
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(core, _cancellationToken);
            }

            private void DisposeFromCancellation()
            {
                if (_disposed) return;
                _disposed = true;
                Detach(disposeRegistration: false);

                var core = _core;
                _core = null;
                if (core != null)
                    LuminTaskSourceCore<AsyncUnit>.TrySetCanceled(core, _cancellationToken);
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

            #region ILuminTaskSource 实现

            #endregion
        }

        #endregion

        #region AsyncUnityEventHandler<T>（有返回值）

        public unsafe sealed class AsyncUnityEventHandler<T> : IDisposable, IAsyncValueChangedEventHandler<T>, IAsyncEndEditEventHandler<T>
        , TextMeshProAsyncExtensions.IAsyncEndTextSelectionEventHandler<T>, TextMeshProAsyncExtensions.IAsyncTextSelectionEventHandler<T>, TextMeshProAsyncExtensions.IAsyncDeselectEventHandler<T>, TextMeshProAsyncExtensions.IAsyncSelectEventHandler<T>, TextMeshProAsyncExtensions.IAsyncSubmitEventHandler<T>
        {
            private readonly UnityEvent<T> _unityEvent;
            private readonly CancellationToken _cancellationToken;
            private readonly bool _callOnce;

            private LuminTaskSourceCore<T>* _core;
            private UnityAction<T> _continuation;
            private CancellationTokenRegistration _registration;
            private bool _disposed;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static AsyncUnityEventHandler<T> Create(UnityEvent<T> unityEvent, CancellationToken cancellationToken, bool callOnce)
            {
                return new AsyncUnityEventHandler<T>(unityEvent, cancellationToken, callOnce);
            }
            
            public AsyncUnityEventHandler(UnityEvent<T> unityEvent, CancellationToken cancellationToken, bool callOnce)
            {
                _unityEvent = unityEvent ?? throw new ArgumentNullException(nameof(unityEvent));
                _cancellationToken = cancellationToken;
                _callOnce = callOnce;
                _core = default;
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
                        var self = (AsyncUnityEventHandler<T>)state;
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

                if (_core == null)
                    _core = LuminTaskSourceCore<T>.Create();

                return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTablePtr, _core, _core->Id);
            }

            private void Invoke(T result)
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    var core = _core;
                    if (core == null) return;
                    _core = null;

                    if (_callOnce)
                    {
                        _disposed = true;
                        Detach();
                    }

                    LuminTaskSourceCore<T>.TrySetResult(core, result);
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Detach();

                var core = _core;
                _core = null;
                if (core != null)
                    LuminTaskSourceCore<T>.TrySetCanceled(core, _cancellationToken);
            }

            private void DisposeFromCancellation()
            {
                if (_disposed) return;
                _disposed = true;
                Detach(disposeRegistration: false);

                var core = _core;
                _core = null;
                if (core != null)
                    LuminTaskSourceCore<T>.TrySetCanceled(core, _cancellationToken);
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

            #region ILuminTaskSource<T> 实现

            #endregion
        }

        #endregion
        
    }
}
