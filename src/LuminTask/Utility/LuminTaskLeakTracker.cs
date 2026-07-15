using System;
using System.Diagnostics;
#if DEBUG
using System.Collections.Concurrent;
using System.Threading;
#endif

namespace LuminThread.Utility
{
    /// <summary>
    /// DEBUG-only tracker that records every task source rented from the pool and reports any
    /// that are never consumed (i.e. never <c>await</c>-ed or <c>Forget()</c>-ed).
    /// <para>
    /// Because <see cref="LuminTask"/> is backed by unmanaged memory and carries no managed
    /// reference, there is no GC fallback: a task that is neither awaited nor Forget()-ed
    /// leaks its slot + native block. This is the same "must be consumed exactly once" contract
    /// that <c>ValueTask</c> imposes — the only difference is the failure mode (leak vs. eventual
    /// GC reclaim). This tracker turns that contract violation into an actionable report with the
    /// creation stack during development.
    /// </para>
    /// <para>
    /// Every method is <see cref="ConditionalAttribute"/>("DEBUG"), so in Release builds the calls
    /// (and their arguments) are removed by the compiler: zero cost, zero allocation.
    /// </para>
    /// </summary>
    public static class LuminTaskLeakTracker
    {
        /// <summary>
        /// When true (default), a creation stack trace is captured for each rented source so that
        /// leak reports point at the exact call site. Set to false to reduce DEBUG overhead.
        /// Has no effect in Release (the tracker is compiled out).
        /// </summary>
        public static bool CaptureStackTrace = true;

#if DEBUG
        private static readonly ConcurrentDictionary<IntPtr, string?> _live = new ConcurrentDictionary<IntPtr, string?>();
        private static int _exitHookInstalled;
#endif

        /// <summary>Records that a task source has been rented. DEBUG only.</summary>
        [Conditional("DEBUG")]
        public static unsafe void OnRent(void* ptr)
        {
#if DEBUG
            EnsureExitHook();
            string? trace = CaptureStackTrace ? new StackTrace(1, true).ToString() : null;
            _live[(IntPtr)ptr] = trace;
#endif
        }

        /// <summary>Records that a task source has been returned (consumed/disposed). DEBUG only.</summary>
        [Conditional("DEBUG")]
        public static unsafe void OnReturn(void* ptr)
        {
#if DEBUG
            _live.TryRemove((IntPtr)ptr, out _);
#endif
        }

        /// <summary>
        /// Number of task sources currently rented but not yet returned.
        /// Always 0 in Release (the tracker is compiled out).
        /// </summary>
        public static int LiveCount
        {
            get
            {
#if DEBUG
                return _live.Count;
#else
                return 0;
#endif
            }
        }

        /// <summary>
        /// Writes every currently-leaked (rented-but-never-returned) task source — with its
        /// creation stack, if captured — to <see cref="Console.Error"/>. DEBUG only; no-op in Release.
        /// Call this at the end of a test run to assert there are no leaks.
        /// </summary>
        [Conditional("DEBUG")]
        public static void DumpLeaks()
        {
#if DEBUG
            foreach (var kv in _live)
            {
                Console.Error.WriteLine(
                    $"[LuminTask LEAK] task source 0x{kv.Key.ToInt64():X} was created but never awaited or Forget()-ed.");
                if (kv.Value != null)
                    Console.Error.WriteLine(kv.Value);
            }
#endif
        }

#if DEBUG
        private static void EnsureExitHook()
        {
            if (Interlocked.Exchange(ref _exitHookInstalled, 1) != 0)
                return;

            AppDomain.CurrentDomain.ProcessExit += static (_, _) =>
            {
                if (_live.IsEmpty)
                    return;

                Console.Error.WriteLine(
                    $"[LuminTask LEAK] {_live.Count} task source(s) were created but never consumed " +
                    "(await or Forget()). Creation stacks follow:");
                DumpLeaks();
            };
        }
#endif
    }
}
