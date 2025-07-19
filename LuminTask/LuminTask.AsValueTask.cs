using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Lumin.Threading.Interface;

namespace Lumin.Threading.Tasks;

internal static class LuminTaskValueTaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask AsValueTask(this in LuminTask task)
    {
        if (task._source is null)
            return new ValueTask();
            
        return new ValueTask(task._source, task._token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<T> AsValueTask<T>(this in LuminTask<T> task)
    {
        if (task._source is null)
            return new ValueTask<T>(task._result!);
            
        return new ValueTask<T>(task._source, task._token);
    }
    
}