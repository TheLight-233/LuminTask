using System.Collections.Immutable;
using LuminThread;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LuminThread.Analyzer;

namespace LuminTaskAnalyzerTests;

/// <summary>
/// In-memory test runner for LuminTask.Analyzer: compiles snippets with the contract
/// generator applied and asserts the LUMxxxx diagnostics that surface.
/// </summary>
public static class Program
{
    private static int s_passed;
    private static int s_failed;
    private static readonly List<string> s_failures = new();


    public static async LuminTask<int> Add(int a, int b)
    {
        await LuminTask.Delay(100);
        return await LuminTask.FromResult(a + b);
    } 
        
    public static int Main()
    {
        Console.WriteLine("==================== LuminTask.Analyzer Tests ====================");
        Console.WriteLine($"Runtime : {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"LuminTask ref: {s_luminTaskRefs[0].Display}");
        Console.WriteLine("==============================================================");
        Console.WriteLine();

        // --- clean contracts --------------------------------------------------------
        Run("await 一次 → 干净", "var t = GetTask(); await t;", Array.Empty<string>());
        Run("Forget → 干净", "var t = GetTask(); t.Forget();", Array.Empty<string>());
        Run("if/else 两分支都消费 → 干净", "var t = GetTask(); if (c) await t; else t.Forget();", Array.Empty<string>());
        Run("WhenAll 转移 + await 结果 → 干净", "var t = GetTask(); var t2 = GetTask(); await LuminTask.WhenAll(t, t2);", Array.Empty<string>());
        Run("FromResult().Result → 干净", "var x = LuminTask.FromResult(5).Result; Console.WriteLine(x);", Array.Empty<string>());
        Run("TCS 完成 + await Task → 干净", "var tcs = new LuminTaskCompletionSource<int>(); tcs.TrySetResult(1); await tcs.Task;", Array.Empty<string>());
        Run("TCS 转移给方法 → 干净", "var tcs = new LuminTaskCompletionSource<int>(); SinkTcs(tcs);", Array.Empty<string>());
        Run("循环内 TCS 完成 + 消费 → 干净", "while (c) { var tcs = new LuminTaskCompletionSource<int>(); tcs.TrySetResult(1); await tcs.Task; }", Array.Empty<string>());
        Run("TCS 在 lambda 中完成 → 干净", "var tcs = new LuminTaskCompletionSource<int>(); Action cb = () => tcs.TrySetResult(1); cb(); await tcs.Task;", Array.Empty<string>());
        Run("循环内重新赋值 → 干净", "LuminTask<int> t; while (c) { t = GetTask(); await t; }", Array.Empty<string>());
        Run("GetAwaiter().GetResult() → 干净", "var t = GetTask(); t.GetAwaiter().GetResult();", Array.Empty<string>());
        Run("yield return 转移 → 干净", "yield return GetTask();", Array.Empty<string>(), iterator: true);
        Run("async LuminTask 方法内部 await → 干净", "await t;", Array.Empty<string>(), asyncBody: true);
        Run("WaitAsync 链式 + await → 干净", "var t = GetTask(); await t.WaitAsync(ct);", Array.Empty<string>());
        Run("AsTask 消费 → 干净", "var t = GetTask(); await t.AsTask();", Array.Empty<string>());
        Run("AsValueTask 消费 → 干净", "var t = GetTask(); await t.AsValueTask();", Array.Empty<string>());
        Run("ConfigureAwait(false) 后 await → 干净", "var t = GetTask(); await t.ConfigureAwait(false);", Array.Empty<string>());
        Run("泛型 ConfigureAwait 后 await → 干净", "var t = GetTask(); await t.ConfigureAwait(true);", Array.Empty<string>());
        Run("Unwrap 消费外层任务 → 干净", "var o = GetOuter(); await o.Unwrap();", Array.Empty<string>());
        Run("TCS 经 Handle + SetResult 完成 → 干净", "var tcs = new LuminTaskCompletionSource<int>(); LuminTaskSource.SetResult(tcs.Handle, 1); await tcs.Task;", Array.Empty<string>());
        Run("TCS 经 Handle + SetCanceled 完成 → 干净", "var tcs = new LuminTaskCompletionSource<int>(); LuminTaskSource.SetCanceled(tcs.Handle, ct); await tcs.Task;", Array.Empty<string>());
        Run("Rent + 回调 SetResult + ToTask 消费 → 干净", "LuminTaskSource.Rent<int>(out var h); Action cb = () => LuminTaskSource.SetResult(h, 1); cb(); var t = LuminTaskSource.ToTask<int>(h); await t;", Array.Empty<string>());
        Run("Rent + 直接 SetResult 完成 → 干净", "LuminTaskSource.Rent<int>(out var h); LuminTaskSource.SetResult(h, 1);", Array.Empty<string>());
        Run("普通 out var 不受 LUM1008 检查 → 干净", "int.TryParse(\"1\", out var parsed);", Array.Empty<string>());
        Run("ContinueWith 消费 → 干净", "var t = GetTask(); await t.ContinueWith(_ => { });", Array.Empty<string>());
        Run("Timeout 消费 → 干净", "var t = GetTask(); await t.Timeout(TimeSpan.FromSeconds(1));", Array.Empty<string>());
        Run("AttachExternalCancellation 消费 → 干净", "var t = GetTask(); await t.AttachExternalCancellation(ct);", Array.Empty<string>());
        Run("SuppressCancellationThrow 消费 → 干净", "var t = GetTask(); await t.SuppressCancellationThrow();", Array.Empty<string>());
        Run("try/finally 消费 → 干净", "var t = GetTask(); try { await t; } finally { Console.WriteLine(1); }", Array.Empty<string>());
        Run("嵌套循环 break/continue 消费 → LUM1001/LUM1002", "var t = GetTask(); while (c) { for (;;) { await t; break; } continue; }", new[] { "LUM1001", "LUM1002" });
        Run("return 转移 → 干净", "return t;", Array.Empty<string>(), returnsTask: true);
        Run("foreach 元素消费 → 干净", "foreach (var x in tasks) { await x; }", Array.Empty<string>());
        Run("被 lambda 捕获 → 干净", "var t = GetTask(); Func<Task> f = async () => { await t; };", Array.Empty<string>());
        Run("lambda 消费任务 → 干净", "var t = GetTask(); Action f = () => t.Forget(); f();", Array.Empty<string>());
        Run("跨方法直接转发 → 干净", "var t = GetTask(); await Pass(t);", Array.Empty<string>());
        Run("switch 模式分支消费 → 干净", "var t = GetTask(); switch (n) { case > 0: await t; break; default: t.Forget(); break; }", Array.Empty<string>());
        Run("提前 return 转移后再 await → 干净", "if (c) return t; await t;", Array.Empty<string>(), asyncReturnsTask: true);
        Run("分支消费后 return，另一分支 await → 干净", "if (c) { await t; return default!; } await t;", Array.Empty<string>(), asyncReturnsTask: true);
        Run("if/else return 后继续路径双消费 → LUM1002", "if (a) { await t; } else { return default!; } await t;", new[] { "LUM1002" }, asyncReturnsTask: true);
        Run("字段存储 LuminTask → LUM1007", "var t = GetTask(); await t;", new[] { "LUM1007" }, hasTaskField: true);
        Run("未引用 LuminTask → 不报任何诊断", "int x = 1;", Array.Empty<string>(), noReference: true);

        // --- violations -------------------------------------------------------------
        Run("从未消费 → LUM1001", "var t = GetTask();", new[] { "LUM1001" });
        Run("条件消费（部分路径）→ LUM1001", "var t = GetTask(); if (c) await t;", new[] { "LUM1001" });
        Run("重复 await → LUM1002", "var t = GetTask(); await t; await t;", new[] { "LUM1002" });
        Run("副本 + 双消费（别名）→ LUM1002", "var t = GetTask(); var copy = t; await t; await copy;", new[] { "LUM1002" });
        Run("先转移再 await → LUM1002", "var t = GetTask(); Sink(t); await t;", new[] { "LUM1002" });
        Run("循环内消费未重赋值 → LUM1002", "var t = GetTask(); await t; while (c) { await t; }", new[] { "LUM1002" });
        Run("Result 访问 → LUM1003", "var t = GetTask(); await t; var x = t.Result; Console.WriteLine(x);", new[] { "LUM1003" });
        Run("TCS 从未完成 → LUM1004", "var tcs = new LuminTaskCompletionSource<int>();", new[] { "LUM1004" });
        Run("TCS 完成两次 → LUM1006", "var tcs = new LuminTaskCompletionSource<int>(); tcs.TrySetResult(1); tcs.TrySetResult(2);", new[] { "LUM1006" });
        Run("丢弃符 → LUM1005", "var t = GetTask(); _ = t;", new[] { "LUM1005", "LUM1001" });
        Run("裸 GetTask() 调用 → LUM1001", "GetTask();", new[] { "LUM1001" });
        Run("裸 tcs.Task 访问 → LUM1001", "var tcs = new LuminTaskCompletionSource<int>(); tcs.Task;", new[] { "LUM1001", "LUM1004" });
        Run("LuminTaskSource.Create 后未消费 → LUM1001", "var t = LuminTaskSource.Create<int>(out var h); LuminTaskSource.SetResult(h, 1);", new[] { "LUM1001" });
        Run("Rent 后句柄从未被引用 → LUM1008", "LuminTaskSource.Rent<int>(out var h);", new[] { "LUM1008" });
        Run("Create 消费任务但句柄未引用 → LUM1008", "var t = LuminTaskSource.Create<int>(out var h); await t;", new[] { "LUM1008" });
        Run("循环内声明后未消费 → LUM1001", "while (c) { var x = GetTask(); }", new[] { "LUM1001" });
        Run("if/else 双消费同一分支 → LUM1002", "var t = GetTask(); if (a) { await t; await t; } else { await t; }", new[] { "LUM1002" });
        Run("lambda 仅捕获未消费 → LUM1001", "var t = GetTask(); Action f = () => Console.WriteLine(t.GetId()); f();", new[] { "LUM1001" });
        Run("跨方法吞掉任务 → LUM1001", "var t = GetTask(); Drop(t);", new[] { "LUM1001" });
        Run("default/null 任务句柄 → 干净", "LuminTask t = default; LuminTask? n = null;", Array.Empty<string>());

        Console.WriteLine();
        Console.WriteLine("==============================================================");
        Console.WriteLine($"RESULT: {s_passed} passed, {s_failed} failed");
        foreach (var f in s_failures)
            Console.WriteLine("   - " + f);
        Console.WriteLine("==============================================================");

        return s_failed == 0 ? 0 : 1;
    }

    // --------------------------------------------------------------------------------

    private const string Template = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using System.Collections.Generic;
        using LuminThread;

        class Test
        {
            bool c = true;
            bool a = true;
            int n = 1;
            CancellationToken ct;
            IEnumerable<LuminTask> tasks = Array.Empty<LuminTask>();

            static LuminTask<int> GetTask() => LuminTask.FromResult(1);
            static LuminTask<LuminTask<int>> GetOuter() => LuminTask.FromResult(GetTask());

            static void Sink(LuminTask task) => task.Forget();
            static void SinkTcs(LuminTaskCompletionSource<int> tcs) => tcs.TrySetResult(1);
            static LuminTask Pass(LuminTask task) => task;
            static void Drop(LuminTask task) { }

            async Task M()
            {
        {BODY}
            }
        }
        """;

    private const string IteratorTemplate = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using System.Collections.Generic;
        using LuminThread;

        class Test
        {
            static LuminTask<int> GetTask() => LuminTask.FromResult(1);

            System.Collections.IEnumerator It()
            {
        {BODY}
            }
        }
        """;

    private const string AsyncTemplate = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using LuminThread;

        class Test
        {
            async LuminTask M()
            {
                var t = GetTask();
        {BODY}
            }

            static LuminTask<int> GetTask() => LuminTask.FromResult(1);
        }
        """;

    private const string ReturnTaskTemplate = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using LuminThread;

        class Test
        {
            LuminTask<int> M()
            {
                var t = GetTask();
        {BODY}
            }

            static LuminTask<int> GetTask() => LuminTask.FromResult(1);
        }
        """;

    private const string AsyncReturnTaskTemplate = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using LuminThread;

        class Test
        {
            bool c = true;
            bool a = true;

            async LuminTask<LuminTask<int>> M()
            {
                var t = GetTask();
        {BODY}
                return default!;
            }

            static LuminTask<int> GetTask() => LuminTask.FromResult(1);
        }
        """;

    private const string FieldTemplate = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using LuminThread;

        class Test
        {
            LuminTask<int> _stored;

            async Task M()
            {
        {BODY}
            }

            static LuminTask<int> GetTask() => LuminTask.FromResult(1);
        }
        """;

    private static readonly MetadataReference[] s_luminTaskRefs = new[]
    {
        MetadataReference.CreateFromFile(typeof(LuminThread.LuminTask).Assembly.Location),
    };

    private static List<(string Id, string Msg)> RunGeneratorOn(string code, bool withReference, bool checkSanity)
    {
        var refs = new List<MetadataReference>();
        var tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        foreach (var path in tpa.Split(Path.PathSeparator))
        {
            // TPA includes app-local assemblies (analyzer/test output). They introduce a
            // 'LuminTask.*' namespace that collides with the LuminTask type in the snippets,
            // so keep only framework assemblies here; the library itself is added explicitly.
            var name = Path.GetFileNameWithoutExtension(path);
            if (name.StartsWith("LuminTask", StringComparison.Ordinal))
                continue;
            refs.Add(MetadataReference.CreateFromFile(path));
        }
        if (withReference)
            refs.AddRange(s_luminTaskRefs);

        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(
            "ContractTest",
            new[] { CSharpSyntaxTree.ParseText(code, parseOptions) },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        var driver = CSharpGeneratorDriver.Create(
            new IIncrementalGenerator[] { new LuminTaskContractGenerator() });

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

        // In Roslyn 4.3.0 generator-reported diagnostics surface on the driver diagnostics
        // out-param; later versions also propagate them into the output compilation. Read both.
        var allDiagnostics = output.GetDiagnostics().Concat(diagnostics);

        var lumMessages = allDiagnostics
            .Where(d => d.Id?.StartsWith("LUM", StringComparison.Ordinal) == true)
            .Select(d => (Id: d.Id!, Msg: d.GetMessage()))
            .Distinct()
            .ToList();

        // Sanity: the snippet itself must not contain unrelated compile errors.
        if (checkSanity)
        {
            var errors = allDiagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error &&
                            !d.Id.StartsWith("LUM", StringComparison.Ordinal))
                .ToList();
            foreach (var e in errors.Take(3))
            {
                var loc = e.Location.GetLineSpan();
                Console.WriteLine($"      [warn] {e.Id}: {e.GetMessage()}  (行 {loc.StartLinePosition.Line + 1}, 列 {loc.StartLinePosition.Character + 1})");
            }
        }

        return lumMessages;
    }

    private static void Run(
        string name,
        string body,
        string[] expected,
        bool noReference = false,
        bool iterator = false,
        bool asyncBody = false,
        bool returnsTask = false,
        bool asyncReturnsTask = false,
        bool hasTaskField = false)
    {
        string template = iterator ? IteratorTemplate
            : asyncBody ? AsyncTemplate
            : returnsTask ? ReturnTaskTemplate
            : asyncReturnsTask ? AsyncReturnTaskTemplate
            : hasTaskField ? FieldTemplate
            : Template;

        string code = template.Replace("{BODY}", body);
        var allDiagMessages = RunGeneratorOn(code, !noReference, checkSanity: !noReference);
        var actualSet = allDiagMessages.Select(m => m.Id).ToHashSet(StringComparer.Ordinal);

        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);

        bool ok = actualSet.SetEquals(expectedSet);
        if (ok)
        {
            s_passed++;
            Console.WriteLine($"  [PASS] {name}");
        }
        else
        {
            s_failed++;
            var missing = expectedSet.Except(actualSet);
            var extra = actualSet.Except(expectedSet);
            var msg = $"{name} — expected [{string.Join(",", expectedSet)}], got [{string.Join(",", actualSet)}]" +
                      (missing.Any() ? $", missing [{string.Join(",", missing)}]" : "") +
                      (extra.Any() ? $", extra [{string.Join(",", extra)}]" : "");
            if (extra.Any())
            {
                var detail = allDiagMessages.FirstOrDefault(m => m.Id == extra.First());
                if (detail.Id != null) msg += " | detail: " + detail.Msg;
            }
            s_failures.Add(msg);
            Console.WriteLine($"  [FAIL] {msg}");
        }
    }
}
