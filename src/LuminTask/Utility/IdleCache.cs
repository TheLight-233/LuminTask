using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility;

/// <summary>Intrusive link used only while an object is owned by the shared idle cache.</summary>
public interface ILuminCacheNode
{
    ILuminCacheNode? CacheNext { get; set; }
}

/// <summary>Shared by the core and separately compiled Unity integrations.</summary>
public static class LuminTaskIdleCache
{
    private struct Bucket
    {
        internal Type? Type;
        internal ILuminCacheNode? Head;
        internal int Charge, Hash;
    }

    private static readonly StorageGate Gate = new();
    private static Bucket[]? _buckets;
    private static int _bucketCount, _indexBytes;
    private static ILuminCacheNode? _recent;
    private static Type? _recentType;
    private static int _recentCharge;
    private static int _limit = 64 * 1024;
    private static int _bytes;

    public static int MaxIdleObjectBytes
    {
        get { using (Gate.Enter()) return _limit; }
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            using (Gate.Enter())
            {
                _limit = value;
                if (_bytes + _indexBytes > value) TrimLocked();
            }
        }
    }

    /// <summary>Charged idle objects and their shared type index.</summary>
    public static int RetainedBytes { get { using (Gate.Enter()) return _bytes + _indexBytes; } }

    internal static void Snapshot(out int objectBytes, out int indexBytes)
    {
        using (Gate.Enter()) { objectBytes = _bytes; indexBytes = _indexBytes; }
    }

    public static ILuminCacheNode? Take(Type type)
    {
        using (Gate.Enter())
        {
            var recent = _recent;
            if (recent != null && ReferenceEquals(_recentType, type))
            {
                _recent = null;
                _recentType = null;
                _bytes -= _recentCharge;
                return recent;
            }
            return TakeFromBucket(type);
        }
    }

    private static ILuminCacheNode? TakeFromBucket(Type type)
    {
        var table = _buckets;
        if (table == null) return null;
        int i = Find(table, type, RuntimeHelpers.GetHashCode(type));
        ref var bucket = ref table[i];
        if (bucket.Type == null) return null;
        var node = bucket.Head!;
        bucket.Head = node.CacheNext;
        node.CacheNext = null;
        _bytes -= bucket.Charge;
        if (bucket.Head == null) RemoveEmpty(table, i);
        return node;
    }

    public static void Return(ILuminCacheNode node, int charge)
    {
        if (charge <= 0) throw new ArgumentOutOfRangeException(nameof(charge));
        Return(node, charge, node.GetType());
    }

    internal static void Return(ILuminCacheNode node, int charge, Type type)
    {
        using (Gate.Enter())
        {
            if (charge > _limit - _bytes - _indexBytes) return;
            if (_recent != null && !TryIndex(_recent, _recentType!, _recentCharge, _limit - _bytes - charge)) return;
            _recent = node;
            _recentType = type;
            _recentCharge = charge;
            _bytes += charge;
        }
    }

    private static int Find(Bucket[] table, Type type, int hash)
    {
        int mask = table.Length - 1;
        int i = hash & mask;
        while (table[i].Type != null && !ReferenceEquals(table[i].Type, type))
            i = (i + 1) & mask;
        return i;
    }

    // Grow only when the retained replacement array fits the same budget as its objects.
    // With at least one empty bucket, lookup terminates even when all keys collide.
    private static bool TryIndex(ILuminCacheNode node, Type type, int charge, int availableBytes)
    {
        int hash = RuntimeHelpers.GetHashCode(type);
        var table = _buckets;
        int i = table == null ? 0 : Find(table, type, hash);
        if (table == null || (table[i].Type == null && _bucketCount >= table.Length - table.Length / 4))
        {
            int length = table == null ? 4 : table.Length * 2;
            long cost = 4L * IntPtr.Size + (long)length * Unsafe.SizeOf<Bucket>();
            if (cost > availableBytes) return false;
            var grown = new Bucket[length];
            if (table != null)
            {
                foreach (var old in table)
                    if (old.Type != null) grown[Find(grown, old.Type, old.Hash)] = old;
            }
            _buckets = table = grown;
            _indexBytes = (int)cost;
            i = Find(table, type, hash);
        }
        ref var bucket = ref table[i];
        if (bucket.Type == null)
        {
            bucket.Type = type;
            bucket.Hash = hash;
            bucket.Charge = charge;
            _bucketCount++;
        }
        else if (bucket.Charge != charge)
            throw new InvalidOperationException("Cache charges must be constant per concrete type.");
        node.CacheNext = bucket.Head;
        bucket.Head = node;
        return true;
    }

    private static void RemoveEmpty(Bucket[] table, int hole)
    {
        // Move a key only if its linear probe crossed the vacated bucket.
        int mask = table.Length - 1;
        for (int i = (hole + 1) & mask; table[i].Type != null; i = (i + 1) & mask)
        {
            int home = table[i].Hash & mask;
            if (((hole - home) & mask) >= ((i - home) & mask)) continue;
            table[hole] = table[i];
            hole = i;
        }
        table[hole] = default;
        _bucketCount--;
    }

    internal static void Trim()
    {
        using (Gate.Enter()) TrimLocked();
    }

    private static void TrimLocked()
    {
        _recent = null;
        _recentType = null;
        if (_buckets != null)
        {
            foreach (var bucket in _buckets)
            {
                var node = bucket.Head;
                while (node != null)
                {
                    var next = node.CacheNext;
                    node.CacheNext = null;
                    node = next;
                }
            }
            _buckets = null;
        }
        _bucketCount = _indexBytes = 0;
        _bytes = 0;
    }
}
