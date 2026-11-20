using System;
using System.Collections.Generic;

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
        internal ILuminCacheNode? Head;
        internal int Charge;
    }

    private static readonly StorageGate Gate = new();
    private static Dictionary<Type, Bucket>? _buckets;
    private static ILuminCacheNode? _recent;
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
                if (_bytes > value) TrimLocked();
            }
        }
    }

    public static int RetainedBytes { get { using (Gate.Enter()) return _bytes; } }

    public static ILuminCacheNode? Take(Type type)
    {
        using (Gate.Enter())
        {
            var recent = _recent;
            if (recent != null && recent.GetType() == type)
            {
                _recent = null;
                _bytes -= _recentCharge;
                return recent;
            }
            return TakeFromBucket(type);
        }
    }

    internal static T? TakeRunner<T>() where T : class, ILuminCacheNode
    {
        using (Gate.Enter())
        {
            if (_recent is T runner)
            {
                _recent = null;
                _bytes -= _recentCharge;
                return runner;
            }
            return _buckets == null ? null : (T?)TakeFromBucket(typeof(T));
        }
    }

    private static ILuminCacheNode? TakeFromBucket(Type type)
    {
        if (_buckets == null || !_buckets.TryGetValue(type, out var bucket)) return null;
        var node = bucket.Head!;
        bucket.Head = node.CacheNext;
        node.CacheNext = null;
        _bytes -= bucket.Charge;
        if (bucket.Head == null) _buckets.Remove(type);
        else _buckets[type] = bucket;
        return node;
    }

    public static void Return(ILuminCacheNode node, int charge)
    {
        if (charge <= 0) throw new ArgumentOutOfRangeException(nameof(charge));
        using (Gate.Enter())
        {
            if (charge > _limit - _bytes) return;
            // Promote every return. Leaving the first type in front permanently
            // would send every subsequent runner type through the dictionary on every await.
            if (_recent != null) PushToBucket(_recent, _recentCharge);
            _recent = node;
            _recentCharge = charge;
            _bytes += charge;
        }
    }

    private static void PushToBucket(ILuminCacheNode node, int charge)
    {
        var type = node.GetType();
        _buckets ??= new Dictionary<Type, Bucket>();
        _buckets.TryGetValue(type, out var bucket);
        if (bucket.Head != null && bucket.Charge != charge)
            throw new InvalidOperationException("Cache charges must be constant per concrete type.");
        node.CacheNext = bucket.Head;
        bucket.Head = node;
        bucket.Charge = charge;
        _buckets[type] = bucket;
    }

    internal static void Trim()
    {
        using (Gate.Enter()) TrimLocked();
    }

    private static void TrimLocked()
    {
        _recent = null;
        if (_buckets != null)
        {
            foreach (var bucket in _buckets.Values)
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
        _bytes = 0;
    }
}
