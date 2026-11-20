using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility
{
    internal sealed class ForgetBlock : SlotObserver, ILuminCacheNode
    {
        public ILuminCacheNode? CacheNext { get; set; }
        private static readonly Action<object> s_run = Run;

        // This instance only consumes the handle supplied by the publishing slot.
        private static readonly ForgetBlock s_discard = new ForgetBlock();

        internal override void Completed(ulong handle, int index)
        {
            LuminTaskSource.TryForget(handle);
        }

        private LuminTask _awaiter;
        private Action<Exception>? _handler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Schedule(in LuminTask awaiter, Action<Exception>? handler)
        {
            if (handler == null && LuminTaskSource.TryRegisterObserver(awaiter._handle, s_discard, 0)) return;
            var b = (ForgetBlock?)LuminTaskIdleCache.Take(typeof(ForgetBlock)) ?? new ForgetBlock();
            b._awaiter = awaiter;
            b._handler = handler;
            try { awaiter.SourceOnCompleted(s_run, b); }
            catch
            {
                b._awaiter = default;
                b._handler = null;
                LuminTaskIdleCache.Return(b, 64);
                throw;
            }
        }

        private static void Run(object state)
        {
            var b = (ForgetBlock)state;
            var awaiter = b._awaiter;
            var handler = b._handler;

            b._awaiter = default;
            b._handler = null;

            LuminTaskIdleCache.Return(b, 64);

            if (handler == null) { LuminTaskSource.TryForget(awaiter._handle); return; }
            try { awaiter.GetResult(); }
            catch (Exception ex) { handler(ex); }
        }
    }
}
