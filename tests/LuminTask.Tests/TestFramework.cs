using System.Diagnostics;
using LuminThread;

namespace LuminTaskUnitTest;

public sealed class AssertException(string message) : Exception(message);

public static class Assert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition) throw new AssertException("Expected condition to be true. " + message);
    }

    public static void False(bool condition, string? message = null)
    {
        if (condition) throw new AssertException("Expected condition to be false. " + message);
    }

    public static void Equal<T>(T expected, T actual, string? message = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new AssertException($"Expected <{expected}> but was <{actual}>. {message}");
    }

    public static void NotNull(object? value, string? message = null)
    {
        if (value is null) throw new AssertException("Expected non-null value. " + message);
    }

    public static void Fail(string message) => throw new AssertException(message);

    // Awaits the action (consuming the task exactly once) and asserts the given exception type.
    public static async LuminTask ThrowsAsync<TException>(Func<LuminTask> action, string? message = null)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new AssertException(
                $"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}. {message}");
        }
        throw new AssertException($"Expected {typeof(TException).Name} but no exception was thrown. {message}");
    }
}

public sealed class TestRunner
{
    public int Passed;
    public int Failed;
    public readonly List<string> Failures = new();

    // Each test is awaited under a timeout, so a broken primitive surfaces as TIMEOUT rather
    // than hanging the whole run. The LuminTask is consumed exactly once (by Consume).
    public async Task Run(string name, Func<LuminTask> body, int timeoutMs = 60_000)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var task = Consume(body);
            var finished = await Task.WhenAny(task, Task.Delay(timeoutMs));
            if (!ReferenceEquals(finished, task))
            {
                sw.Stop();
                Failed++;
                var msg = $"{name} — TIMEOUT after {timeoutMs}ms (possible deadlock)";
                Failures.Add(msg);
                Console.WriteLine($"  [FAIL] {msg}");
                return;
            }

            await task; // surface exceptions / assertion failures
            sw.Stop();
            Passed++;
            Console.WriteLine($"  [PASS] {name} ({sw.ElapsedMilliseconds} ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Failed++;
            var msg = $"{name} — {ex.GetType().Name}: {ex.Message}";
            Failures.Add(msg);
            Console.WriteLine($"  [FAIL] {msg}");
        }
    }

    public Task Run(string name, Action body, int timeoutMs = 60_000)
        => Run(name, () => { body(); return LuminTask.CompletedTask(); }, timeoutMs);

    private static async Task Consume(Func<LuminTask> body) => await body();
}
