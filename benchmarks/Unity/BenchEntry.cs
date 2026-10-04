using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LuminThread;
using UnityEngine;

public sealed class BenchEntry : MonoBehaviour
{
    private const int Operations = 1_000_000;
    private readonly StringBuilder _report = new StringBuilder();
    private LuminTask _smoke;
    private bool _waiting;
    private float _started;
    private static long _sink;

    private void Start()
    {
        UnityEngine.Debug.Log("Lumin benchmark: start");
        _started = Time.realtimeSinceStartup;
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        _report.AppendLine("unity=" + Application.unityVersion);
        _report.AppendLine("pointer_bytes=" + IntPtr.Size);
        _report.AppendLine("operations=" + Operations);
        _report.AppendLine("allocation_metric=managed heap delta with collection disabled; process-wide, block-granular");
#if ENABLE_IL2CPP
        _report.AppendLine("backend=IL2CPP");
#else
        _report.AppendLine("backend=Mono");
#endif
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            IntResult(20_000);
            VectorResult(20_000);
            QuaternionResult(20_000);
            TaskIntResult(20_000);
            UnityEngine.Debug.Log("Lumin benchmark: warmup complete");
            for (int round = 0; round < 5; round++)
            {
                Measure(round, "handle-int", IntResult);
                Measure(round, "handle-vector3", VectorResult);
                Measure(round, "handle-quaternion", QuaternionResult);
                Measure(round, "Task-int", TaskIntResult);
                UnityEngine.Debug.Log("Lumin benchmark: round " + (round + 1));
            }
            MeasureAllocation("handle-int", IntResult);
            MeasureAllocation("handle-vector3", VectorResult);
            MeasureAllocation("handle-quaternion", QuaternionResult);
            MeasureAllocation("Task-int", TaskIntResult);
        }
        catch (Exception error)
        {
            _report.AppendLine("error=" + error);
            Finish(1);
            return;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        _smoke = PlayerLoopSmoke();
        _waiting = true;
        UnityEngine.Debug.Log("Lumin benchmark: player loop smoke started");
    }

    private void Update()
    {
        if (!_waiting) return;
        if (Time.realtimeSinceStartup - _started > 60)
        {
            _report.AppendLine("error=watchdog");
            Finish(1);
            return;
        }
        if (!_smoke.IsCompleted) return;
        try
        {
            _smoke.GetAwaiter().GetResult();
            _report.AppendLine("player_loop=pass");
            _report.AppendLine("live=" + LuminTaskBag.GetLiveCount());
            _report.AppendLine("checksum=" + _sink);
            Finish(LuminTaskBag.GetLiveCount() == 0 ? 0 : 1);
        }
        catch (Exception error)
        {
            _report.AppendLine("error=" + error);
            Finish(1);
        }
    }

    private void Measure(int round, string name, Action<int> run)
    {
        int gen0 = GC.CollectionCount(0);
        long start = Stopwatch.GetTimestamp();
        run(Operations);
        long elapsed = Stopwatch.GetTimestamp() - start;
        _report.AppendLine((round + 1) + "," + name + "," +
            (elapsed * 1e9 / Stopwatch.Frequency / Operations).ToString("F2") + " ns/op," +
            "collections=" + (GC.CollectionCount(0) - gen0));
    }

    private void MeasureAllocation(string name, Action<int> run)
    {
        const int count = 100_000;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var previous = UnityEngine.Scripting.GarbageCollector.GCMode;
        long bytes;
        try
        {
            UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine.Scripting.GarbageCollector.Mode.Disabled;
            bytes = GC.GetTotalMemory(false);
            run(count);
            bytes = GC.GetTotalMemory(false) - bytes;
        }
        finally
        {
            UnityEngine.Scripting.GarbageCollector.GCMode = previous;
        }
        _report.AppendLine("allocation," + name + "," + (bytes / (double)count).ToString("F4") + " heap_B/op");
    }

    private static async LuminTask<T> AwaitLumin<T>(LuminTask<T> task) => await task;
    private static async Task<int> AwaitTask(Task<int> task) => await task;

    private static void IntResult(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var task = AwaitLumin(LuminTaskSource.Create<int>(out var handle));
            LuminTaskSource.SetResult(handle, i + 1000);
            _sink += task.GetAwaiter().GetResult();
        }
    }

    private static void VectorResult(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var task = AwaitLumin(LuminTaskSource.Create<Vector3>(out var handle));
            LuminTaskSource.SetResult(handle, new Vector3(i, -i, 123));
            var value = task.GetAwaiter().GetResult();
            if (value.x != i || value.y != -i || value.z != 123) throw new InvalidOperationException("vector result");
            _sink += (int)value.x;
        }
    }

    private static void QuaternionResult(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var task = AwaitLumin(LuminTaskSource.Create<Quaternion>(out var handle));
            LuminTaskSource.SetResult(handle, new Quaternion(i, -i, 1, 2));
            var value = task.GetAwaiter().GetResult();
            if (value.x != i || value.y != -i || value.z != 1 || value.w != 2) throw new InvalidOperationException("quaternion result");
            _sink += (int)value.x;
        }
    }

    private static void TaskIntResult(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var source = new TaskCompletionSource<int>();
            var task = AwaitTask(source.Task);
            source.SetResult(i + 1000);
            _sink += task.GetAwaiter().GetResult();
        }
    }

    private static async LuminTask PlayerLoopSmoke()
    {
        for (int i = 0; i < 100; i++) await LuminTask.Yield(PlayerLoopTiming.Update);
        await LuminTask.DelayFrame(2, PlayerLoopTiming.Update);
        await LuminTask.WaitUntil(new Vector3(1, 2, 3), value => value.z == 3, PlayerLoopTiming.Update);
        using (var cancellation = new CancellationTokenSource())
        {
            var pending = LuminTask.Never(cancellation.Token);
            cancellation.Cancel();
            try { await pending; throw new InvalidOperationException("missing cancellation"); }
            catch (OperationCanceledException) { }
        }
    }

    private void Finish(int code)
    {
        _waiting = false;
        string output = Path.Combine(Application.persistentDataPath, "lumin-extreme.txt");
        var arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < arguments.Length; i++)
            if (arguments[i] == "-benchOut") output = arguments[i + 1];
        File.WriteAllText(output, _report.ToString());
        UnityEngine.Debug.Log(_report.ToString());
        Application.Quit(code);
    }
}
