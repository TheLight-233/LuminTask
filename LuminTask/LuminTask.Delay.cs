using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Lumin.Threading.Source;

namespace Lumin.Threading.Tasks
{
    public readonly ref partial struct LuminTask
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask Delay(int millisecondsDelay, CancellationToken cancellationToken = default)
        {
            var source = LuminTaskSource.Rent();
            Timer? timer = null;
            timer = new Timer(static s =>
            {
                var source = ((LuminTaskSource, Timer?))s!;
                try
                {
                    source.Item1.TrySetResult();
                }
                finally
                {
                    LuminTaskSource.Return(source.Item1);
                    source.Item2?.Dispose();
                }
            }, (source, timer), millisecondsDelay, Timeout.Infinite);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    timer.Dispose();
                    source.TrySetCanceled();
                    LuminTaskSource.Return(source);
                });
            }

            return new LuminTask(source, source.Token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LuminTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            return Delay((int)delay.TotalMilliseconds, cancellationToken);
        }

        
    }
}