using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LuminThread.CompilerServices;
using LuminThread.Interface;
using LuminThread.TaskSource;

namespace LuminThread;

public enum LuminTaskModel
{
    Safe,
    Unsafe,
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VTable
{
    public static readonly VTable Null = new (); 
    
    [FieldOffset(0)]
    public delegate*<void*, short, void> GetResult;
    [FieldOffset(8)]
    public delegate*<void*, short, LuminTaskStatus> GetStatus;
    [FieldOffset(16)]
    public delegate*<void*, LuminTaskStatus> UnsafeGetStatus;
    [FieldOffset(24)]
    public delegate*<void*, Action<object>, object, short, void> OnCompleted;
    [FieldOffset(32)]
    public delegate*<void*, bool> TrySetResult;
    [FieldOffset(40)]
    public delegate*<void*, Exception, bool> TrySetException;
    [FieldOffset(48)]
    public delegate*<void*, bool> TrySetCanceled;
    [FieldOffset(56)]
    public delegate*<void*, void> Dispose;
    
    
    [FieldOffset(0)]
    public void* GetResultValue;
}

[AsyncMethodBuilder(typeof(AsyncLuminTaskMethodBuilder))]
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe partial struct LuminTask
{
    private readonly VTable _vTable;
    internal readonly void* _taskSource;
    internal readonly short _id;
    
    public short Id => _id;
    
    public static LuminTaskModel Model = LuminTaskModel.Safe;

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask(VTable vTable, void* taskSource, short id)
    {
        _vTable = vTable;
        _taskSource = taskSource;
        _id = id;
    }
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskAwaiter GetAwaiter() => new(_vTable, _taskSource, _id);
    
    public LuminTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _taskSource == null ? LuminTaskStatus.Succeeded : _vTable.GetStatus(_taskSource, _id);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ValueTask(in LuminTask self)
    {
        if (self._taskSource is null)
        {
            return default;
        }
        
        return new ValueTask(new ValueTaskSource(self._vTable, self._taskSource), self._id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LuminTask other)
    {
        return other._taskSource == _taskSource && other._id == _id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        if (_taskSource is null) return "()";
        return "(" + _vTable.UnsafeGetStatus(_taskSource) + ")";
    }
}

[AsyncMethodBuilder(typeof(AsyncLuminTaskMethodBuilder<>))]
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe partial struct LuminTask<T>
{
    private readonly VTable _vTable;
    private readonly void* _taskSource;
    internal readonly short _id;
    internal readonly T? _result;

    public T? Result => _result;
    
    public short Id => _id;
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask(VTable vTable, void* taskSource, short id)
    {
        _vTable = vTable;
        _taskSource = taskSource;
        _id = id;
    }
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTask(T result)
    {
        _vTable = VTable.Null;
        _taskSource = null;
        _id = 0;
        _result = result;
    }
    
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LuminTaskAwaiter<T> GetAwaiter() => new(_vTable, _taskSource, _id, _result);
    
    public LuminTaskStatus Status
    {
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _taskSource == null ? LuminTaskStatus.Succeeded : _vTable.GetStatus(_taskSource, _id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LuminTask(LuminTask<T> self)
    {
        if (self._taskSource is null)
        {
            return LuminTask.CompletedTask();
        }
        
        return new LuminTask(self._vTable, self._taskSource, self._id);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ValueTask<T>(LuminTask<T> self)
    {
        if (self._taskSource is null)
        {
            return default;
        }
        
        return new ValueTask<T>(new ValueTaskSource<T>(self._vTable, self._taskSource), self._id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LuminTask<T> other)
    {
        return other._taskSource == _taskSource && other._id == _id;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LuminTask other)
    {
        return other._taskSource == _taskSource && other._id == _id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string? ToString()
    {
        return _taskSource is null ? _result?.ToString()
            : "(" + _vTable.UnsafeGetStatus(_taskSource) + ")";
    }
}