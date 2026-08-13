using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LuminThread.Analyzer
{
    /// <summary>
    /// Roslyn source generator that enforces the LuminTask single-consumption contract at
    /// compile time. Reports LUM1001–LUM1006 as build diagnostics (most as errors), so a
    /// contract violation fails the build instead of leaking native memory at runtime.
    ///
    /// Use it by adding the analyzer project/package to the consuming project:
    /// <code>
    /// &lt;ProjectReference Include="..\..\analyzer\LuminTask.Analyzer\LuminTask.Analyzer.csproj"
    ///     OutputItemType="Analyzer" ReferenceOutputAssembly="false" /&gt;
    /// </code>
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class LuminTaskContractGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
            {
                new ContractEngine(compilation, spc.ReportDiagnostic).Run();
            });
        }
    }
}
