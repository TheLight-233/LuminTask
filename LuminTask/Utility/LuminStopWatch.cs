using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LuminThread.Utility;

public struct LuminStopWatch
{
    private long _startTimestamp;
    private long _endTimestamp;
    private TimerState _state;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Start()
    {
        _state = TimerState.Running;
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Stop()
    {
        if (_state != TimerState.Running)
            throw new InvalidOperationException("Timer not running");
        
        _endTimestamp = Stopwatch.GetTimestamp();
        _state = TimerState.Stopped;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Restart()
    {
        _state = TimerState.Running;
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    public readonly TimeSpan Elapsed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            long end = _state == TimerState.Running 
                ? Stopwatch.GetTimestamp() 
                : _endTimestamp;
            
            long elapsedTicks = end - _startTimestamp;
            return TimeSpan.FromTicks((long)((double)elapsedTicks * TimeSpan.TicksPerSecond / Stopwatch.Frequency));
        }
    }

    public readonly double ElapsedMilliseconds
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            long end = _state == TimerState.Running 
                ? Stopwatch.GetTimestamp() 
                : _endTimestamp;
            
            long elapsedTicks = end - _startTimestamp;
            return (elapsedTicks * 1000.0) / Stopwatch.Frequency;
        }
    }

    public readonly double ElapsedMicroseconds
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            long end = _state == TimerState.Running 
                ? Stopwatch.GetTimestamp() 
                : _endTimestamp;
            
            long elapsedTicks = end - _startTimestamp;
            return (elapsedTicks * 1_000_000.0) / Stopwatch.Frequency;
        }
    }

    public readonly bool IsRunning => _state == TimerState.Running;
    public readonly bool IsStopped => _state == TimerState.Stopped;

    private enum TimerState : byte
    {
        Initial,
        Running,
        Stopped
    }
}