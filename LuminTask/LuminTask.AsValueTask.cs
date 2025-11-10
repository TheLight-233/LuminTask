using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LuminThread;

internal static class LuminTaskValueTaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask AsValueTask(this in LuminTask task)
    {
        return task;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<T> AsValueTask<T>(this in LuminTask<T> task)
    {
        return task;
    }
    
}