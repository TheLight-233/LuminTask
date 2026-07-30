using System;
using System.Threading;

namespace LuminThread.Utility;

using System.Runtime.CompilerServices;

#if NET8_0_OR_GREATER
public interface IPooledObjectPolicy<T> where T : class
{
    static abstract T Create();
    static abstract bool Return(T obj);
}
#else
public interface IPooledObjectPolicy<T> where T : class
{
    T Create();
    bool Return(T obj);
}
#endif

public sealed class ObjectPool<T>
#if NET8_0_OR_GREATER
    where T : class, IPooledObjectPolicy<T>, IDisposable
#else
    where T : class, IDisposable
#endif
{
    private readonly object _gate = new();
    private T?[] _items;
    private T? _fastItem;
    private int _count;
    private int _maxSize;

#if !NET8_0_OR_GREATER
    private readonly IPooledObjectPolicy<T> _policy;
#endif

    public ObjectPool(
#if !NET8_0_OR_GREATER
        IPooledObjectPolicy<T> policy,
#endif
        int maxSize = 16)
    {
        if (maxSize < 0) throw new ArgumentOutOfRangeException(nameof(maxSize));

        // One item gets a lock-free fast slot; the array is only the contended fallback.
        _items = new T?[Math.Max(0, maxSize - 1)];
        _count = 0;
        _maxSize = maxSize;

#if !NET8_0_OR_GREATER
        _policy = policy;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Rent()
    {
        var fastItem = Volatile.Read(ref _fastItem);
        if (fastItem != null &&
            ReferenceEquals(Interlocked.CompareExchange(ref _fastItem, null, fastItem), fastItem))
        {
            return fastItem;
        }

        lock (_gate)
        {
            if (_count > 0)
            {
                var item = _items[_count - 1];
                _items[_count - 1] = null;
                _count--;
                return item!;
            }
        }

#if NET8_0_OR_GREATER
        return T.Create();
#else
        return _policy.Create();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));

#if NET8_0_OR_GREATER
        if (!T.Return(item))
#else
        if (!_policy.Return(item))
#endif
        {
            item.Dispose();
            return;
        }

        if (Volatile.Read(ref _maxSize) == 0)
        {
            item.Dispose();
            return;
        }

        if (Volatile.Read(ref _fastItem) is null &&
            Interlocked.CompareExchange(ref _fastItem, item, null) is null)
        {
            // Resize(0) can race this CAS. Recheck capacity and retract the item if it still
            // belongs to this return operation; a concurrent renter may already own it.
            if (Volatile.Read(ref _maxSize) != 0)
                return;

            if (ReferenceEquals(Interlocked.CompareExchange(ref _fastItem, null, item), item))
                item.Dispose();
            return;
        }

        lock (_gate)
        {
            if (_count < _items.Length)
            {
                _items[_count] = item;
                _count++;
                return;
            }
        }

        item.Dispose();
    }

    public static void ClearThreadLocalCache()
    {
        // Kept for source compatibility. Pools now use an instance-owned lock-free fast slot,
        // avoiding cross-pool item migration caused by the previous static thread-local cache.
    }

    public int AvailableCount => Volatile.Read(ref _count) + (Volatile.Read(ref _fastItem) is null ? 0 : 1);
    public int MaxSize => Volatile.Read(ref _maxSize);

    public void Resize(int newSize)
    {
        if (newSize < 0) throw new ArgumentOutOfRangeException(nameof(newSize));

        T?[] retiredItems;
        lock (_gate)
        {
            var newItems = new T?[Math.Max(0, newSize - 1)];
            var itemsToCopy = Math.Min(_count, newItems.Length);
            Array.Copy(_items, newItems, itemsToCopy);

            var retiredCount = _count - itemsToCopy;
            retiredItems = new T?[retiredCount];
            if (retiredCount > 0)
                Array.Copy(_items, itemsToCopy, retiredItems, 0, retiredCount);

            _items = newItems;
            _count = itemsToCopy;
            Volatile.Write(ref _maxSize, newSize);
        }

        foreach (var retiredItem in retiredItems)
            retiredItem?.Dispose();

        if (newSize == 0)
            Interlocked.Exchange(ref _fastItem, null)?.Dispose();
    }

    public T?[] GetAllItems()
    {
        lock (_gate)
        {
            var fastItem = Volatile.Read(ref _fastItem);
            var result = new T?[_count + (fastItem is null ? 0 : 1)];
            var offset = 0;
            if (fastItem is not null)
                result[offset++] = fastItem;
            Array.Copy(_items, 0, result, offset, _count);
            return result;
        }
    }

}
