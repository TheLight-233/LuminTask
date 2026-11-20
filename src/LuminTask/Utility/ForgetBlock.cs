using System;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility
{
    internal sealed class ForgetBlock : ILuminCacheNode
    {
        public ILuminCacheNode? CacheNext { get; set; }
        private static readonly Action<object> s_run = Run;

        private LuminTask _awaiter;
        private Action<Exception>? _handler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Schedule(in LuminTask awaiter, Action<Exception>? handler)
        {
            var b = (ForgetBlock?)LuminTaskIdleCache.Take(typeof(ForgetBlock)) ?? new ForgetBlock();
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

            LuminTaskIdleCache.Return(b, 64);

            try { awaiter.GetResult(); }
            catch (Exception ex) { handler?.Invoke(ex); }
        }
    }
}