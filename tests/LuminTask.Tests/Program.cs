using System.Runtime.InteropServices;
using LuminThread;
using LuminThread.Utility;

namespace LuminTaskUnitTest;

public static class Program
{
    public static async Task<int> Main()
    {
        Console.WriteLine("==================== LuminTask Unit Tests ====================");
#if DEBUG
        Console.WriteLine("Build      : DEBUG  (leak tracker active — LiveCount is meaningful)");
#else
        Console.WriteLine("Build      : RELEASE (leak tracker compiled out — LiveCount always 0)");
#endif
        Console.WriteLine($"Runtime    : {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"Processors : {Environment.ProcessorCount}");
        Console.WriteLine("==============================================================");
        Console.WriteLine();

        var r = new TestRunner();

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

        // Give any fire-and-forget continuations a beat to drain before the final leak snapshot.
        await Task.Delay(100);

        var live = LuminTaskLeakTracker.LiveCount;
        Console.WriteLine($"Outstanding LuminTask sources at exit: {live}");
        if (live > 0)
        {
            Console.WriteLine("(In DEBUG these are real leaks — stacks below. In RELEASE this is always 0.)");
            LuminTaskLeakTracker.DumpLeaks();
        }
        Console.WriteLine("==============================================================");
        
        Console.WriteLine(LuminTaskBag.GetAvailableCount());
        

        return r.Failed == 0 ? 0 : 1;
    }
}
