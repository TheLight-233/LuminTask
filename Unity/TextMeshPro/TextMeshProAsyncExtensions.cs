
using System;
using System.Threading;
using TMPro;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static class TextMeshProAsyncExtensions
    {
        // <string> -> Text
        public static void BindTo(this ILuminTaskAsyncEnumerable<string> source, TMP_Text text, bool rebindOnError = true)
        {
            BindToCore(source, text, text.GetCancellationTokenOnDestroy(), rebindOnError).Forget();
        }

        public static void BindTo(this ILuminTaskAsyncEnumerable<string> source, TMP_Text text, CancellationToken cancellationToken, bool rebindOnError = true)
        {
            BindToCore(source, text, cancellationToken, rebindOnError).Forget();
        }

        static async LuminTask BindToCore(ILuminTaskAsyncEnumerable<string> source, TMP_Text text, CancellationToken cancellationToken, bool rebindOnError)
        {
            var repeat = false;
            BIND_AGAIN:
            var e = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (true)
                {
                    bool moveNext;
                    try
                    {
                        moveNext = await e.MoveNextAsync();
                        repeat = false;
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException) return;

                        if (rebindOnError && !repeat)
                        {
                            repeat = true;
                            goto BIND_AGAIN;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (!moveNext) return;

                    text.text = e.Current;
                }
            }
            finally
            {
                if (e != null)
                {
                    await e.DisposeAsync();
                }
            }
        }

        // <T> -> Text

        public static void BindTo<T>(this ILuminTaskAsyncEnumerable<T> source, TMP_Text text, bool rebindOnError = true)
        {
            BindToCore(source, text, text.GetCancellationTokenOnDestroy(), rebindOnError).Forget();
        }

        public static void BindTo<T>(this ILuminTaskAsyncEnumerable<T> source, TMP_Text text, CancellationToken cancellationToken, bool rebindOnError = true)
        {
            BindToCore(source, text, cancellationToken, rebindOnError).Forget();
        }

        static async LuminTask BindToCore<T>(ILuminTaskAsyncEnumerable<T> source, TMP_Text text, CancellationToken cancellationToken, bool rebindOnError)
        {
            var repeat = false; 
            
            Start:
            var e = source.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (true)
                {
                    bool moveNext;
                    try
                    {
                        moveNext = await e.MoveNextAsync();
                        repeat = false;
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException) return;

                        if (rebindOnError && !repeat)
                        {
                            repeat = true;
                            goto Start;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    if (!moveNext) return;

                    text.text = e.Current.ToString();
                }
            }
            finally
            {
                if (e != null)
                {
                    await e.DisposeAsync();
                }
            }
        }
        
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

        public interface IAsyncEndTextSelectionEventHandler<T> : IDisposable
        {
            LuminTask<T> OnEndTextSelectionAsync();
        }

        public interface IAsyncTextSelectionEventHandler<T> : IDisposable
        {
            LuminTask<T> OnTextSelectionAsync();
        }

        public interface IAsyncDeselectEventHandler<T> : IDisposable
        {
            LuminTask<T> OnDeselectAsync();
        }

        public interface IAsyncSelectEventHandler<T> : IDisposable
        {
            LuminTask<T> OnSelectAsync();
        }

        public interface IAsyncSubmitEventHandler<T> : IDisposable
        {
            LuminTask<T> OnSubmitAsync();
        }
        
        #region TMP_InputField ValueChanged 扩展

        public static UnityAsyncExtensions.IAsyncValueChangedEventHandler<string> GetAsyncValueChangedEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onValueChanged, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static UnityAsyncExtensions.IAsyncValueChangedEventHandler<string> GetAsyncValueChangedEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<string> OnValueChangedAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onValueChanged, inputField.GetCancellationTokenOnDestroy(), true).OnValueChangedAsync();
        }

        public static LuminTask<string> OnValueChangedAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onValueChanged, cancellationToken, true).OnValueChangedAsync();
        }

        #endregion

        #region TMP_InputField EndEdit 扩展

        public static UnityAsyncExtensions.IAsyncEndEditEventHandler<string> GetAsyncEndEditEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onEndEdit, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static UnityAsyncExtensions.IAsyncEndEditEventHandler<string> GetAsyncEndEditEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onEndEdit, cancellationToken, false);
        }

        public static LuminTask<string> OnEndEditAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onEndEdit, inputField.GetCancellationTokenOnDestroy(), true).OnEndEditAsync();
        }

        public static LuminTask<string> OnEndEditAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onEndEdit, cancellationToken, true).OnEndEditAsync();
        }

        #endregion

        #region TMP_InputField EndTextSelection 扩展

        public static IAsyncEndTextSelectionEventHandler<(string, int, int)> GetAsyncEndTextSelectionEventHandler(this TMP_InputField inputField)
        {
            return new AsyncTMPTextSelectionEventHandler(inputField.onEndTextSelection, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncEndTextSelectionEventHandler<(string, int, int)> GetAsyncEndTextSelectionEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncTMPTextSelectionEventHandler(inputField.onEndTextSelection, cancellationToken, false);
        }

        public static LuminTask<(string, int, int)> OnEndTextSelectionAsync(this TMP_InputField inputField)
        {
            return new AsyncTMPTextSelectionEventHandler(inputField.onEndTextSelection, inputField.GetCancellationTokenOnDestroy(), true).ToLuminTask();
        }

        public static LuminTask<(string, int, int)> OnEndTextSelectionAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncTMPTextSelectionEventHandler(inputField.onEndTextSelection, cancellationToken, true).ToLuminTask();
        }

        #endregion

        #region TMP_InputField TextSelection 扩展

        public static IAsyncTextSelectionEventHandler<(string, int, int)> GetAsyncTextSelectionEventHandler(this TMP_InputField inputField)
        {
            return new AsyncTMPTextSelectionEventHandler(inputField.onTextSelection, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncTextSelectionEventHandler<(string, int, int)> GetAsyncTextSelectionEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncTMPTextSelectionEventHandler(inputField.onTextSelection, cancellationToken, false);
        }

        public static LuminTask<(string, int, int)> OnTextSelectionAsync(this TMP_InputField inputField)
        {
            return new AsyncTMPTextSelectionEventHandler(inputField.onTextSelection, inputField.GetCancellationTokenOnDestroy(), true).ToLuminTask();
        }

        public static LuminTask<(string, int, int)> OnTextSelectionAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncTMPTextSelectionEventHandler(inputField.onTextSelection, cancellationToken, true).ToLuminTask();
        }

        #endregion

        #region TMP_InputField Deselect 扩展

        public static IAsyncDeselectEventHandler<string> GetAsyncDeselectEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onDeselect, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncDeselectEventHandler<string> GetAsyncDeselectEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onDeselect, cancellationToken, false);
        }

        public static LuminTask<string> OnDeselectAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onDeselect, inputField.GetCancellationTokenOnDestroy(), true).OnDeselectAsync();
        }

        public static LuminTask<string> OnDeselectAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onDeselect, cancellationToken, true).OnDeselectAsync();
        }

        #endregion

        #region TMP_InputField Select 扩展

        public static IAsyncSelectEventHandler<string> GetAsyncSelectEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onSelect, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncSelectEventHandler<string> GetAsyncSelectEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onSelect, cancellationToken, false);
        }

        public static LuminTask<string> OnSelectAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onSelect, inputField.GetCancellationTokenOnDestroy(), true).OnSelectAsync();
        }

        public static LuminTask<string> OnSelectAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onSelect, cancellationToken, true).OnSelectAsync();
        }

        #endregion

        #region TMP_InputField Submit 扩展

        public static IAsyncSubmitEventHandler<string> GetAsyncSubmitEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onSubmit, inputField.GetCancellationTokenOnDestroy(), false);
        }

        public static IAsyncSubmitEventHandler<string> GetAsyncSubmitEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onSubmit, cancellationToken, false);
        }

        public static LuminTask<string> OnSubmitAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onSubmit, inputField.GetCancellationTokenOnDestroy(), true).OnSubmitAsync();
        }

        public static LuminTask<string> OnSubmitAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncExtensions.AsyncUnityEventHandler<string>(inputField.onSubmit, cancellationToken, true).OnSubmitAsync();
        }

        #endregion

        #region AsyncTMPTextSelectionEventHandler（文本选择事件处理器）

        public unsafe struct AsyncTMPTextSelectionEventHandler : 
            IAsyncTextSelectionEventHandler<(string, int, int)>, 
            IAsyncEndTextSelectionEventHandler<(string, int, int)>,
            ILuminTaskSource<(string, int, int)>
        {
            private readonly TMP_InputField.TextSelectionEvent _selectionEvent;
            private readonly CancellationToken _cancellationToken;
            private readonly bool _callOnce;

            private LuminTaskSourceCore<(string, int, int)>* _core;
            private UnityEngine.Events.UnityAction<string, int, int> _continuation;
            private CancellationTokenRegistration _registration;

            public AsyncTMPTextSelectionEventHandler(
                TMP_InputField.TextSelectionEvent selectionEvent,
                CancellationToken cancellationToken,
                bool callOnce)
            {
                _selectionEvent = selectionEvent ?? throw new ArgumentNullException(nameof(selectionEvent));
                _cancellationToken = cancellationToken;
                _callOnce = callOnce;
                _core = default;
                _continuation = null;
                _registration = default;

                if (_cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _core = LuminTaskSourceCore<(string, int, int)>.Create();
                _continuation = Invoke;

                selectionEvent.AddListener(_continuation);

                if (_cancellationToken.CanBeCanceled)
                {
                    _registration = _cancellationToken.Register(static state =>
                    {
                        var self = (AsyncTMPTextSelectionEventHandler)state;
                        self.Dispose();
                    }, this);
                }
            }

            public LuminTask<(string, int, int)> OnTextSelectionAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<(string, int, int)> OnEndTextSelectionAsync()
            {
                return ToLuminTask();
            }

            public LuminTask<(string, int, int)> ToLuminTask()
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    return LuminTask.FromCanceled<(string, int, int)>(_cancellationToken);
                }

                return new LuminTask<(string, int, int)>(LuminTaskSourceCore<(string, int, int)>.MethodTable, _core, _core->Id);
            }

            private void Invoke(string text, int start, int end)
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    LuminTaskSourceCore<(string, int, int)>.TrySetResult(_core, (text, start, end));
                }
            }

            public void Dispose()
            {
                if (_continuation != null && _selectionEvent != null)
                {
                    _selectionEvent.RemoveListener(_continuation);
                    _continuation = null;
                }

                _registration.Dispose();

                if (_callOnce)
                {
                    LuminTaskSourceCore<(string, int, int)>.Dispose(_core);
                }
            }

            #region ILuminTaskSource<(string, int, int)> 实现

            public (string, int, int) GetResult(short token)
            {
                var result = LuminTaskSourceCore<(string, int, int)>.GetResultValue(_core, token);

                if (_callOnce)
                {
                    Dispose();
                }

                return result;
            }

            void ILuminTaskSource.GetResult(short token)
            {
                GetResult(token);
            }

            public LuminTaskStatus GetStatus(short token)
            {
                return LuminTaskSourceCore<(string, int, int)>.GetStatus(_core, token);
            }

            public LuminTaskStatus UnsafeGetStatus()
            {
                return LuminTaskSourceCore<(string, int, int)>.UnsafeGetStatus(_core);
            }

            public void OnCompleted(Action<object> continuation, object state, short token)
            {
                LuminTaskSourceCore<(string, int, int)>.OnCompleted(_core, continuation, state, token);
            }

            #endregion
        }

        #endregion
    }
}
