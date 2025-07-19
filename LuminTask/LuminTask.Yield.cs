using System.Runtime.CompilerServices;
using System.Threading;
using Lumin.Threading.Source;

namespace Lumin.Threading.Tasks;

public readonly ref partial struct LuminTask
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask Yield()
    {
        var source = LuminTaskSource.Rent();
        ThreadPool.QueueUserWorkItem(static s =>
        {
            var source = (LuminTaskSource)s!;
            try
            {
                source.TrySetResult();
            }
            finally
            {
                LuminTaskSource.Return(source);
            }
        }, source);

        return new LuminTask(source, source.Token);
    }
}