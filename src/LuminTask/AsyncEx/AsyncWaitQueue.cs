using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;

namespace LuminThread.AsyncEx;

public interface IAsyncWaitQueue<T>
{
    bool IsEmpty { get; }
    LuminTask<T> Enqueue();
    void Dequeue(T? result = default);
    bool TryDequeue(T? result = default);
    void DequeueAll(T? result = default);
    bool TryCancel(LuminTask task, CancellationToken cancellationToken);
    void CancelAll(CancellationToken cancellationToken);
}

internal sealed unsafe class DefaultAsyncWaitQueue<T> : IAsyncWaitQueue<T>
{
    private readonly LuminDeque<WaitNode> _queue = new();
    private readonly object _queueLock = new object();

    private class WaitNode
    {
        public IntPtr CorePtr;
        public CancellationTokenRegistration? Registration;
    }

    public bool IsEmpty
    {
        get { lock (_queueLock) return _queue.Count == 0; }
    }

    public LuminTask<T> Enqueue()
    {
        lock (_queueLock)
        {
            var core = LuminTaskSourceCore<T>.Create();
            var node = new WaitNode { CorePtr = new IntPtr(core) };
            _queue.PushBack(node);
            return new LuminTask<T>(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, core, core->Id);
        }
    }

    public void Dequeue(T? result = default)
    {
        WaitNode node;
        lock (_queueLock)
        {
            if (_queue.Count == 0) return;
            node = _queue.PopFront();
        }

        node.Registration?.Dispose();

        var ptr = node.CorePtr.ToPointer();
        LuminTaskSourceCore<T>.TrySetResult(ptr, result);
    }

    public bool TryDequeue(T? result = default)
    {
        WaitNode node;
        lock (_queueLock)
        {
            if (_queue.Count == 0) return false;
            node = _queue.PopFront();
        }

        node.Registration?.Dispose();

        var ptr = node.CorePtr.ToPointer();
        LuminTaskSourceCore<T>.TrySetResult(ptr, result);
        return true;
    }

    public void DequeueAll(T? result = default)
    {
        WaitNode[] nodes;
        lock (_queueLock)
        {
            if (_queue.Count == 0) return;

            nodes = new WaitNode[_queue.Count];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = _queue.PopFront();
            }
        }

        foreach (var node in nodes)
        {
            node.Registration?.Dispose();

            var ptr = node.CorePtr.ToPointer();
            LuminTaskSourceCore<T>.TrySetResult(ptr, result);
        }
    }

    public bool TryCancel(LuminTask task, CancellationToken cancellationToken)
    {
        WaitNode node = null;

        lock (_queueLock)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].CorePtr.ToPointer() == task._taskSource)
                {
                    node = _queue[i];
                    _queue.RemoveAt(i);
                    break;
                }
            }
        }

        if (node != null)
        {
            node.Registration?.Dispose();

            var ptr = node.CorePtr.ToPointer();
            LuminTaskSourceCore<T>.TrySetCanceled(ptr);
            return true;
        }

        return false;
    }

    public void CancelAll(CancellationToken cancellationToken)
    {
        WaitNode[] nodes;
        lock (_queueLock)
        {
            if (_queue.Count == 0) return;

            nodes = new WaitNode[_queue.Count];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = _queue.PopFront();
            }
        }

        foreach (var node in nodes)
        {
            node.Registration?.Dispose();

            var ptr = node.CorePtr.ToPointer();
            LuminTaskSourceCore<T>.TrySetCanceled(ptr);
        }
    }

    public LuminTask<T> EnqueueWithCancellation(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return LuminTask.FromCanceled<T>(cancellationToken);
        }

        WaitNode node;
        LuminTask<T> task;

        lock (_queueLock)
        {
            var core = LuminTaskSourceCore<T>.Create();
            node = new WaitNode { CorePtr = new IntPtr(core) };
            _queue.PushBack(node);
            task = new LuminTask<T>(LuminTaskSourceCore<T>.MethodTableAutoDisposePtr, core, core->Id);
        }

        if (cancellationToken.CanBeCanceled)
        {
            node.Registration = cancellationToken.Register(() =>
            {
                TryCancel(task, cancellationToken);
            }, useSynchronizationContext: false);
        }

        return task;
    }
}

internal static class AsyncWaitQueueExtensions
{
    public static LuminTask<T> Enqueue<T>(this IAsyncWaitQueue<T> queue, CancellationToken cancellationToken)
    {
        if (queue is DefaultAsyncWaitQueue<T> defaultQueue)
        {
            return defaultQueue.EnqueueWithCancellation(cancellationToken);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return LuminTask.FromCanceled<T>(cancellationToken);
        }

        var task = queue.Enqueue();

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                queue.TryCancel(task, cancellationToken);
            }, useSynchronizationContext: false);
        }

        return task;
    }
}

public sealed class LuminDeque<T> : IDisposable, IEnumerable<T>
{
    #region Nested Types

    private struct Iterator
    {
        public int CurIndex;
        public int FirstIndex;
        public int LastIndex;
        public int NodeIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNode(int newNodeIndex, int blockSize)
        {
            NodeIndex = newNodeIndex;
            FirstIndex = 0;
            LastIndex = blockSize;
        }
    }

    #endregion

    private T[][] _map;
    private int _mapSize;
    private Iterator _start;
    private Iterator _finish;
    private int _allocatedStart;
    private int _allocatedEnd;
    private readonly int _blockSize;

    private const int INITIAL_MAP_SIZE = 8;
    private const int MIN_CAPACITY = 4;
    private const int BUFFER_SIZE = 512;

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            long nodeCount = _finish.NodeIndex - _start.NodeIndex;
            return (int)(nodeCount * _blockSize) - (_start.CurIndex - _start.FirstIndex) +
                   (_finish.CurIndex - _finish.FirstIndex);
        }
    }

    public bool IsCreated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _map != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateBlockSize()
    {
        int elementSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
        return elementSize < BUFFER_SIZE ? BUFFER_SIZE / elementSize : 1;
    }

    public LuminDeque() : this(MIN_CAPACITY)
    {
    }

    public LuminDeque(int capacity)
    {
        _blockSize = CalculateBlockSize();

        if (capacity < MIN_CAPACITY) capacity = MIN_CAPACITY;

        int numBlocks = capacity / _blockSize + 1;
        _mapSize = Math.Max(INITIAL_MAP_SIZE, numBlocks + 2);

        _map = new T[_mapSize][];

        int startNode = (_mapSize - numBlocks) / 2;
        _allocatedStart = startNode;
        _allocatedEnd = startNode + numBlocks;

        for (int i = startNode; i < _allocatedEnd; i++)
        {
            _map[i] = new T[_blockSize];
        }

        _start.SetNode(startNode, _blockSize);
        _start.CurIndex = _start.FirstIndex;

        _finish.SetNode(startNode, _blockSize);
        _finish.CurIndex = _finish.FirstIndex;
    }

    public LuminDeque(LuminDeque<T> other)
    {
        _blockSize = other._blockSize;

        int size = other.Count;
        if (size == 0) size = MIN_CAPACITY;

        int numBlocks = size / _blockSize + 1;
        _mapSize = Math.Max(INITIAL_MAP_SIZE, numBlocks + 2);

        _map = new T[_mapSize][];

        int startNode = (_mapSize - numBlocks) / 2;
        _allocatedStart = startNode;
        _allocatedEnd = startNode + numBlocks;

        for (int i = startNode; i < _allocatedEnd; i++)
        {
            _map[i] = new T[_blockSize];
        }

        _start.SetNode(startNode, _blockSize);
        _start.CurIndex = _start.FirstIndex;
        _finish.SetNode(startNode, _blockSize);
        _finish.CurIndex = _finish.FirstIndex;

        if (other.Count > 0)
        {
            Iterator it = other._start;
            while (it.CurIndex != other._finish.CurIndex || it.NodeIndex != other._finish.NodeIndex)
            {
                _map[_finish.NodeIndex][_finish.CurIndex] = other._map[it.NodeIndex][it.CurIndex];
                _finish.CurIndex++;

                if (_finish.CurIndex == _finish.LastIndex)
                {
                    _finish.SetNode(_finish.NodeIndex + 1, _blockSize);
                    _finish.CurIndex = _finish.FirstIndex;
                }

                it.CurIndex++;
                if (it.CurIndex == it.LastIndex)
                {
                    it.SetNode(it.NodeIndex + 1, _blockSize);
                    it.CurIndex = it.FirstIndex;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T[] AllocateBlock()
    {
        return new T[_blockSize];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ResetToCenter()
    {

        int centerNode = _mapSize / 2;

        if (centerNode < _allocatedStart || centerNode >= _allocatedEnd)
        {
            if (_map[centerNode] == null)
            {
                _map[centerNode] = AllocateBlock();
            }

            if (centerNode < _allocatedStart)
                _allocatedStart = centerNode;
            if (centerNode >= _allocatedEnd)
                _allocatedEnd = centerNode + 1;
        }

        _start.SetNode(centerNode, _blockSize);
        _start.CurIndex = _start.FirstIndex;
        _finish.SetNode(centerNode, _blockSize);
        _finish.CurIndex = _finish.FirstIndex;
    }

    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int cur = _start.CurIndex + index;
            int last = _start.LastIndex;

            if (cur < last)
                return _map[_start.NodeIndex][cur];

            int offset = (_start.CurIndex - _start.FirstIndex) + index;
            int blockSize = _blockSize;
            int nodeOffset = offset / blockSize;
            int elemOffset = offset - nodeOffset * blockSize;
            return _map[_start.NodeIndex + nodeOffset][elemOffset];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            int cur = _start.CurIndex + index;
            int last = _start.LastIndex;

            if (cur < last)
            {
                _map[_start.NodeIndex][cur] = value;
                return;
            }

            int offset = (_start.CurIndex - _start.FirstIndex) + index;
            int blockSize = _blockSize;
            int nodeOffset = offset / blockSize;
            int elemOffset = offset - nodeOffset * blockSize;
            _map[_start.NodeIndex + nodeOffset][elemOffset] = value;
        }
    }

    public T Front
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _map[_start.NodeIndex][_start.CurIndex];
    }

    public T Back
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            int cur = _finish.CurIndex;
            int first = _finish.FirstIndex;
            if (cur != first)
                return _map[_finish.NodeIndex][cur - 1];

            int prevNode = _finish.NodeIndex - 1;
            return _map[prevNode][_blockSize - 1];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushBack(T item)
    {
        int cur = _finish.CurIndex;

        if (cur == _start.CurIndex && _start.NodeIndex == _finish.NodeIndex)
        {

            int centerNode = _mapSize >> 1;
            if (_start.NodeIndex != centerNode)
            {
                ResetToCenter();
                cur = _finish.CurIndex;
            }
        }

        _map[_finish.NodeIndex][cur++] = item;
        _finish.CurIndex = cur;

        if (cur == _finish.LastIndex)
        {
            int nextNode = _finish.NodeIndex + 1;

            if (nextNode >= _mapSize)
            {
                ReallocateMap(1, false);
                nextNode = _finish.NodeIndex + 1;
            }

            if (nextNode >= _allocatedEnd)
            {
                _map[nextNode] = AllocateBlock();
                _allocatedEnd = nextNode + 1;
            }

            _finish.NodeIndex = nextNode;
            _finish.FirstIndex = 0;
            _finish.LastIndex = _blockSize;
            _finish.CurIndex = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushFront(T item)
    {
        int cur = _start.CurIndex;

        if (cur == _finish.CurIndex && _start.NodeIndex == _finish.NodeIndex)
        {

            int centerNode = _mapSize >> 1;
            if (_start.NodeIndex != centerNode)
            {
                ResetToCenter();
                cur = _start.CurIndex;
            }
        }

        if (cur != _start.FirstIndex)
        {
            _start.CurIndex = --cur;
            _map[_start.NodeIndex][cur] = item;
            return;
        }

        int prevNode = _start.NodeIndex - 1;

        if (prevNode < 0)
        {
            ReallocateMap(1, true);
            prevNode = _start.NodeIndex - 1;
        }

        if (prevNode < _allocatedStart)
        {
            _map[prevNode] = AllocateBlock();
            _allocatedStart = prevNode;
        }

        _start.NodeIndex = prevNode;
        _start.FirstIndex = 0;
        _start.LastIndex = _blockSize;
        cur = _blockSize - 1;
        _start.CurIndex = cur;
        _map[_start.NodeIndex][cur] = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushBackFast(T item)
    {
        int cur = _finish.CurIndex;
        _map[_finish.NodeIndex][cur] = item;
        cur++;

        if (cur != _finish.LastIndex)
        {
            _finish.CurIndex = cur;
        }
        else
        {
            _finish.CurIndex = cur;
            int nextNode = _finish.NodeIndex + 1;

            if (nextNode >= _mapSize)
            {
                ReallocateMap(1, false);
                nextNode = _finish.NodeIndex + 1;
            }

            if (nextNode >= _allocatedEnd)
            {
                _map[nextNode] = AllocateBlock();
                _allocatedEnd = nextNode + 1;
            }

            _finish.NodeIndex = nextNode;
            _finish.FirstIndex = 0;
            _finish.LastIndex = _blockSize;
            _finish.CurIndex = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushFrontFast(T item)
    {
        int cur = _start.CurIndex;
        int first = _start.FirstIndex;

        if (cur != first)
        {
            cur--;
            _map[_start.NodeIndex][cur] = item;
            _start.CurIndex = cur;
        }
        else
        {
            int prevNode = _start.NodeIndex - 1;

            if (prevNode < 0)
            {
                ReallocateMap(1, true);
                prevNode = _start.NodeIndex - 1;
            }

            if (prevNode < _allocatedStart)
            {
                _map[prevNode] = AllocateBlock();
                _allocatedStart = prevNode;
            }

            _start.NodeIndex = prevNode;
            first = 0;
            _start.FirstIndex = first;
            _start.LastIndex = first + _blockSize;
            cur = first + _blockSize - 1;
            _map[_start.NodeIndex][cur] = item;
            _start.CurIndex = cur;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PopBackFast()
    {
        int cur = _finish.CurIndex;
        int first = _finish.FirstIndex;

        if (cur != first)
        {
            cur--;
            _finish.CurIndex = cur;
            return _map[_finish.NodeIndex][cur];
        }

        int prevNode = _finish.NodeIndex - 1;
        _finish.NodeIndex = prevNode;
        first = 0;
        _finish.FirstIndex = first;
        cur = first + _blockSize;
        _finish.LastIndex = cur;
        cur--;
        _finish.CurIndex = cur;
        return _map[_finish.NodeIndex][cur];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PopFrontFast()
    {
        int cur = _start.CurIndex;
        T result = _map[_start.NodeIndex][cur];
        cur++;

        if (cur != _start.LastIndex)
        {
            _start.CurIndex = cur;
        }
        else
        {
            _start.CurIndex = cur;
            int nextNode = _start.NodeIndex + 1;
            _start.NodeIndex = nextNode;
            int first = 0;
            _start.FirstIndex = first;
            _start.LastIndex = first + _blockSize;
            _start.CurIndex = first;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCentered()
    {
        if (_start.NodeIndex == _finish.NodeIndex && _start.CurIndex == _finish.CurIndex)
        {
            int centerNode = _mapSize >> 1;
            if (_start.NodeIndex != centerNode)
            {
                ResetToCenter();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PopBack()
    {
        int cur = _finish.CurIndex;
        if (cur != _finish.FirstIndex)
        {
            --cur;
            _finish.CurIndex = cur;
            return _map[_finish.NodeIndex][cur];
        }

        int prevNode = _finish.NodeIndex - 1;
        _finish.NodeIndex = prevNode;
        int first = 0;
        _finish.FirstIndex = first;
        cur = first + _blockSize;
        _finish.LastIndex = cur;
        --cur;
        _finish.CurIndex = cur;
        return _map[_finish.NodeIndex][cur];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PopFront()
    {
        int cur = _start.CurIndex;
        T result = _map[_start.NodeIndex][cur++];
        _start.CurIndex = cur;

        if (cur == _start.LastIndex)
        {
            int nextNode = _start.NodeIndex + 1;
            _start.NodeIndex = nextNode;
            int first = 0;
            _start.FirstIndex = first;
            _start.LastIndex = first + _blockSize;
            _start.CurIndex = first;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (_start.NodeIndex == _finish.NodeIndex && _start.CurIndex == _finish.CurIndex)
            return;

        ResetToCenter();
    }

    private void ReallocateMap(int nodesToAdd, bool addAtFront)
    {
        int oldNodeCount = (_finish.NodeIndex - _start.NodeIndex) + 1;
        int newNodeCount = oldNodeCount + nodesToAdd;

        int newStartNode;
        if (_mapSize > (newNodeCount << 1))
        {
            newStartNode = (_mapSize - newNodeCount) >> 1 + (addAtFront ? nodesToAdd : 0);

            int copyCount = oldNodeCount;
            int src = _start.NodeIndex;
            int dst = newStartNode;

            if (dst < src)
            {
                Array.Copy(_map, src, _map, dst, copyCount);
            }
            else
            {
                while (copyCount-- > 0)
                    _map[dst + copyCount] = _map[src + copyCount];
            }

            int offset = newStartNode - src;
            _allocatedStart += offset;
            _allocatedEnd += offset;
        }
        else
        {
            int newMapSize = _mapSize + Math.Max(_mapSize, nodesToAdd) + 2;

            T[][] newMap = new T[newMapSize][];

            newStartNode = (newMapSize - newNodeCount) >> 1 + (addAtFront ? nodesToAdd : 0);

            Array.Copy(_map, _start.NodeIndex, newMap, newStartNode, oldNodeCount);

            int oldStart = _start.NodeIndex;
            int newStart = newStartNode;
            int rangeSize = _allocatedEnd - _allocatedStart;
            _allocatedStart = newStart - (oldStart - _allocatedStart);
            _allocatedEnd = _allocatedStart + rangeSize;

            _map = newMap;
            _mapSize = newMapSize;
        }

        _start.SetNode(newStartNode, _blockSize);
        _finish.SetNode(newStartNode + oldNodeCount - 1, _blockSize);
    }

    public void Dispose()
    {
        _map = null;
        _mapSize = 0;
        _start = default;
        _finish = default;
        _allocatedStart = 0;
        _allocatedEnd = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPopBack(out T result)
    {
        if (Count == 0)
        {
            result = default;
            return false;
        }

        result = PopBack();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPopFront(out T result)
    {
        if (Count == 0)
        {
            result = default;
            return false;
        }

        result = PopFront();
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekBack(out T result)
    {
        if (Count == 0)
        {
            result = default;
            return false;
        }

        result = Back;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeekFront(out T result)
    {
        if (Count == 0)
        {
            result = default;
            return false;
        }

        result = Front;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        if (Count == 0) return false;

        var comparer = EqualityComparer<T>.Default;
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (comparer.Equals(_map[node][cur], item))
                return true;

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray()
    {
        int size = Count;
        if (size == 0) return Array.Empty<T>();

        T[] result = new T[size];
        int index = 0;
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            result[index++] = _map[node][cur];

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return result;
    }

    public void CopyTo(T[] array, int arrayIndex = 0)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        int count = Count;
        if (arrayIndex < 0 || arrayIndex + count > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (count == 0) return;

        int index = arrayIndex;
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            array[index++] = _map[node][cur];

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }
    }

    public void CopyTo(Span<T> destination)
    {
        int count = Count;
        if (destination.Length < count)
            throw new ArgumentException("Destination span is too small", nameof(destination));

        if (count == 0) return;

        int index = 0;
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            destination[index++] = _map[node][cur];

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }
    }

    public bool TryGetSpan(out Span<T> span)
    {
        if (Count == 0)
        {
            span = Span<T>.Empty;
            return true;
        }

        if (_start.NodeIndex == _finish.NodeIndex)
        {
            int length = _finish.CurIndex - _start.CurIndex;
            span = new Span<T>(_map[_start.NodeIndex], _start.CurIndex, length);
            return true;
        }

        span = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        int index = 0;
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (comparer.Equals(_map[node][cur], item))
                return index;

            index++;
            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return -1;
    }

    public int LastIndexOf(T item)
    {
        int count = Count;
        if (count == 0) return -1;

        var comparer = EqualityComparer<T>.Default;

        for (int i = count - 1; i >= 0; i--)
        {
            if (comparer.Equals(this[i], item))
                return i;
        }

        return -1;
    }

    public int FindIndex(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        int index = 0;
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (predicate(_map[node][cur]))
                return index;

            index++;
            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return -1;
    }

    public bool TryFind(Func<T, bool> predicate, out T result)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (predicate(_map[node][cur]))
            {
                result = _map[node][cur];
                return true;
            }

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        result = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Exists(Func<T, bool> predicate)
    {
        return FindIndex(predicate) >= 0;
    }

    public bool TrueForAll(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (!predicate(_map[node][cur]))
                return false;

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return true;
    }

    public void AddRange(ReadOnlySpan<T> items)
    {
        for (int i = 0; i < items.Length; i++)
            PushBack(items[i]);
    }

    public void AddRangeFast(ReadOnlySpan<T> items)
    {
        for (int i = 0; i < items.Length; i++)
            PushBackFast(items[i]);
    }

    public void Insert(int index, T item)
    {
        int count = Count;
        if (index < 0 || index > count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (index == 0)
        {
            PushFront(item);
            return;
        }

        if (index == count)
        {
            PushBack(item);
            return;
        }

        if (index <= count / 2)
        {

            PushFront(Front);
            for (int i = 0; i < index - 1; i++)
                this[i] = this[i + 1];
            this[index] = item;
        }
        else
        {

            PushBack(Back);
            for (int i = count - 1; i > index; i--)
                this[i] = this[i - 1];
            this[index] = item;
        }
    }

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index < 0) return false;

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        int count = Count;
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (index < count / 2)
        {

            for (int i = index; i > 0; i--)
                this[i] = this[i - 1];
            PopFront();
        }
        else
        {

            for (int i = index; i < count - 1; i++)
                this[i] = this[i + 1];
            PopBack();
        }
    }

    public void RemoveRange(int index, int count)
    {
        int size = Count;
        if (index < 0 || count < 0 || index + count > size)
            throw new ArgumentOutOfRangeException();

        if (count == 0) return;

        for (int i = 0; i < count; i++)
            RemoveAt(index);
    }

    public int RemoveAll(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        int removed = 0;
        int writeIndex = 0;
        int count = Count;

        while (writeIndex < count && !predicate(this[writeIndex]))
            writeIndex++;

        if (writeIndex == count)
            return 0;

        for (int readIndex = writeIndex + 1; readIndex < count; readIndex++)
        {
            T item = this[readIndex];
            if (!predicate(item))
            {
                this[writeIndex++] = item;
            }
            else
            {
                removed++;
            }
        }

        int toRemove = count - writeIndex;
        for (int i = 0; i < toRemove; i++)
            PopBack();

        return removed + 1;
    }

    public void Reverse()
    {
        int count = Count;
        for (int i = 0; i < count / 2; i++)
        {
            T temp = this[i];
            this[i] = this[count - i - 1];
            this[count - i - 1] = temp;
        }
    }

    public void Reverse(int index, int count)
    {
        if (index < 0 || count < 0 || index + count > Count)
            throw new ArgumentOutOfRangeException();

        for (int i = 0; i < count / 2; i++)
        {
            int left = index + i;
            int right = index + count - 1 - i;
            T temp = this[left];
            this[left] = this[right];
            this[right] = temp;
        }
    }

    public void Sort()
    {
        int count = Count;
        if (count <= 1) return;

        T[] array = ToArray();
        Array.Sort(array);

        for (int i = 0; i < count; i++)
            this[i] = array[i];
    }

    public void Sort(IComparer<T> comparer)
    {
        int count = Count;
        if (count <= 1) return;

        T[] array = ToArray();
        Array.Sort(array, comparer);

        for (int i = 0; i < count; i++)
            this[i] = array[i];
    }

    public void Sort(Comparison<T> comparison)
    {
        if (comparison == null)
            throw new ArgumentNullException(nameof(comparison));

        int count = Count;
        if (count <= 1) return;

        T[] array = ToArray();
        Array.Sort(array, comparison);

        for (int i = 0; i < count; i++)
            this[i] = array[i];
    }

    public int BinarySearch(T item)
    {
        return BinarySearch(item, Comparer<T>.Default);
    }

    public int BinarySearch(T item, IComparer<T> comparer)
    {
        if (comparer == null)
            throw new ArgumentNullException(nameof(comparer));

        int left = 0;
        int right = Count - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int cmp = comparer.Compare(this[mid], item);

            if (cmp == 0)
                return mid;
            else if (cmp < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return ~left;
    }

    public void ForEach(Action<T> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            action(_map[node][cur]);

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }
    }

    public List<T> FindAll(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        var result = new List<T>();
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (predicate(_map[node][cur]))
                result.Add(_map[node][cur]);

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return result;
    }

    public LuminDeque<T> GetRange(int index, int count)
    {
        if (index < 0 || count < 0 || index + count > Count)
            throw new ArgumentOutOfRangeException();

        var result = new LuminDeque<T>(count);
        for (int i = 0; i < count; i++)
            result.PushBack(this[index + i]);

        return result;
    }

    public LuminDeque<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter)
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        int count = Count;
        var result = new LuminDeque<TOutput>(count);

        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            result.PushBack(converter(_map[node][cur]));

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return result;
    }

    public void TrimExcess()
    {
        int count = Count;
        if (count == 0)
        {

            for (int i = 0; i < _mapSize; i++)
            {
                _map[i] = null;
            }

            _allocatedStart = _mapSize / 2;
            _allocatedEnd = _allocatedStart;
            ResetToCenter();
            return;
        }

        for (int i = 0; i < _allocatedStart; i++)
        {
            _map[i] = null;
        }

        int startNodeIndex = _start.NodeIndex;
        int finishNodeIndex = _finish.NodeIndex;

        for (int i = finishNodeIndex + 1; i < _mapSize; i++)
        {
            _map[i] = null;
        }

        _allocatedStart = startNodeIndex;
        _allocatedEnd = finishNodeIndex + 1;
    }

    public void EnsureCapacity(int capacity)
    {
        int currentCapacity = _mapSize * _blockSize;
        if (capacity > currentCapacity)
        {
            Reserve(capacity);
        }
    }

    public LuminDeque<T> Take(int count)
    {
        int actualCount = Math.Min(count, Count);
        if (actualCount <= 0)
            return new LuminDeque<T>();

        return GetRange(0, actualCount);
    }

    public LuminDeque<T> Skip(int count)
    {
        int totalCount = Count;
        if (count >= totalCount)
            return new LuminDeque<T>();

        int takeCount = totalCount - count;
        return GetRange(count, takeCount);
    }

    public LuminDeque<T> Where(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        var result = new LuminDeque<T>();
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (predicate(_map[node][cur]))
                result.PushBack(_map[node][cur]);

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return result;
    }

    public LuminDeque<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        var result = new LuminDeque<TResult>(Count);
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            result.PushBack(selector(_map[node][cur]));

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return result;
    }

    public int CountBy(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        int count = 0;
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (predicate(_map[node][cur]))
                count++;

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Any(Func<T, bool> predicate)
    {
        return Exists(predicate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Any()
    {
        return Count > 0;
    }

    public T FirstOrDefault()
    {
        return Count > 0 ? Front : default;
    }

    public T FirstOrDefault(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        return TryFind(predicate, out T result) ? result : default;
    }

    public T LastOrDefault()
    {
        return Count > 0 ? Back : default;
    }

    public T LastOrDefault(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        int count = Count;
        for (int i = count - 1; i >= 0; i--)
        {
            if (predicate(this[i]))
                return this[i];
        }

        return default;
    }

    public T ElementAtOrDefault(int index)
    {
        return (index >= 0 && index < Count) ? this[index] : default;
    }

    public TAccumulate Aggregate<TAccumulate>(TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        TAccumulate result = seed;
        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            result = func(result, _map[node][cur]);

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return result;
    }

    public T Min()
    {
        int count = Count;
        if (count == 0)
            throw new InvalidOperationException("Deque is empty");

        var comparer = Comparer<T>.Default;
        T min = this[0];

        for (int i = 1; i < count; i++)
        {
            if (comparer.Compare(this[i], min) < 0)
                min = this[i];
        }

        return min;
    }

    public T Max()
    {
        int count = Count;
        if (count == 0)
            throw new InvalidOperationException("Deque is empty");

        var comparer = Comparer<T>.Default;
        T max = this[0];

        for (int i = 1; i < count; i++)
        {
            if (comparer.Compare(this[i], max) > 0)
                max = this[i];
        }

        return max;
    }

    public TResult Min<TResult>(Func<T, TResult> selector) where TResult : IComparable<TResult>
    {
        int count = Count;
        if (count == 0)
            throw new InvalidOperationException("Deque is empty");

        TResult min = selector(this[0]);

        for (int i = 1; i < count; i++)
        {
            TResult value = selector(this[i]);
            if (value.CompareTo(min) < 0)
                min = value;
        }

        return min;
    }

    public TResult Max<TResult>(Func<T, TResult> selector) where TResult : IComparable<TResult>
    {
        int count = Count;
        if (count == 0)
            throw new InvalidOperationException("Deque is empty");

        TResult max = selector(this[0]);

        for (int i = 1; i < count; i++)
        {
            TResult value = selector(this[i]);
            if (value.CompareTo(max) > 0)
                max = value;
        }

        return max;
    }

    public LuminDeque<T> Distinct()
    {
        var result = new LuminDeque<T>();
        var seen = new HashSet<T>();

        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            if (seen.Add(_map[node][cur]))
                result.PushBack(_map[node][cur]);

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return result;
    }

    public LuminDeque<T> Concat(LuminDeque<T> other)
    {
        var result = new LuminDeque<T>(Count + other.Count);

        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            result.PushBack(_map[node][cur]);

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        cur = other._start.CurIndex;
        node = other._start.NodeIndex;
        last = other._start.LastIndex;
        finishCur = other._finish.CurIndex;
        finishNode = other._finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            result.PushBack(other._map[node][cur]);

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = other._blockSize;
            }
        }

        return result;
    }

    public void Fill(T value, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        for (int i = 0; i < count; i++)
            PushBack(value);
    }

    public void Swap(ref LuminDeque<T> other)
    {

        var temp = other._map;
        other._map = _map;
        _map = temp;
        (other._mapSize, _mapSize) = (_mapSize, other._mapSize);
        (other._start, _start) = (_start, other._start);
        (other._finish, _finish) = (_finish, other._finish);
        (other._allocatedStart, _allocatedStart) = (_allocatedStart, other._allocatedStart);
        (other._allocatedEnd, _allocatedEnd) = (_allocatedEnd, other._allocatedEnd);
    }

    public int Partition(Func<T, bool> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        int count = Count;
        int writeIndex = 0;

        for (int readIndex = 0; readIndex < count; readIndex++)
        {
            if (predicate(this[readIndex]))
            {
                if (readIndex != writeIndex)
                {
                    T temp = this[writeIndex];
                    this[writeIndex] = this[readIndex];
                    this[readIndex] = temp;
                }

                writeIndex++;
            }
        }

        return writeIndex;
    }

    public void RemoveFrontFast(int count)
    {
        if (count < 0 || count > Count)
            throw new ArgumentOutOfRangeException(nameof(count));

        for (int i = 0; i < count; i++)
            PopFrontFast();
    }

    public void RemoveBackFast(int count)
    {
        if (count < 0 || count > Count)
            throw new ArgumentOutOfRangeException(nameof(count));

        for (int i = 0; i < count; i++)
            PopBackFast();
    }

    public LuminDeque<T> Clone()
    {
        return new LuminDeque<T>(this);
    }

    public List<T> ToList()
    {
        int count = Count;
        var list = new List<T>(count);

        for (int i = 0; i < count; i++)
            list.Add(this[i]);

        return list;
    }

    public HashSet<T> ToHashSet()
    {
        var set = new HashSet<T>();

        int cur = _start.CurIndex;
        int node = _start.NodeIndex;
        int last = _start.LastIndex;
        int finishCur = _finish.CurIndex;
        int finishNode = _finish.NodeIndex;

        while (node != finishNode || cur != finishCur)
        {
            set.Add(_map[node][cur]);

            if (++cur == last)
            {
                node++;
                cur = 0;
                last = _blockSize;
            }
        }

        return set;
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _mapSize * _blockSize;
    }

    public int AllocatedBlocks
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _allocatedEnd - _allocatedStart;
    }

    public void Reserve(int capacity)
    {
        int currentCapacity = _mapSize * _blockSize;
        if (capacity <= currentCapacity) return;

        int blocksNeeded = (capacity + _blockSize - 1) / _blockSize;
        int newMapSize = Math.Max(_mapSize * 2, blocksNeeded + 2);

        ReallocateMap(newMapSize - _mapSize, false);
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly LuminDeque<T> _deque;
        private Iterator _current;
        private bool _started;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(LuminDeque<T> deque)
        {
            _deque = deque;
            _current = deque._start;
            _started = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                return _current.CurIndex != _deque._finish.CurIndex ||
                       _current.NodeIndex != _deque._finish.NodeIndex;
            }

            _current.CurIndex++;
            if (_current.CurIndex == _current.LastIndex)
            {
                _current.SetNode(_current.NodeIndex + 1, _deque._blockSize);
                _current.CurIndex = _current.FirstIndex;
            }

            return _current.CurIndex != _deque._finish.CurIndex || _current.NodeIndex != _deque._finish.NodeIndex;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _deque._map[_current.NodeIndex][_current.CurIndex];
        }

        object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _current = _deque._start;
            _started = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
        }
    }
}
