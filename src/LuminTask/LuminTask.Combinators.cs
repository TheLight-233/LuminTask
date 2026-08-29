using System;
using System.Collections.Generic;
using System.Threading;

namespace LuminThread;

public readonly partial struct LuminTask
{
    /// <summary>Awaits tasks one at a time in input order.</summary>
    public static async LuminTask WhenAllSequential(IEnumerable<LuminTask> tasks, CancellationToken cancellationToken = default)
    {
        if (tasks == null) throw new ArgumentNullException(nameof(tasks));

        foreach (var task in tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await task.ConfigureAwait(false);
        }
    }

    /// <summary>Runs a factory and retries failures up to <paramref name="retryCount"/> times.</summary>
    public static async LuminTask Retry(Func<LuminTask> factory, int retryCount, TimeSpan initialDelay = default, CancellationToken cancellationToken = default)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (retryCount < 0) throw new ArgumentOutOfRangeException(nameof(retryCount));
        if (initialDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(initialDelay));

        Exception? last = null;
        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await factory().ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (attempt < retryCount)
            {
                last = exception;
                if (initialDelay > TimeSpan.Zero)
                {
                    var multiplier = Math.Min(attempt, 30);
                    var delayTicks = initialDelay.Ticks > TimeSpan.MaxValue.Ticks / (1L << multiplier)
                        ? TimeSpan.MaxValue.Ticks
                        : initialDelay.Ticks * (1L << multiplier);
                    await Delay(new TimeSpan(delayTicks), cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }

        throw last ?? new InvalidOperationException("Retry did not execute the factory.");
    }
}

public static class LuminTaskUnwrapExtensions
{
    public static async LuminTask<T> Unwrap<T>(this LuminTask<LuminTask<T>> task)
        => await (await task.ConfigureAwait(false)).ConfigureAwait(false);
}
