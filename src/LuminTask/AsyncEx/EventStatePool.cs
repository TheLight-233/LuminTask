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
    /// </summary>
    internal static class EventStatePool<TState>
        where TState : class, IEventPoolableState, new()
    {
        private static readonly object Gate = new();
        private static TState?[] _items = Array.Empty<TState?>();
        private static TState? _fastItem;
        private static int _count;
        private static int _maxSize = Math.Max(8, Environment.ProcessorCount * 2);

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
                    var item = _items[--_count];
                    _items[_count] = null;
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
                if (_count >= _maxSize)
                    return;

                if (_count == _items.Length)
                    Array.Resize(ref _items, Math.Max(4, _items.Length * 2));

                _items[_count++] = state;
            }
        }
    }
}
