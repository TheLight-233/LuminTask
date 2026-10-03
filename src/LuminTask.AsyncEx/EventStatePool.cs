using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LuminThread.AsyncEx
{
    /// <summary>事件状态对象池化所需的复用钩子。</summary>
    internal interface IEventPoolableState
    {
        void ResetForReuse();
        void BumpGeneration();
    }

    /// <summary>
    /// 事件状态的有界对象池：无锁快槽 + 数组回退。
    /// 归还时先递增代际，再入池；旧句柄的代际与当前状态不一致即判定为过期，
    /// 避免对已复用给新事件的同一状态对象进行操作。
    /// 所有静态字段都是零初始化的：没有非常量初始化器就没有按 TState 生成的静态构造函数
    /// （IL2CPP 下即每个闭合类型少一个初始化函数和一条注册），锁与数组改为首次回退时惰性创建。
    /// </summary>
    internal static class EventStatePool<TState>
        where TState : class, IEventPoolableState, new()
    {
        // 固定上限，替代原来的 Math.Max(8, ProcessorCount * 2)：非常量初始化器会带来 per-TState 的
        // 静态构造；16 与原实现在 8 核机器上的取值一致。
        private const int MaxSize = 16;

        private static TState?[]? _items;
        private static TState? _fastItem;
        private static int _count;
        private static object? _gate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TState Rent()
        {
            var fast = Interlocked.Exchange(ref _fastItem, null);
            if (fast != null)
                return fast;

            lock (Gate)
            {
                if (_count > 0)
                {
                    var items = _items!;
                    var item = items[--_count];
                    items[_count] = null;
                    return item!;
                }
            }

            return new TState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Return(TState state)
        {
            state.ResetForReuse();
            state.BumpGeneration();

            if (Interlocked.CompareExchange(ref _fastItem, state, null) == null)
                return;

            lock (Gate)
            {
                if (_count >= MaxSize)
                    return;

                var items = _items;
                if (items == null || _count == items.Length)
                {
                    Array.Resize(ref _items, Math.Max(4, (items?.Length ?? 0) * 2));
                    items = _items;
                }

                items![_count++] = state;
            }
        }

        // 惰性创建且只创建一次：CAS 失败者用胜者的实例，所以所有线程最终锁在同一个对象上。
        private static object Gate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var gate = Volatile.Read(ref _gate);
                if (gate != null)
                    return gate;

                return Interlocked.CompareExchange(ref _gate, new object(), null) ?? _gate!;
            }
        }
    }
}