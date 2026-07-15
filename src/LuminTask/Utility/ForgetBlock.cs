using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility
{
    // Pooled, allocation-free continuation for Forget() on a not-yet-completed task. Avoids the
    // per-call closure that capturing the awaiter would otherwise create. Same per-thread pool
    // discipline as StateMachineBox: rented when scheduling, returned when it fires.
    internal sealed class ForgetBlock
    {
        [ThreadStatic] private static ForgetBlock? _freeList;
        private static readonly Action<object> s_run = Run;

        private ForgetBlock? _next;
        private LuminTaskAwaiter _awaiter;
        private Action<Exception>? _handler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Schedule(in LuminTaskAwaiter awaiter, Action<Exception>? handler)
        {
            var b = _freeList;
            if (b != null) { _freeList = b._next; b._next = null; }
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
            b._next = _freeList;
            _freeList = b;

            try { awaiter.GetResult(); }
            catch (Exception ex) { handler?.Invoke(ex); }
        }
    }

    internal sealed class ForgetBlock<T>
    {
        [ThreadStatic] private static ForgetBlock<T>? _freeList;
        private static readonly Action<object> s_run = Run;

        private ForgetBlock<T>? _next;
        private LuminTaskAwaiter<T> _awaiter;
        private Action<Exception>? _handler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Schedule(in LuminTaskAwaiter<T> awaiter, Action<Exception>? handler)
        {
            var b = _freeList;
            if (b != null) { _freeList = b._next; b._next = null; }
            else b = new ForgetBlock<T>();

            b._awaiter = awaiter;
            b._handler = handler;
            awaiter.SourceOnCompleted(s_run, b);
        }

        private static void Run(object state)
        {
            var b = (ForgetBlock<T>)state;
            var awaiter = b._awaiter;
            var handler = b._handler;

            b._awaiter = default;
            b._handler = null;
            b._next = _freeList;
            _freeList = b;

            try { awaiter.GetResult(); }
            catch (Exception ex) { handler?.Invoke(ex); }
        }
    }
}
