using System;
using System.Diagnostics;
#if DEBUG
using System.Collections.Concurrent;
using System.Threading;
#endif

namespace LuminThread.Utility
{
    /// <summary>
    /// DEBUG-only tracker that records every task slot rented from <see cref="LuminTaskBag"/> and
    /// reports any that are never consumed (i.e. never <c>await</c>-ed or <c>Forget()</c>-ed).
    /// <para>
    /// Because <see cref="LuminTask"/> is a bare 64-bit handle and carries no managed reference,
    /// there is no GC fallback: a task that is neither awaited nor Forget()-ed leaks its slot.
    /// This is the same "must be consumed exactly once" contract that <c>ValueTask</c> imposes —
    /// the only difference is the failure mode (leak vs. eventual GC reclaim). This tracker turns
    /// that contract violation into an actionable report with the creation stack during development.
    /// </para>
    /// <para>
    /// Every method is <see cref="ConditionalAttribute"/>("DEBUG"), so in Release builds the calls
    /// (and their arguments) are removed by the compiler: zero cost, zero allocation.
    /// </para>
    /// </summary>
    public static class LuminTaskLeakTracker
    {
        /// <summary>
        /// When true (default), a creation stack trace is captured for each rented slot so that
        /// leak reports point at the exact call site. Set to false to reduce DEBUG overhead.
        /// Has no effect in Release (the tracker is compiled out).
        /// </summary>
        public static bool CaptureStackTrace = true;

#if DEBUG
        private static readonly ConcurrentDictionary<LuminTaskHandle, string?> _live = new ConcurrentDictionary<LuminTaskHandle, string?>();
        private static int _exitHookInstalled;
#endif

        /// <summary>Records that a task slot has been rented. DEBUG only.</summary>
        [Conditional("DEBUG")]
        public static void OnRent(LuminTaskHandle handle)
        {
#if DEBUG
            EnsureExitHook();
            string? trace = CaptureStackTrace ? new StackTrace(1, true).ToString() : null;
            _live[handle] = trace;
#endif
        }

        /// <summary>Records that a task slot has been returned (consumed/disposed). DEBUG only.</summary>
        [Conditional("DEBUG")]
        public static void OnReturn(LuminTaskHandle handle)
        {
#if DEBUG
            _live.TryRemove(handle, out _);
#endif
        }

        /// <summary>
        /// Number of task slots currently rented but not yet returned.
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
        /// Writes every currently-leaked (rented-but-never-returned) task slot — with its
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
                    $"[LuminTask LEAK] task slot {((int)(uint)kv.Key & 0xFFFF) - 1} (handle 0x{kv.Key:X}) was created but never awaited or Forget()-ed.");
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
                    $"[LuminTask LEAK] {_live.Count} task slot(s) were created but never consumed " +
                    "(await or Forget()). Creation stacks follow:");
                DumpLeaks();
            };
        }
#endif
    }
}