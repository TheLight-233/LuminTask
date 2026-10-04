using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using LuminThread;
using LuminThread.Interface;

namespace LuminTaskSuspensionBenchmarks;

internal static class Program
{
    private const int BatchSize = 1024;
    private static readonly ulong[] Handles = new ulong[BatchSize];
    private static readonly LuminTask<int>[] Tasks = new LuminTask<int>[BatchSize];
    private static readonly Pump ManualPump = new();
    private static long _sink;

    private static void Main(string[] args)
    {
        int operations = args.Length > 0 ? int.Parse(args[0]) : 1_000_000;
        int rounds = args.Length > 1 ? int.Parse(args[1]) : 5;
        if (operations < BatchSize || rounds < 1) throw new ArgumentOutOfRangeException();
        Console.WriteLine($"{RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"operations={operations}; rounds={rounds}; tiered={Environment.GetEnvironmentVariable("DOTNET_TieredCompilation") ?? "default"}");
        Console.WriteLine("Source phases use batches of 1024. Suspended phases use batches of 8 to fit the box pool; complete includes the inner consumer and resumed method.");
        Phases(32_768, false);
        Phases(operations, true);
        Phases(32_768, false, suspended: true);
        Phases(operations, true, suspended: true);
        PlayerLoopHelper.SetStrategy(PlayerLoopTiming.DotNet, ManualPump);

        (string Name, Action<int> Run)[] cases =
        [
            ("source-int", SourceInt),
            ("R1-source-suspend", SourceSuspend),
            ("R3-handle-suspend", HandleSuspend),
            ("handle-vector3", VectorSuspend),
            ("handle-quaternion", QuaternionSuspend),
            ("R2-chain", Chain),
            ("Task-suspend", TaskSuspend),
            ("Task-chain", TaskChain),
            ("Yield-Lumin", YieldLumin),
            ("Yield-Task", YieldTask),
            ("sync-int", SyncInt),
        ];
        if (args.Length > 2)
        {
            var selected = new HashSet<string>(args[2].Split(','), StringComparer.Ordinal);
            cases = Array.FindAll(cases, entry => selected.Contains(entry.Name));
            if (cases.Length == 0) throw new ArgumentException("No matching benchmark cases.");
        }
        foreach (var entry in cases) entry.Run(20_000);
        for (int round = 0; round < rounds; round++)
        {
            // Reverse alternate rounds so one case does not always follow the same GC workload.
            for (int i = 0; i < cases.Length; i++)
            {
                var entry = cases[(round & 1) == 0 ? i : cases.Length - i - 1];
                Measure(round, entry.Name, entry.Run, operations);
            }
        }
        Console.WriteLine($"live={LuminTaskBag.GetLiveCount()}; capacity={LuminTaskBag.Capacity}; checksum={_sink}");
        if (LuminTaskBag.GetLiveCount() != 0) throw new InvalidOperationException("Leaked sources.");
    }

    private static void Phases(int operations, bool print, bool suspended = false)
    {
        long rent = 0, complete = 0, consume = 0;
        int size = suspended ? 8 : BatchSize;
        int batches = operations / size;
        for (int batch = 0; batch < batches; batch++)
        {
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < size; i++)
            {
                var source = LuminTaskSource.Create<int>(out Handles[i]);
                Tasks[i] = suspended ? AwaitLumin(source) : source;
            }
            long rented = Stopwatch.GetTimestamp();
            for (int i = 0; i < size; i++) LuminTaskSource.SetResult(Handles[i], i + 1000);
            long completed = Stopwatch.GetTimestamp();
            for (int i = 0; i < size; i++) _sink += Tasks[i].GetAwaiter().GetResult();
            long consumed = Stopwatch.GetTimestamp();
            rent += rented - start;
            complete += completed - rented;
            consume += consumed - completed;
        }
        if (print)
        {
            double scale = 1e9 / Stopwatch.Frequency / (batches * size);
            Console.WriteLine($"phases {(suspended ? "suspended" : "source")} ns/op: rent={rent * scale:F2}; complete={complete * scale:F2}; consume={consume * scale:F2}");
        }
    }

    private static void Measure(int round, string name, Action<int> run, int count)
    {
        long bytes = GC.GetAllocatedBytesForCurrentThread();
        int gen0 = GC.CollectionCount(0);
        long start = Stopwatch.GetTimestamp();
        run(count);
        long elapsed = Stopwatch.GetTimestamp() - start;
        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        gen0 = GC.CollectionCount(0) - gen0;
        Console.WriteLine($"{round + 1},{name},{elapsed * 1e9 / Stopwatch.Frequency / count:F2} ns/op,{bytes / (double)count:F2} B/op,gen0={gen0}");
    }

    private static async LuminTask<int> AwaitLumin(LuminTask<int> task) => await task;
    private static async Task<int> AwaitTask(Task<int> task) => await task;
    private static async LuminTask<Vector3> AwaitVector(LuminTask<Vector3> task) => await task;
    private static async LuminTask<Quaternion> AwaitQuaternion(LuminTask<Quaternion> task) => await task;

    private static void VectorSuspend(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var task = AwaitVector(LuminTaskSource.Create<Vector3>(out var handle));
            LuminTaskSource.SetResult(handle, new Vector3(i, -i, 123));
            var value = task.GetAwaiter().GetResult();
            if (value.X != i || value.Y != -i || value.Z != 123) throw new InvalidOperationException("Vector result");
            _sink += (int)value.X;
        }
    }

    private static void QuaternionSuspend(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var task = AwaitQuaternion(LuminTaskSource.Create<Quaternion>(out var handle));
            LuminTaskSource.SetResult(handle, new Quaternion(i, -i, 1, 2));
            var value = task.GetAwaiter().GetResult();
            if (value.X != i || value.Y != -i || value.Z != 1 || value.W != 2) throw new InvalidOperationException("Quaternion result");
            _sink += (int)value.X;
        }
    }

    private static void SourceInt(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var task = LuminTaskSource.Create<int>(out var handle);
            LuminTaskSource.SetResult(handle, i + 1000);
            _sink += task.GetAwaiter().GetResult();
        }
    }

    private static void SourceSuspend(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var source = new LuminTaskCompletionSource<int>();
            var task = AwaitLumin(source.Task);
            source.TrySetResult(i + 1000);
            _sink += task.GetAwaiter().GetResult();
        }
    }

    private static void HandleSuspend(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var source = LuminTaskSource.Create<int>(out var handle);
            var task = AwaitLumin(source);
            LuminTaskSource.SetResult(handle, i + 1000);
            _sink += task.GetAwaiter().GetResult();
        }
    }

    private static void TaskSuspend(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var source = new TaskCompletionSource<int>();
            var task = AwaitTask(source.Task);
            source.SetResult(i + 1000);
            _sink += task.GetAwaiter().GetResult();
        }
    }

    private readonly record struct Big(long A, long B, long C, long D, long E, long F, long G, long H, long I);

    private static async LuminTask<long> AwaitChain(LuminTask<int> a, LuminTask<string> b, LuminTask<Big> c)
        => await a + (await b).Length + (await c).A;

    private static async Task<long> AwaitTaskChain(Task<int> a, Task<string> b, Task<Big> c)
        => await a + (await b).Length + (await c).A;

    private static void Chain(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var a = new LuminTaskCompletionSource<int>();
            var b = new LuminTaskCompletionSource<string>();
            var c = new LuminTaskCompletionSource<Big>();
            var task = AwaitChain(a.Task, b.Task, c.Task);
            a.TrySetResult(i + 1000);
            b.TrySetResult("lumin");
            c.TrySetResult(new Big(i, 2, 3, 4, 5, 6, 7, 8, 9));
            _sink += task.GetAwaiter().GetResult();
        }
    }

    private static void TaskChain(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var a = new TaskCompletionSource<int>();
            var b = new TaskCompletionSource<string>();
            var c = new TaskCompletionSource<Big>();
            var task = AwaitTaskChain(a.Task, b.Task, c.Task);
            a.SetResult(i + 1000);
            b.SetResult("lumin");
            c.SetResult(new Big(i, 2, 3, 4, 5, 6, 7, 8, 9));
            _sink += task.GetAwaiter().GetResult();
        }
    }

    private static void SyncInt(int count)
    {
        for (int i = 0; i < count; i++) _sink += LuminTask.FromResult(i + 1000).GetAwaiter().GetResult();
    }

    private static async LuminTask<int> YieldLuminLoop(int count)
    {
        for (int i = 0; i < count; i++) await LuminTask.Yield();
        return count;
    }

    private static async Task<int> YieldTaskLoop(int count)
    {
        for (int i = 0; i < count; i++) await Task.Yield();
        return count;
    }

    private static void YieldLumin(int count)
    {
        var task = YieldLuminLoop(count);
        while (!task.IsCompleted) ManualPump.RunCore();
        _sink += task.GetAwaiter().GetResult();
    }

    private static void YieldTask(int count)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(ManualPump);
        try
        {
            var task = YieldTaskLoop(count);
            while (!task.IsCompleted) ManualPump.RunCore();
            _sink += task.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class Pump : SynchronizationContext, IPlayLoopStrategy
    {
        private readonly Queue<(LuminTaskState State, MoveNext Next)> _actions = new();
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _callbacks = new();
        public void AddAction(LuminTaskState state, MoveNext moveNext) => _actions.Enqueue((state, moveNext));
        public void AddContinuation(Action action) => Post(static state => ((Action)state!)(), action);
        public override void Post(SendOrPostCallback callback, object? state) => _callbacks.Enqueue((callback, state));
        public void Dispose() { }

        public void RunCore()
        {
            int count = _actions.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = _actions.Dequeue();
                if (entry.Next(in entry.State)) _actions.Enqueue(entry);
            }
            count = _callbacks.Count;
            for (int i = 0; i < count; i++)
            {
                var entry = _callbacks.Dequeue();
                entry.Callback(entry.State);
            }
        }
    }
}
