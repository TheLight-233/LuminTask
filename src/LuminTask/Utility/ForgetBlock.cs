using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility
{
    // Pooled, allocation-free continuation for Forget() on a not-yet-completed task. Avoids the
    // per-call closure that capturing the awaiter would otherwise create. Same per-thread pool
    // discipline and cap as StateMachineBox: rented when scheduling, returned when it fires, and the
    // free list never keeps more than MaxRetainedPerThread blocks.
    //
    // There is deliberately only ONE block type. Forget discards the result, so the typed overload
    // needs no LuminTask<T>-shaped awaiter: it converts to the plain 8-byte LuminTask and exits
    // through LuminTaskCoreSource.ConsumeVoid, which propagates the failure and still returns a
    // pooled result container on the way. A per-T ForgetBlock<T> would have cost one closed generic
    // type - and its 16 retained blocks per thread - for every T that is ever Forget()-ed.
    internal sealed class ForgetBlock
    {
        private const int MaxRetainedPerThread = 16;

        [ThreadStatic] private static ForgetBlock? _freeList;
        [ThreadStatic] private static int _freeCount;
        private static readonly Action<object> s_run = Run;

        private ForgetBlock? _next;
        private LuminTask _awaiter;
        private Action<Exception>? _handler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Schedule(in LuminTask awaiter, Action<Exception>? handler)
        {
            var b = _freeList;
            if (b != null) { _freeList = b._next; b._next = null; _freeCount--; }
            else b = new ForgetBlock();

            b._awaiter = awaiter;
            b._handler = handler;
            awaiter.SourceOnCompleted(s_run, b);
        }

        private static void Run(object state)
        {
            var b = (ForgetBlock)state;
            var awaiter = b._awaiter;
            var handler = b._handler;

            b._awaiter = default;
            b._handler = null;

            if (_freeCount < MaxRetainedPerThread)
            {
                b._next = _freeList;
                _freeList = b;
                _freeCount++;
            }
            else
            {
                b._next = null;
            }

            try { awaiter.GetResult(); }
            catch (Exception ex) { handler?.Invoke(ex); }
        }
    }
}