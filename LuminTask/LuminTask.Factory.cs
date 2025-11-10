using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;
using LuminThread.TaskSource.Promise;
using LuminThread.Utility;

namespace LuminThread;

public readonly unsafe partial struct LuminTask
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask CompletedTask() => new ();
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask CanceledTask()
    {
        var source = CanceledResultSource<AsyncUnit>.Create(CancellationToken.None);
        
        return new LuminTask(CanceledResultSource<AsyncUnit>.MethodTable,
            source , source->Id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask FromResult() => new(VTable.Null, null, 0);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromResult<T>(T value) => new (value);
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask FromException(Exception ex)
    {
        if (ex is OperationCanceledException oce)
        {
            return FromCanceled(oce.CancellationToken);
        }

        var source = ExceptionResultSource<AsyncUnit>.Create(ex);
        
        return new LuminTask(ExceptionResultSource<AsyncUnit>.MethodTable, source, source->Id);
    }
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromException<T>(Exception ex)
    {
        if (ex is OperationCanceledException oce)
        {
            return FromCanceled<T>(oce.CancellationToken);
        }

        var source = ExceptionResultSource<T>.Create(ex);
        
        return new LuminTask<T>(ExceptionResultSource<T>.MethodTable, source, source->Id);
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask FromCanceled(CancellationToken cancellationToken = default)
    {
        if (cancellationToken == CancellationToken.None)
        {
            return CanceledTask();
        }
        else
        {
            var source = CanceledResultSource<AsyncUnit>.Create(cancellationToken);
            return new LuminTask(CanceledResultSource<AsyncUnit>.MethodTable, source , source->Id);
        }
    }

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromCanceled<T>(CancellationToken cancellationToken = default)
    {
        var source = CanceledResultSource<T>.Create(cancellationToken);
        return new LuminTask<T>(CanceledResultSource<T>.MethodTable, source , source->Id);
    }
    
    /// <summary>
    /// Never complete.
    /// </summary>
    public static LuminTask Never(CancellationToken cancellationToken)
    {
        var source = NeverPromise<AsyncUnit>.Create(cancellationToken);
        return new LuminTask(NeverPromise<AsyncUnit>.MethodTable, source , source->Id);
    }

    /// <summary>
    /// Never complete.
    /// </summary>
    public static LuminTask<T> Never<T>(CancellationToken cancellationToken)
    {
        var source = NeverPromise<T>.Create(cancellationToken);
        return new LuminTask<T>(NeverPromise<T>.MethodTable, source , source->Id);
    }
}

public readonly ref partial struct LuminTask<T>
{
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<T> FromResult(T result) => new(result);
}


internal static class CompletedTasks
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<AsyncUnit> AsyncUnit() => LuminTask<AsyncUnit>.FromResult(Utility.AsyncUnit.Default);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<bool> True() => LuminTask<bool>.FromResult(true);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<bool> False() => LuminTask<bool>.FromResult(false);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> Zero() => LuminTask<int>.FromResult(0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> MinusOne() => LuminTask<int>.FromResult(-1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LuminTask<int> One() => LuminTask<int>.FromResult(1);
}