using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.Utility;

namespace LuminThread.TaskSource.Promise;

// 极简单消费者无界队列，只服务 WhenEach 的“任务完成回调(多生产者) → 枚举器(单消费者)”模式。
//
// 语义对齐原 SingleConsumerUnboundedLuminChannel：完成后不再接受写入、剩余条目仍可读取、
// 队列为空时完成信号以 false 唤醒等待者、同一时刻只允许一个挂起等待。
// 去掉公开通道层级、错误传播分支与终结器后，核心不再实例化 LuminChannel<WhenEachResult<T>>
// 一族泛型（公开通道本身已迁往 LuminTask.AsyncEx，供用户直接使用）。
internal sealed class WhenEachQueue<T>
{
    static readonly Action<object> CancellationCallbackDelegate = CancellationCallback;

    readonly Queue<T> _items = new Queue<T>();

    bool _completed;
    LuminTaskHandle _waiter;
    CancellationToken _waiterToken;
    CancellationTokenRegistration _waiterRegistration;

    public bool TryWrite(T item)
    {
        bool signal;
        lock (_items)
        {
            if (_completed) return false;

            _items.Enqueue(item);
            signal = _waiter != 0;
        }

        if (signal) CompleteWaiter(true);
        return true;
    }

    public void TryComplete()
    {
        bool signal;
        lock (_items)
        {
            if (_completed) return;
            _completed = true;
            signal = _waiter != 0 && _items.Count == 0;
        }

        if (signal) CompleteWaiter(false);
    }

    public bool TryRead(out T item)
    {
        lock (_items)
        {
            if (_items.Count != 0)
            {
                item = _items.Dequeue();
                return true;
            }
        }

        item = default!;
        return false;
    }

    public LuminTask<bool> WaitToReadAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return LuminTask.FromCanceled<bool>(cancellationToken);
        }

        lock (_items)
        {
            if (_items.Count != 0) return CompletedTasks.True();
            if (_completed) return CompletedTasks.False();
            if (_waiter != 0) throw new InvalidOperationException("Only one pending WaitToReadAsync operation is allowed.");

            var handle = LuminTask.RentSlot();

            _waiter = handle;
            _waiterToken = cancellationToken;
            if (cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.RegisterWithoutCaptureExecutionContext(CancellationCallbackDelegate, this);
                if (_waiter != 0)
                    _waiterRegistration = registration;
                else
                    registration.Dispose();
            }

            return new LuminTask<bool>(handle);
        }
    }

    static void CancellationCallback(object state) => ((WhenEachQueue<T>)state).CancelWaiter();

    void CompleteWaiter(bool result)
    {
        var handle = TakeWaiter(out _);
        if (handle == 0) return;

        LuminTaskResultBridge.TrySetResult(handle, in result);
    }

    void CancelWaiter()
    {
        var handle = TakeWaiter(out var token);
        if (handle == 0) return;

        LuminTaskCoreSource.TrySetCanceled(handle, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    LuminTaskHandle TakeWaiter(out CancellationToken token)
    {
        LuminTaskHandle handle;
        lock (_items)
        {
            handle = _waiter;
            _waiter = 0;
            token = _waiterToken;
            _waiterToken = default;
        }

        if (handle != 0) _waiterRegistration.Release();
        return handle;
    }
}