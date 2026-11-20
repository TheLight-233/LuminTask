using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.Interface;
using LuminThread.Utility;

namespace LuminThread
{
    public enum PlayerLoopTiming : byte
    {
        Initialization = 0,
        LastInitialization = 1,

        EarlyUpdate = 2,
        LastEarlyUpdate = 3,

        FixedUpdate = 4,
        LastFixedUpdate = 5,

        PreUpdate = 6,
        LastPreUpdate = 7,

        Update = 8,
        LastUpdate = 9,

        PreLateUpdate = 10,
        LastPreLateUpdate = 11,

        PostLateUpdate = 12,
        LastPostLateUpdate = 13,

        TimeUpdate = 14,
        LastTimeUpdate = 15,

        DotNet = 16
    }

    public static partial class PlayerLoopHelper
    {
        public const int LoopTimingCount = 17;

        public static int MainThreadId
        {
            get
            {
                EnsureStrategies();
                return Volatile.Read(ref _mainThreadId);
            }
        }
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;
        public static SynchronizationContext? MainThreadSynchronizationContext { get; private set; }

        private static IPlayLoopStrategy?[]? _strategies;
        private static int _mainThreadId;

        private static IPlayLoopStrategy?[] EnsureStrategies()
        {
            var current = Volatile.Read(ref _strategies);
            if (current != null) return current;
            var fresh = new IPlayLoopStrategy?[LoopTimingCount];
            fresh[(int)PlayerLoopTiming.DotNet] = new ManagedPlayerLoop(timerDriven: true);
            Interlocked.CompareExchange(ref _mainThreadId, Thread.CurrentThread.ManagedThreadId, 0);
            return Interlocked.CompareExchange(ref _strategies, fresh, null) ?? fresh;
        }

        public static void Initialize()
        {
            var strategies = EnsureStrategies();
            if (Volatile.Read(ref strategies[(int)PlayerLoopTiming.DotNet]) == null)
                Interlocked.CompareExchange(ref strategies[(int)PlayerLoopTiming.DotNet], new ManagedPlayerLoop(true), null);
        }

        public static void RegisterMainThread(SynchronizationContext synchronizationContext)
        {
            if (synchronizationContext == null) throw new ArgumentNullException(nameof(synchronizationContext));
            MainThreadSynchronizationContext = synchronizationContext;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public static void UnregisterMainThread() => MainThreadSynchronizationContext = null;

        public static void AddAction(PlayerLoopTiming timing, LuminTaskState state, MoveNext moveNext)
        {
            var strategy = Volatile.Read(ref EnsureStrategies()[(byte)timing]);
            if (strategy is null)
                ThrowInvalidLoopTiming(timing);
            strategy!.AddAction(state, moveNext);
        }

        internal static void Schedule(PlayerLoopTiming timing, LuminTaskState state, MoveNext step)
        {
            try { AddAction(timing, state, step); }
            catch
            {
                // Ownership of an inner input transfers only when the scheduling call succeeds.
                if (state.State is IPooledResult helper) helper.ReturnToPool();
                LuminTaskSource.AbandonUnpublished(state.Handle);
                throw;
            }
        }

        public static void AddContinuation(PlayerLoopTiming timing, Action continuation)
        {
            var strategy = Volatile.Read(ref EnsureStrategies()[(byte)timing]);
            if (strategy is null)
                ThrowInvalidLoopTiming(timing);
            strategy!.AddContinuation(continuation);
        }

        public static void SetStrategy(PlayerLoopTiming timing, IPlayLoopStrategy strategy)
        {
            var previous = Interlocked.Exchange(ref EnsureStrategies()[(byte)timing], strategy);
            if (!ReferenceEquals(previous, strategy)) previous?.Dispose();
        }

        static void ThrowInvalidLoopTiming(PlayerLoopTiming playerLoopTiming)
        {
            throw new InvalidOperationException("Target playerLoopTiming is not injected. Please check PlayerLoopHelper.Initialize. PlayerLoopTiming:" + playerLoopTiming);
        }

        public static void Dispose()
        {
            var strategies = Volatile.Read(ref _strategies);
            if (strategies == null) return;
            for (int i = 0; i < strategies.Length; i++)
                Interlocked.Exchange(ref strategies[i], null)?.Dispose();
        }

        internal static long GetBufferBytes()
        {
            var strategies = Volatile.Read(ref _strategies);
            if (strategies == null) return 0;
            long bytes = 0;
            for (int i = 0; i < strategies.Length; i++)
            {
                if (Volatile.Read(ref strategies[i]) is not ManagedPlayerLoop pump) continue;
                if (Array.IndexOf(strategies, pump, 0, i) < 0) bytes += pump.BufferBytes;
            }
            return bytes;
        }

        internal static void TrimBuffers()
        {
            var strategies = Volatile.Read(ref _strategies);
            if (strategies == null) return;
            foreach (var strategy in strategies)
                if (strategy is ManagedPlayerLoop pump) pump.TrimBuffers();
        }
    }
}
