using System.Runtime.InteropServices;
using LuminThread;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

public static class Program
{
    public static async LuminTask<int> Add(int a, int b)
    {
        return await LuminTask.Run(() => a + b);
    }
    
    public static async Task<int> Main()
    {
        Console.WriteLine("==================== LuminTask Unit Tests ====================");
#if DEBUG
        Console.WriteLine("Build      : DEBUG  (leak tracker active — LiveCount is meaningful)");
#else
        Console.WriteLine("Build      : RELEASE (Bag live count remains active)");
#endif
        Console.WriteLine($"Runtime    : {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"Processors : {Environment.ProcessorCount}");
        Console.WriteLine("==============================================================");
        Console.WriteLine();

        var r = new TestRunner();

        Console.WriteLine("[ResultStorage]");
        await ResultStorageTests.RunAll(r);
        Console.WriteLine();
        
        Console.WriteLine("[Basic]");
        await BasicTests.RunAll(r);
        Console.WriteLine();

        Console.WriteLine("[Combinators]");
        await CombinatorTests.RunAll(r);
        Console.WriteLine();

        Console.WriteLine("[CompletionSource]");
        await CompletionSourceTests.RunAll(r);
        Console.WriteLine();

        Console.WriteLine("[AsyncEx]");
        await AsyncExTests.RunAll(r);
        Console.WriteLine();

        Console.WriteLine("[Stress]");
        await StressTests.RunAll(r);
        Console.WriteLine();

        Console.WriteLine("[Leak]");
        await LeakTests.RunAll(r);
        Console.WriteLine();

        Console.WriteLine("==============================================================");
        Console.WriteLine($"RESULT: {r.Passed} passed, {r.Failed} failed");
        if (r.Failures.Count > 0)
        {
            Console.WriteLine("Failures:");
            foreach (var f in r.Failures)
                Console.WriteLine("   - " + f);
        }

        // Let asynchronous cleanup drain before the final leak snapshot.
        await Task.Delay(100);

        var live = LuminTaskBag.GetLiveCount();
        var tracked = LuminTaskLeakTracker.LiveCount;
        Console.WriteLine($"Outstanding LuminTask sources at exit: {live}");
        if (live > 0 || tracked > 0)
        {
            Console.WriteLine($"DEBUG tracker outstanding: {tracked}");
            LuminTaskLeakTracker.DumpLeaks();
        }
        Console.WriteLine("==============================================================");
        
        Console.WriteLine(LuminTaskBag.GetAvailableCount());
        

        return r.Failed == 0 && live == 0 && tracked == 0 ? 0 : 1;
    }
}
