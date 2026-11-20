using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LuminThread.Analyzer
{
    /// <summary>
    /// Roslyn 源生成器，编译期强制 LuminTask 单次消费契约。
    /// 分析按方法/字段粒度通过 SyntaxProvider 管线执行：
    /// 只有发生编辑的文件会重新分析，未变化的输出被增量缓存，且多节点并行分析。
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class LuminTaskContractGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var methods = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is MethodDeclarationSyntax
                    or ConstructorDeclarationSyntax
                    or DestructorDeclarationSyntax
                    or OperatorDeclarationSyntax
                    or ConversionOperatorDeclarationSyntax
                    or AccessorDeclarationSyntax
                    or LocalFunctionStatementSyntax
                    or AnonymousFunctionExpressionSyntax,
                static (ctx, _) => ContractEngine.AnalyzeMethod(ctx.Node, ctx.SemanticModel));

            var fields = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is FieldDeclarationSyntax,
                static (ctx, _) => ContractEngine.AnalyzeField(ctx.Node, ctx.SemanticModel));

            context.RegisterSourceOutput(methods, static (spc, diagnostics) =>
            {
                foreach (var diagnostic in diagnostics)
                    spc.ReportDiagnostic(diagnostic);
            });

            context.RegisterSourceOutput(fields, static (spc, diagnostics) =>
            {
                foreach (var diagnostic in diagnostics)
                    spc.ReportDiagnostic(diagnostic);
            });
        }
    }
}
