
using System;
using System.Threading;
using TMPro;
using LuminThread.TaskSource;
using LuminThread.Utility;
using LuminThread.Interface;

namespace LuminThread.Unity
{
    public static class TextMeshProAsyncLuminTask
    {
        // <string> -> Text
        public static void BindTo(this ILuminTaskAsyncEnumerable<string> source, TMP_Text text, bool rebindOnError = true)
        {
            BindToCore(source, text, text.GetOnDestroyCancellationToken(), rebindOnError).Forget();
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
            BindToCore(source, text, text.GetOnDestroyCancellationToken(), rebindOnError).Forget();
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
        
        public interface IAsyncEndTextSelectionLuminTaskEventHandler<T> : IDisposable
        {
            LuminTask<T> OnEndTextSelectionAsync();
        }

        public interface IAsyncTextSelectionLuminTaskEventHandler<T> : IDisposable
        {
            LuminTask<T> OnTextSelectionAsync();
        }

        public interface IAsyncDeselectLuminTaskEventHandler<T> : IDisposable
        {
            LuminTask<T> OnDeselectAsync();
        }

        public interface IAsyncSelectLuminTaskEventHandler<T> : IDisposable
        {
            LuminTask<T> OnSelectAsync();
        }

        public interface IAsyncSubmitLuminTaskEventHandler<T> : IDisposable
        {
            LuminTask<T> OnSubmitAsync();
        }
        
        #region TMP_InputField ValueChanged 扩展

        public static UnityAsyncLuminTask.IAsyncValueChangedLuminTaskEventHandler<string> GetAsyncValueChangedEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onValueChanged, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static UnityAsyncLuminTask.IAsyncValueChangedLuminTaskEventHandler<string> GetAsyncValueChangedEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onValueChanged, cancellationToken, false);
        }

        public static LuminTask<string> OnValueChangedAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onValueChanged, inputField.GetOnDestroyCancellationToken(), true).OnValueChangedAsync();
        }

        public static LuminTask<string> OnValueChangedAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onValueChanged, cancellationToken, true).OnValueChangedAsync();
        }

        #endregion

        #region TMP_InputField EndEdit 扩展

        public static UnityAsyncLuminTask.IAsyncEndEditLuminTaskEventHandler<string> GetAsyncEndEditEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onEndEdit, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static UnityAsyncLuminTask.IAsyncEndEditLuminTaskEventHandler<string> GetAsyncEndEditEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onEndEdit, cancellationToken, false);
        }

        public static LuminTask<string> OnEndEditAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onEndEdit, inputField.GetOnDestroyCancellationToken(), true).OnEndEditAsync();
        }

        public static LuminTask<string> OnEndEditAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onEndEdit, cancellationToken, true).OnEndEditAsync();
        }

        #endregion

        #region TMP_InputField EndTextSelection 扩展

        public static IAsyncEndTextSelectionLuminTaskEventHandler<(string, int, int)> GetAsyncEndTextSelectionEventHandler(this TMP_InputField inputField)
        {
            return new AsyncTmpTextSelectionLuminTaskEventHandler(inputField.onEndTextSelection, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncEndTextSelectionLuminTaskEventHandler<(string, int, int)> GetAsyncEndTextSelectionEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncTmpTextSelectionLuminTaskEventHandler(inputField.onEndTextSelection, cancellationToken, false);
        }

        public static LuminTask<(string, int, int)> OnEndTextSelectionAsync(this TMP_InputField inputField)
        {
            return new AsyncTmpTextSelectionLuminTaskEventHandler(inputField.onEndTextSelection, inputField.GetOnDestroyCancellationToken(), true).ToLuminTask();
        }

        public static LuminTask<(string, int, int)> OnEndTextSelectionAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncTmpTextSelectionLuminTaskEventHandler(inputField.onEndTextSelection, cancellationToken, true).ToLuminTask();
        }

        #endregion

        #region TMP_InputField TextSelection 扩展

        public static IAsyncTextSelectionLuminTaskEventHandler<(string, int, int)> GetAsyncTextSelectionEventHandler(this TMP_InputField inputField)
        {
            return new AsyncTmpTextSelectionLuminTaskEventHandler(inputField.onTextSelection, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncTextSelectionLuminTaskEventHandler<(string, int, int)> GetAsyncTextSelectionEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncTmpTextSelectionLuminTaskEventHandler(inputField.onTextSelection, cancellationToken, false);
        }

        public static LuminTask<(string, int, int)> OnTextSelectionAsync(this TMP_InputField inputField)
        {
            return new AsyncTmpTextSelectionLuminTaskEventHandler(inputField.onTextSelection, inputField.GetOnDestroyCancellationToken(), true).ToLuminTask();
        }

        public static LuminTask<(string, int, int)> OnTextSelectionAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new AsyncTmpTextSelectionLuminTaskEventHandler(inputField.onTextSelection, cancellationToken, true).ToLuminTask();
        }

        #endregion

        #region TMP_InputField Deselect 扩展

        public static IAsyncDeselectLuminTaskEventHandler<string> GetAsyncDeselectEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onDeselect, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncDeselectLuminTaskEventHandler<string> GetAsyncDeselectEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onDeselect, cancellationToken, false);
        }

        public static LuminTask<string> OnDeselectAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onDeselect, inputField.GetOnDestroyCancellationToken(), true).OnDeselectAsync();
        }

        public static LuminTask<string> OnDeselectAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onDeselect, cancellationToken, true).OnDeselectAsync();
        }

        #endregion

        #region TMP_InputField Select 扩展

        public static IAsyncSelectLuminTaskEventHandler<string> GetAsyncSelectEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onSelect, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncSelectLuminTaskEventHandler<string> GetAsyncSelectEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onSelect, cancellationToken, false);
        }

        public static LuminTask<string> OnSelectAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onSelect, inputField.GetOnDestroyCancellationToken(), true).OnSelectAsync();
        }

        public static LuminTask<string> OnSelectAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onSelect, cancellationToken, true).OnSelectAsync();
        }

        #endregion

        #region TMP_InputField Submit 扩展

        public static IAsyncSubmitLuminTaskEventHandler<string> GetAsyncSubmitEventHandler(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onSubmit, inputField.GetOnDestroyCancellationToken(), false);
        }

        public static IAsyncSubmitLuminTaskEventHandler<string> GetAsyncSubmitEventHandler(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onSubmit, cancellationToken, false);
        }

        public static LuminTask<string> OnSubmitAsync(this TMP_InputField inputField)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onSubmit, inputField.GetOnDestroyCancellationToken(), true).OnSubmitAsync();
        }

        public static LuminTask<string> OnSubmitAsync(this TMP_InputField inputField, CancellationToken cancellationToken)
        {
            return new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(inputField.onSubmit, cancellationToken, true).OnSubmitAsync();
        }

        #endregion

        #region AsyncTmpTextSelectionLuminTaskEventHandler（文本选择事件处理器）

        public sealed class AsyncTmpTextSelectionLuminTaskEventHandler : 
            IAsyncTextSelectionLuminTaskEventHandler<(string, int, int)>, 
            IAsyncEndTextSelectionLuminTaskEventHandler<(string, int, int)>
        {
            private readonly TMP_InputField.TextSelectionEvent _selectionEvent;
            private readonly CancellationToken _cancellationToken;
            private readonly bool _callOnce;

            private ulong _handle;
            private UnityEngine.Events.UnityAction<string, int, int> _continuation;
            private CancellationTokenRegistration _registration;
            private bool _disposed;

            public AsyncTmpTextSelectionLuminTaskEventHandler(
                TMP_InputField.TextSelectionEvent selectionEvent,
                CancellationToken cancellationToken,
                bool callOnce)
            {
                _selectionEvent = selectionEvent ?? throw new ArgumentNullException(nameof(selectionEvent));
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

                selectionEvent.AddListener(_continuation);

                if (_cancellationToken.CanBeCanceled)
                {
                    var registration = _cancellationToken.Register(static state =>
                    {
                        var self = (AsyncTmpTextSelectionLuminTaskEventHandler)state;
                        self.DisposeFromCancellation();
                    }, this);
                    if (_disposed)
                        registration.Dispose();
                    else
                        _registration = registration;
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

                if (_disposed)
                    return LuminTask.FromCanceled<(string, int, int)>(new CancellationToken(canceled: true));

                if (_handle == 0)
                    LuminTaskSource.Rent<(string, int, int)>(out _handle);

                return LuminTaskSource.ToTask<(string, int, int)>(_handle);
            }

            private void Invoke(string text, int start, int end)
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

                    LuminTaskSource.SetResult(handle, (text, start, end));
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
                if (_continuation != null && _selectionEvent != null)
                {
                    _selectionEvent.RemoveListener(_continuation);
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
    }
}
