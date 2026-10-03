using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LuminThread.Analyzer
{
    /// <summary>LuminTask 相关类型符号，一次解析后供方法级分析复用。</summary>
    internal readonly struct ContractTypes
    {
        internal readonly INamedTypeSymbol? TaskType;
        internal readonly INamedTypeSymbol? TaskOfTType;
        internal readonly INamedTypeSymbol? TcsType;
        internal readonly INamedTypeSymbol? TcsOfTType;

        /// <summary>LuminTaskSource：扩展程序集通过原生 handle 驱动完成源的生产者入口。</summary>
        internal readonly INamedTypeSymbol? SourceType;

        internal bool HasLuminTask => TaskType != null || TaskOfTType != null;

        internal ContractTypes(Compilation compilation)
        {
            TaskType = compilation.GetTypeByMetadataName("LuminThread.LuminTask");
            TaskOfTType = compilation.GetTypeByMetadataName("LuminThread.LuminTask`1");
            TcsType = compilation.GetTypeByMetadataName("LuminThread.LuminTaskCompletionSource");
            TcsOfTType = compilation.GetTypeByMetadataName("LuminThread.LuminTaskCompletionSource`1");
            SourceType = compilation.GetTypeByMetadataName("LuminThread.LuminTaskSource");
        }
    }

    /// <summary>
    /// 契约分析引擎入口：由源生成器的语法管线按单个方法/字段调用，输出诊断。
    /// </summary>
    internal static class ContractEngine
    {
        /// <summary>分析一个方法体（含局部函数、lambda、访问器等），返回契约诊断。</summary>
        internal static ImmutableArray<Diagnostic> AnalyzeMethod(SyntaxNode node, SemanticModel model)
        {
            var types = new ContractTypes(model.Compilation);
            if (!types.HasLuminTask)
                return ImmutableArray<Diagnostic>.Empty;

            var diagnostics = new List<Diagnostic>();
            try
            {
                new MethodAnalysis(model, types, diagnostics.Add).Run(node);
            }
            catch (Exception ex)
            {
                // 分析器内部错误以警告形式呈现，而不是让编译崩溃。
                diagnostics.Add(Diagnostic.Create(ContractDiagnostics.InternalError, node.GetLocation(), ex.Message));
            }
            return diagnostics.ToImmutableArray();
        }

        /// <summary>分析字段声明：LuminTask 字段不受单次消费生命周期跟踪。</summary>
        internal static ImmutableArray<Diagnostic> AnalyzeField(SyntaxNode node, SemanticModel model)
        {
            var types = new ContractTypes(model.Compilation);
            if (!types.HasLuminTask)
                return ImmutableArray<Diagnostic>.Empty;

            var field = (FieldDeclarationSyntax)node;
            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

            foreach (var v in field.Declaration.Variables)
            {
                if (model.GetDeclaredSymbol(v) is not IFieldSymbol symbol)
                    continue;

                if (!IsTaskType(types, symbol.Type))
                    continue;

                diagnostics.Add(Diagnostic.Create(
                    ContractDiagnostics.FieldStoredTask,
                    v.Identifier.GetLocation(),
                    new object[] { symbol.Name, string.Empty }));
            }

            return diagnostics.ToImmutable();
        }

        internal static bool IsTaskType(ContractTypes types, ITypeSymbol? type)
        {
            if (type == null) return false;
            var def = type.OriginalDefinition;
            return (types.TaskType != null && SymbolEqualityComparer.Default.Equals(def, types.TaskType)) ||
                   (types.TaskOfTType != null && SymbolEqualityComparer.Default.Equals(def, types.TaskOfTType));
        }

        internal static bool IsTcsType(ContractTypes types, ITypeSymbol? type)
        {
            if (type == null) return false;
            var def = type.OriginalDefinition;
            return (types.TcsType != null && SymbolEqualityComparer.Default.Equals(def, types.TcsType)) ||
                   (types.TcsOfTType != null && SymbolEqualityComparer.Default.Equals(def, types.TcsOfTType));
        }

        private static bool IsMethodLike(SyntaxNode node)
        {
            return node is MethodDeclarationSyntax
                or ConstructorDeclarationSyntax
                or DestructorDeclarationSyntax
                or OperatorDeclarationSyntax
                or ConversionOperatorDeclarationSyntax
                or AccessorDeclarationSyntax
                or LocalFunctionStatementSyntax
                or AnonymousFunctionExpressionSyntax;
        }

        /// <summary>单次方法分析上下文。</summary>
        private sealed class MethodAnalysis
        {
            private static readonly SymbolEqualityComparer SymEquals = SymbolEqualityComparer.Default;

            // 会释放/接管任务源的成员调用。ConfigureAwait 与 Unwrap 各自返回 awaitable / 内层任务，
            // 真正的消费发生在 await 它们时，因此与 AsTask / WaitAsync 同等对待：调用点即消费点。
            private static readonly HashSet<string> s_consumingTaskApis = new(StringComparer.Ordinal)
            {
                "Forget", "AsTask", "AsValueTask", "ContinueWith", "Timeout", "WaitAsync",
                "AttachExternalCancellation", "SuppressCancellationThrow", "ToCoroutine",
                "ConfigureAwait", "Unwrap",
            };

            private static readonly HashSet<string> s_tcsCompleteApis = new(StringComparer.Ordinal)
            {
                "TrySetResult", "TrySetException", "TrySetCanceled",
            };

            // LuminTaskSource.Set*：以原生 handle 完成完成源，是 TrySet* 之外的第二条完成通道。
            private static readonly HashSet<string> s_handleCompleteApis = new(StringComparer.Ordinal)
            {
                "SetResult", "SetException", "SetCanceled",
            };

            private readonly SemanticModel _model;
            private readonly ContractTypes _types;
            private readonly Action<Diagnostic> _report;

            internal MethodAnalysis(SemanticModel model, ContractTypes types, Action<Diagnostic> report)
            {
                _model = model;
                _types = types;
                _report = report;
            }

            internal void Run(SyntaxNode node)
            {
                if (!IsMethodLike(node)) return;
                AnalyzeMethodLike(node);
            }
            private void AnalyzeMethodLike(SyntaxNode node)
            {
                SyntaxNode? body;
            IEnumerable<ParameterSyntax>? parameters = null;

            switch (node)
            {
                case MethodDeclarationSyntax m:
                    body = m.Body ?? (SyntaxNode?)m.ExpressionBody;
                    parameters = m.ParameterList.Parameters;
                    break;
                case ConstructorDeclarationSyntax c:
                    body = c.Body ?? (SyntaxNode?)c.ExpressionBody;
                    parameters = c.ParameterList.Parameters;
                    break;
                case DestructorDeclarationSyntax d:
                    body = d.Body ?? (SyntaxNode?)d.ExpressionBody;
                    parameters = d.ParameterList.Parameters;
                    break;
                case OperatorDeclarationSyntax o:
                    body = o.Body ?? (SyntaxNode?)o.ExpressionBody;
                    parameters = o.ParameterList.Parameters;
                    break;
                case ConversionOperatorDeclarationSyntax co:
                    body = co.Body ?? (SyntaxNode?)co.ExpressionBody;
                    parameters = co.ParameterList.Parameters;
                    break;
                case AccessorDeclarationSyntax a:
                    body = a.Body ?? (SyntaxNode?)a.ExpressionBody;
                    break;
                case LocalFunctionStatementSyntax lf:
                    body = lf.Body ?? (SyntaxNode?)lf.ExpressionBody;
                    parameters = lf.ParameterList.Parameters;
                    break;
                case AnonymousFunctionExpressionSyntax anon:
                    body = anon.Body;
                    if (anon is AnonymousMethodExpressionSyntax am)
                        parameters = am.ParameterList?.Parameters;
                    else if (anon is ParenthesizedLambdaExpressionSyntax pl)
                        parameters = pl.ParameterList?.Parameters;
                    else if (anon is SimpleLambdaExpressionSyntax sl)
                        parameters = new[] { sl.Parameter };
                    break;
                default:
                    return;
            }

            if (body == null) return;

            var analyzer = new FlowAnalyzer(_model, _types, _report);
            if (parameters != null) analyzer.RegisterParameters(parameters);

            if (body is BlockSyntax block)
            {
                var env = new FlowEnv();
                analyzer.WalkStatements(block.Statements, env, null);
                // Must run after the walk: task groups are registered as declarations are visited.
                analyzer.MarkCapturedAsTrivial(body);
                analyzer.FinishBody(body, env);
            }
            else if (body is ArrowExpressionClauseSyntax arrow)
            {
                var env = new FlowEnv();
                analyzer.AnalyzeExpression(arrow.Expression, env, transfer: true);
                analyzer.MarkCapturedAsTrivial(body);
                analyzer.FinishBody(body, env);
            }
            else if (body is ExpressionSyntax expr)
            {
                var env = new FlowEnv();
                analyzer.AnalyzeExpression(expr, env, transfer: true);
                analyzer.MarkCapturedAsTrivial(body);
                analyzer.FinishBody(body, env);
            }
        }

        // ------------------------------------------------------------------------------------
        // Per-body flow analyzer
        // ------------------------------------------------------------------------------------

        internal sealed class FlowAnalyzer
        {
            private readonly SemanticModel _model;
            private readonly ContractTypes _types;
            private readonly Action<Diagnostic> _report;

            private readonly Dictionary<ISymbol, Group> _taskGroups = new(SymEquals);
            private readonly Dictionary<ISymbol, Group> _tcsGroups = new(SymEquals);
            private readonly List<Group> _allGroups = new();

            // 租出的 LuminTaskHandle 单独记账：它不参与任务/完成源的消费计数，
            // 只要求“在方法体内被引用过”（引用即意味着存在完成路径或已逃逸）。
            private readonly Dictionary<ISymbol, Group> _handleGroups = new(SymEquals);
            private readonly List<Group> _handleGroupList = new();

            internal FlowAnalyzer(
                SemanticModel model,
                ContractTypes types,
                Action<Diagnostic> report)
            {
                _model = model;
                _types = types;
                _report = report;
            }

            /// <summary>A consumption group shared by a task and every alias copy of it.</summary>
            internal sealed class Group
            {
                internal readonly string Name;
                internal readonly Location Declaration;
                internal Location? Site;
                internal int Members = 1;
                internal bool Trivial;      // FromResult / default — nothing to release, never tracked
                internal bool LoopScoped;   // declared inside a loop body — exempt from scope-final checks
                internal bool LoopManaged;  // reset & consumed inside a loop — lifecycle ends in the loop
                internal bool IsTcs;        // completion source (LUM1004/LUM1006) instead of task (LUM1001/LUM1002)
                internal bool CapturedConsumed; // consumed by a lambda/local function
                internal bool IsHandle;     // handle rented by LuminTaskSource.Rent/Create (LUM1008)
                internal bool Used;         // handle referenced anywhere in the body

                internal Group(string name, Location declaration)
                {
                    Name = name;
                    Declaration = declaration;
                }
            }

            internal enum TaskKind
            {
                NotTask,
                TrivialTask,
                NewTask,
                Handle,
            }

            // ----------------------------------------------------------------------------
            // Registration
            // ----------------------------------------------------------------------------

            internal void RegisterParameters(IEnumerable<ParameterSyntax> parameters)
            {
                foreach (var p in parameters)
                {
                    var symbol = _model.GetDeclaredSymbol(p);
                    if (symbol == null) continue;

                    if (IsTaskType(symbol.Type))
                        RegisterTask(symbol, p.Identifier.GetLocation(), symbol.Name);
                    else if (IsTcsType(symbol.Type))
                        RegisterTcs(symbol, p.Identifier.GetLocation(), symbol.Name);
                }
            }

            /// <summary>Accounts for captured tasks only when the lambda actually consumes them.</summary>
            internal void MarkCapturedAsTrivial(SyntaxNode body)
            {
                foreach (var anon in body.DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>())
                {
                    foreach (var id in anon.DescendantNodes().OfType<IdentifierNameSyntax>())
                    {
                        var symbol = ResolveSymbol(id);
                        if (symbol == null) continue;

                        if (!IsConsumedInLambda(anon, id))
                            continue;

                        if (_taskGroups.TryGetValue(symbol, out var g))
                            g.CapturedConsumed = true;
                        else if (_tcsGroups.TryGetValue(symbol, out var tcsG))
                            tcsG.CapturedConsumed = true;
                    }
                }
            }

            private bool IsConsumedInLambda(AnonymousFunctionExpressionSyntax lambda, IdentifierNameSyntax id)
            {
                for (SyntaxNode? current = id.Parent; current != null && current != lambda; current = current.Parent)
                {
                    if (current is AwaitExpressionSyntax)
                        return true;
                    if (current is InvocationExpressionSyntax invocation)
                    {
                        var name = GetInvokedMemberName(invocation);
                        if (s_consumingTaskApis.Contains(name) || name == "GetResult" || s_tcsCompleteApis.Contains(name))
                            return true;
                    }
                    if (current is ArgumentSyntax argument && argument.Parent?.Parent is InvocationExpressionSyntax call)
                    {
                        var method = _model.GetSymbolInfo(call).Symbol as IMethodSymbol;
                        var index = call.ArgumentList.Arguments.IndexOf(argument);
                        if (method != null && index >= 0 && index < method.Parameters.Length &&
                            (IsTaskType(method.Parameters[index].Type) || IsTcsType(method.Parameters[index].Type)))
                            return true;
                    }
                }
                return false;
            }

            // ----------------------------------------------------------------------------
            // Statement walking
            // ----------------------------------------------------------------------------

            /// <summary>Walks statements; returns true when the current path terminates (return / throw / yield / goto).</summary>
            internal bool WalkStatements(IEnumerable<StatementSyntax> statements, FlowEnv env, HashSet<Group>? assignedInLoop)
            {
                foreach (var s in statements)
                {
                    if (WalkStatement(s, env, assignedInLoop))
                        return true;
                }
                return false;
            }

            /// <summary>Walks a statement; returns true when the current path terminates.</summary>
            private bool WalkStatement(StatementSyntax stmt, FlowEnv env, HashSet<Group>? assignedInLoop)
            {
                switch (stmt)
                {
                    case BlockSyntax b:
                        return WalkStatements(b.Statements, env, assignedInLoop);

                    case ExpressionStatementSyntax es:
                        AnalyzeExpression(es.Expression, env, transfer: false, bare: true, assignedInLoop);
                        return false;

                    case LocalDeclarationStatementSyntax d:
                        HandleDeclaration(d.Declaration, env, assignedInLoop);
                        return false;

                    case IfStatementSyntax i:
                        AnalyzeExpression(i.Condition, env, transfer: false, assignedInLoop: assignedInLoop);
                        var thenEnv = env.Clone();
                        bool thenTerm = WalkStatement(i.Statement, thenEnv, assignedInLoop);
                        bool elseTerm = false;
                        FlowEnv? elseEnv = null;
                        if (i.Else != null)
                        {
                            elseEnv = env.Clone();
                            elseTerm = WalkStatement(i.Else.Statement, elseEnv, assignedInLoop);
                        }

                        if (thenTerm && elseTerm && elseEnv != null)
                            return true; // both branches exit; code after the if is unreachable

                        if (thenTerm && elseEnv == null)
                            return false; // the continue-path is the implicit (condition false) branch: main stays unchanged

                        if (thenTerm)
                        {
                            // Only the else branch continues: it represents the surviving path.
                            MergeMany(env, new List<FlowEnv> { elseEnv! }, coverCount: 1);
                            return false;
                        }

                        if (elseTerm)
                        {
                            // Only the then branch continues.
                            MergeMany(env, new List<FlowEnv> { thenEnv }, coverCount: 1);
                            return false;
                        }

                        // Neither branch terminates: normal merge.
                        MergeBranches(env, thenEnv, elseEnv, hasElse: elseEnv != null);
                        return false;

                    case SwitchStatementSyntax sw:
                        AnalyzeExpression(sw.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        var sectionEnvs = new List<FlowEnv>();
                        bool allSectionsTerminate = sw.Sections.Count > 0;
                        foreach (var section in sw.Sections)
                        {
                            var clone = env.Clone();
                            foreach (var label in section.Labels)
                            {
                                switch (label)
                                {
                                    case CaseSwitchLabelSyntax cl:
                                        AnalyzeExpression(cl.Value, clone, transfer: false, assignedInLoop: assignedInLoop);
                                        break;
                                    case CasePatternSwitchLabelSyntax cpl:
                                        AnalyzePattern(cpl.Pattern, clone, assignedInLoop);
                                        if (cpl.WhenClause != null)
                                            AnalyzeExpression(cpl.WhenClause.Condition, clone, transfer: false, assignedInLoop: assignedInLoop);
                                        break;
                                }
                            }
                            bool sectionTerm = WalkStatements(section.Statements, clone, assignedInLoop);
                            if (!sectionTerm)
                            {
                                sectionEnvs.Add(clone);
                                allSectionsTerminate = false;
                            }
                        }

                        if (allSectionsTerminate)
                            return true;

                        bool hasDefault = sw.Sections.Any(s => s.Labels.Any(l => l is DefaultSwitchLabelSyntax));
                        MergeMany(env, sectionEnvs, coverCount: hasDefault ? sectionEnvs.Count : 0);
                        return false;

                    case WhileStatementSyntax w:
                        AnalyzeExpression(w.Condition, env, transfer: false, assignedInLoop: assignedInLoop);
                        WalkLoop(w.Statement, env);
                        return false; // conservative: the loop may exit via its condition/break

                    case DoStatementSyntax d:
                        WalkLoop(d.Statement, env);
                        AnalyzeExpression(d.Condition, env, transfer: false, assignedInLoop: assignedInLoop);
                        return false;

                    case ForStatementSyntax f:
                        if (f.Declaration != null) HandleDeclaration(f.Declaration, env, assignedInLoop);
                        foreach (var init in f.Initializers)
                            AnalyzeExpression(init, env, transfer: false, assignedInLoop: assignedInLoop);
                        AnalyzeExpression(f.Condition, env, transfer: false, assignedInLoop: assignedInLoop);
                        WalkLoop(f.Statement, env);
                        foreach (var inc in f.Incrementors)
                            AnalyzeExpression(inc, env, transfer: false, assignedInLoop: assignedInLoop);
                        return false;

                    case ForEachStatementSyntax fe:
                        AnalyzeExpression(fe.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        // The foreach variable is fresh on every iteration: register it inside
                        // the loop context so consuming it is not flagged as a double-consume.
                        WalkLoop(fe.Statement, env, preRegister: fe);
                        return false;

                    case TryStatementSyntax t:
                        var tryEnv = env.Clone();
                        bool tryTerm = WalkStatement(t.Block, tryEnv, assignedInLoop);
                        var branchEnvs = new List<FlowEnv> { tryEnv };
                        bool allCatchTerm = t.Catches.Count > 0;
                        foreach (var c in t.Catches)
                        {
                            var catchEnv = env.Clone();
                            if (c.Declaration != null)
                                RegisterCatchVariable(c.Declaration);
                            if (c.Filter != null)
                                AnalyzeExpression(c.Filter.FilterExpression, catchEnv, transfer: false, assignedInLoop: assignedInLoop);
                            if (!WalkStatement(c.Block, catchEnv, assignedInLoop))
                                allCatchTerm = false;
                            branchEnvs.Add(catchEnv);
                        }

                        if (t.Finally != null)
                        {
                            // The try block is the main path; catches are exceptional branches.
                            // Finally executes on every path: run it linearly on the merged result.
                            MergeMany(env, branchEnvs, coverCount: tryTerm ? 0 : 1);
                            return WalkStatement(t.Finally.Block, env, assignedInLoop);
                        }

                        if (tryTerm && allCatchTerm)
                            return true; // no path escapes the try/catch region
                        MergeMany(env, branchEnvs, coverCount: tryTerm ? 0 : 1);
                        return false;

                    case ReturnStatementSyntax r:
                        if (r.Expression != null)
                            AnalyzeExpression(r.Expression, env, transfer: true, assignedInLoop: assignedInLoop);
                        return true;

                    case ThrowStatementSyntax th:
                        if (th.Expression != null)
                            AnalyzeExpression(th.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        return true;

                    case YieldStatementSyntax y:
                        if (y.Expression != null)
                            AnalyzeExpression(y.Expression, env, transfer: true, assignedInLoop: assignedInLoop);
                        return true;

                    case LockStatementSyntax l:
                        AnalyzeExpression(l.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        return WalkStatement(l.Statement, env, assignedInLoop);

                    case UsingStatementSyntax u:
                        if (u.Declaration != null) HandleDeclaration(u.Declaration, env, assignedInLoop);
                        if (u.Expression != null)
                            AnalyzeExpression(u.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        return WalkStatement(u.Statement, env, assignedInLoop);

                    case FixedStatementSyntax fx:
                        HandleDeclaration(fx.Declaration, env, assignedInLoop);
                        return WalkStatement(fx.Statement, env, assignedInLoop);

                    case CheckedStatementSyntax ch:
                        return WalkStatement(ch.Block, env, assignedInLoop);

                    case UnsafeStatementSyntax un:
                        return WalkStatement(un.Block, env, assignedInLoop);

                    case LabeledStatementSyntax lb:
                        return WalkStatement(lb.Statement, env, assignedInLoop);

                    case GotoStatementSyntax:
                        return true;

                    case LocalFunctionStatementSyntax:
                    case BreakStatementSyntax:
                    case ContinueStatementSyntax:
                    case EmptyStatementSyntax:
                        return false;
                }

                return false;
            }

            private void WalkLoop(StatementSyntax body, FlowEnv env, ForEachStatementSyntax? preRegister = null)
            {
                var bodyEnv = env.Clone();
                var assignedInBody = new HashSet<Group>();
                if (preRegister != null)
                    HandleForeachVariable(preRegister, assignedInBody);
                WalkStatement(body, bodyEnv, assignedInBody);

                // Consuming a task inside a loop without reassigning it first = consuming
                // the same source again on the second iteration.
                foreach (var g in _allGroups)
                {
                    if (bodyEnv.Get(g) > env.Get(g) && !assignedInBody.Contains(g))
                    {
                        if (g.IsTcs)
                        {
                            Report(ContractDiagnostics.DoubleCompletedSource, g.Site ?? g.Declaration, g.Name,
                                "（完成发生在循环体内，第二次迭代将重复完成）");
                        }
                        else
                        {
                            Report(ContractDiagnostics.DoubleConsumedTask, g.Site ?? g.Declaration, g.Name,
                                "（消费发生在循环体内，第二次迭代将重复消费）");
                        }
                    }
                }

                // A task/completion-source created inside the loop body but never consumed
                // there leaks once per iteration.
                foreach (var g in assignedInBody)
                {
                    if (g.Trivial) continue;
                    if (bodyEnv.Get(g) > 0)
                    {
                        // Reset and consumed within the loop: its lifecycle ends here.
                        g.LoopManaged = true;
                        continue;
                    }

                    string note = bodyEnv.Partial.Contains(g)
                        ? "（在循环体内创建后仅在部分路径上被消费/完成）"
                        : "（在循环体内创建后未消费/完成）";
                    if (g.IsTcs)
                        Report(ContractDiagnostics.UncompletedSource, g.Declaration, g.Name, note);
                    else
                        Report(ContractDiagnostics.UnconsumedTask, g.Declaration, g.Name, note);
                }
            }

            private void MergeBranches(FlowEnv main, FlowEnv thenEnv, FlowEnv? elseEnv, bool hasElse)
            {
                // An if/else pair covers all paths; a bare if does not (the condition may be false).
                MergeMany(main, elseEnv != null
                        ? new List<FlowEnv> { thenEnv, elseEnv }
                        : new List<FlowEnv> { thenEnv },
                    coverCount: hasElse ? 2 : 0);
            }

            /// <summary>
            /// Folds branch environments back into the main path.
            /// <paramref name="coverCount"/> is the number of leading branches that jointly cover
            /// every path (2 for if/else, 1 for a try block, 0 when the union may leave gaps,
            /// e.g. a bare if or a switch without a default section).
            /// </summary>
            private void MergeMany(FlowEnv main, List<FlowEnv> branchEnvs, int coverCount)
            {
                if (branchEnvs.Count == 0) return;

                foreach (var g in _allGroups)
                {
                    int baseCount = main.Get(g);
                    int consumingBranches = 0;
                    int coveredConsuming = 0;
                    bool anyPartial = main.Partial.Contains(g);

                    for (int i = 0; i < branchEnvs.Count; i++)
                    {
                        var branch = branchEnvs[i];
                        if (branch.Get(g) > baseCount)
                        {
                            consumingBranches++;
                            if (i < coverCount) coveredConsuming++;
                        }
                        if (branch.Partial.Contains(g)) anyPartial = true;
                    }

                    // Union the initialized sets: a task assigned on any path is "initialized".
                    for (int i = 0; i < branchEnvs.Count; i++)
                    {
                        foreach (var init in branchEnvs[i].Initialized)
                            main.Initialized.Add(init);
                    }

                    if (coverCount > 0 && coveredConsuming == coverCount)
                    {
                        main.Set(g, baseCount + 1);
                    }
                    else
                    {
                        main.Set(g, baseCount);
                        if (consumingBranches > 0) anyPartial = true;
                    }

                    if (anyPartial) main.Partial.Add(g);
                }
            }

            // ----------------------------------------------------------------------------
            // Expression analysis
            // ----------------------------------------------------------------------------

            internal TaskKind AnalyzeExpression(
                ExpressionSyntax? expr,
                FlowEnv env,
                bool transfer,
                bool bare = false,
                HashSet<Group>? assignedInLoop = null)
            {
                if (expr == null) return TaskKind.NotTask;

                switch (expr)
                {
                    case ParenthesizedExpressionSyntax p:
                        return AnalyzeExpression(p.Expression, env, transfer, bare, assignedInLoop);

                    case CheckedExpressionSyntax c:
                        return AnalyzeExpression(c.Expression, env, transfer, bare, assignedInLoop);

                    case CastExpressionSyntax cast:
                        return AnalyzeExpression(cast.Expression, env, transfer, bare, assignedInLoop);

                    case PostfixUnaryExpressionSyntax post when post.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                        return AnalyzeExpression(post.Operand, env, transfer, bare, assignedInLoop);

                    case IdentifierNameSyntax id:
                        if (TryGetTaskGroup(id, out var g))
                        {
                            if (transfer) Consume(g, id.GetLocation(), env);
                            return TaskKind.Handle;
                        }
                        if (TryGetTcsGroup(id, out var tcsG))
                        {
                            if (transfer) TransferTcs(tcsG, id.GetLocation(), env);
                            return TaskKind.NotTask;
                        }
                        return TaskKind.NotTask;

                    case DefaultExpressionSyntax:
                    case LiteralExpressionSyntax when expr.IsKind(SyntaxKind.DefaultLiteralExpression):
                        return IsTaskType(_model.GetTypeInfo(expr).Type) ? TaskKind.TrivialTask : TaskKind.NotTask;

                    case LiteralExpressionSyntax lit when lit.IsKind(SyntaxKind.NullLiteralExpression):
                        return IsTaskType(_model.GetTypeInfo(expr).Type) ? TaskKind.TrivialTask : TaskKind.NotTask;

                    case AssignmentExpressionSyntax a:
                        return AnalyzeAssignment(a, env, bare, assignedInLoop);

                    case AwaitExpressionSyntax aw:
                        if (TryGetTaskGroup(aw.Expression, out var ag))
                        {
                            Consume(ag, aw.GetLocation(), env);
                        }
                        else
                        {
                            AnalyzeExpression(aw.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        }
                        return TaskKind.NotTask;

                    case InvocationExpressionSyntax inv:
                        return AnalyzeInvocation(inv, env, bare, assignedInLoop);

                    case MemberAccessExpressionSyntax ma:
                        return AnalyzeMemberAccess(ma, env, bare, assignedInLoop);

                    case ObjectCreationExpressionSyntax oc:
                        if (IsTcsType(_model.GetTypeInfo(oc).Type))
                        {
                            if (bare)
                            {
                                Report(ContractDiagnostics.UncompletedSource, oc.GetLocation(), "unnamed",
                                    "（创建后未完成也未转移）");
                            }
                            return TaskKind.NotTask;
                        }
                        foreach (var arg in oc.ArgumentList?.Arguments ?? Enumerable.Empty<ArgumentSyntax>())
                            AnalyzeExpression(arg.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        return TaskKind.NotTask;

                    case ConditionalExpressionSyntax cond:
                        AnalyzeExpression(cond.Condition, env, transfer: false, assignedInLoop: assignedInLoop);
                        var thenKind = AnalyzeExpression(cond.WhenTrue, env, transfer: true, assignedInLoop: assignedInLoop);
                        var elseKind = AnalyzeExpression(cond.WhenFalse, env, transfer: true, assignedInLoop: assignedInLoop);
                        return (thenKind != TaskKind.NotTask || elseKind != TaskKind.NotTask)
                            ? TaskKind.NewTask
                            : TaskKind.NotTask;

                    case InterpolatedStringExpressionSyntax interp:
                        foreach (var content in interp.Contents)
                        {
                            if (content is InterpolationSyntax ip)
                                AnalyzeExpression(ip.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        }
                        return TaskKind.NotTask;

                    default:
                        return TaskKind.NotTask;
                }
            }

            private TaskKind AnalyzeAssignment(AssignmentExpressionSyntax a, FlowEnv env, bool bare, HashSet<Group>? assignedInLoop)
            {
                // ' _ = task; ' does not run any release logic — contract violation.
                if (a.Left is IdentifierNameSyntax discard && discard.Identifier.ValueText == "_")
                {
                    var kind = AnalyzeExpression(a.Right, env, transfer: false, assignedInLoop: assignedInLoop);
                    if (kind == TaskKind.Handle || kind == TaskKind.NewTask || kind == TaskKind.TrivialTask)
                        Report(ContractDiagnostics.DiscardedTask, a.GetLocation(), "unnamed");
                    return kind == TaskKind.Handle ? TaskKind.NotTask : kind;
                }

                var leftSymbol = ResolveSymbol(a.Left);
                if (leftSymbol != null && _taskGroups.TryGetValue(leftSymbol, out _))
                {
                    var rhsKind = AnalyzeExpression(a.Right, env, transfer: false, assignedInLoop: assignedInLoop);
                    if (TryGetTaskGroup(a.Right, out var srcGroup))
                    {
                        // t2 = t1 → t2 becomes an alias of t1's consumption group.
                        Remap(leftSymbol, srcGroup);
                    }
                    else if (rhsKind == TaskKind.TrivialTask)
                    {
                        MakeTrivial(leftSymbol);
                    }
                    else if (rhsKind == TaskKind.NewTask)
                    {
                        // t = GetTask() → fresh task, fresh consumption accounting.
                        ResetOrReplace(leftSymbol, env, assignedInLoop);
                    }
                    return TaskKind.NotTask;
                }

                if (leftSymbol != null && _tcsGroups.TryGetValue(leftSymbol, out _))
                {
                    AnalyzeExpression(a.Right, env, transfer: false, assignedInLoop: assignedInLoop);
                    return TaskKind.NotTask;
                }

                AnalyzeExpression(a.Right, env, transfer: false, assignedInLoop: assignedInLoop);
                return TaskKind.NotTask;
            }

            private TaskKind AnalyzeInvocation(
                InvocationExpressionSyntax inv,
                FlowEnv env,
                bool bare,
                HashSet<Group>? assignedInLoop)
            {
                var methodSymbol = _model.GetSymbolInfo(inv).Symbol as IMethodSymbol;
                var arguments = inv.ArgumentList?.Arguments ?? Enumerable.Empty<ArgumentSyntax>();

                // Chain: receiver.Forget() / task.GetAwaiter().GetResult() / tcs.TrySetResult(...)
                var receiver = UnwrapReceiver(inv);
                if (receiver != null && TryGetTaskGroup(receiver, out var rg))
                {
                    string name = GetInvokedMemberName(inv);

                    if (name == "GetResult")
                    {
                        // t.GetAwaiter().GetResult() → consuming. GetAwaiter() alone is not.
                        Consume(rg, inv.GetLocation(), env);
                        AnalyzeInvocationArguments(arguments, methodSymbol, env, assignedInLoop);
                        return TaskKind.NotTask;
                    }

                    if (s_consumingTaskApis.Contains(name))
                    {
                        Consume(rg, inv.GetLocation(), env);
                        AnalyzeInvocationArguments(arguments, methodSymbol, env, assignedInLoop);
                        bool produces = IsTaskType(methodSymbol?.ReturnType);
                        if (produces && bare)
                        {
                            Report(ContractDiagnostics.UnconsumedTask, inv.GetLocation(), name,
                                "（链式调用产生的新任务在表达式中被丢弃）");
                        }
                        return produces ? TaskKind.NewTask : TaskKind.NotTask;
                    }

                    // Non-consuming member on a tracked task (Status / Id / GetAwaiter / Equals ...).
                    AnalyzeInvocationArguments(arguments, methodSymbol, env, assignedInLoop);
                    return TaskKind.NotTask;
                }

                if (receiver != null && TryGetTcsGroup(receiver, out var tcg))
                {
                    string name = GetInvokedMemberName(inv);
                    if (s_tcsCompleteApis.Contains(name))
                    {
                        CompleteTcs(tcg, inv.GetLocation(), env);
                    }
                    AnalyzeInvocationArguments(arguments, methodSymbol, env, assignedInLoop);
                    return TaskKind.NotTask;
                }

                // Static / free-standing invocation.
                AnalyzeInvocationArguments(arguments, methodSymbol, env, assignedInLoop);

                // LuminTaskSource.SetResult(tcs.Handle, ...) 是扩展程序集驱动完成源的生产者入口：
                // handle 由某个 LuminTaskCompletionSource 租出，settle 它等同于完成了该完成源。
                if (_types.SourceType != null &&
                    methodSymbol != null &&
                    SymEquals.Equals(methodSymbol.ContainingType?.OriginalDefinition, _types.SourceType) &&
                    s_handleCompleteApis.Contains(methodSymbol.Name) &&
                    arguments.FirstOrDefault() is ArgumentSyntax handleArgument &&
                    TryGetTcsHandleOwner(handleArgument.Expression, out var handleOwner))
                {
                    CompleteTcs(handleOwner, inv.GetLocation(), env);
                }

                if (IsTaskType(methodSymbol?.ReturnType))
                {
                    bool trivial = methodSymbol != null &&
                                   (methodSymbol.Name == "FromResult" || methodSymbol.Name == "CompletedTask");
                    if (!trivial && bare)
                    {
                        Report(ContractDiagnostics.UnconsumedTask, inv.GetLocation(),
                            GetInvokedMemberName(inv), "（产生的任务在表达式中被丢弃）");
                    }
                    return trivial ? TaskKind.TrivialTask : TaskKind.NewTask;
                }

                return TaskKind.NotTask;
            }

            private void AnalyzeInvocationArguments(
                IEnumerable<ArgumentSyntax> arguments,
                IMethodSymbol? methodSymbol,
                FlowEnv env,
                HashSet<Group>? assignedInLoop)
            {
                int index = 0;
                foreach (var arg in arguments)
                {
                    bool transfersOwnership = false;
                    if (methodSymbol != null && index < methodSymbol.Parameters.Length)
                    {
                        var paramType = methodSymbol.Parameters[index].Type;
                        transfersOwnership = (IsTaskType(paramType) || IsTcsType(paramType)) &&
                                             GetParameterContract(methodSymbol, methodSymbol.Parameters[index].Name) != ParameterContract.Drops;
                    }
                    // LuminTaskSource.Rent/Create 以 out var 租出的句柄：登记后只要求被引用过。
                    if ((IsSourceApi(methodSymbol, "Rent") || IsSourceApi(methodSymbol, "Create")) &&
                        TryGetRentedHandle(arg.Expression, out var rentedHandle))
                    {
                        RegisterHandle(rentedHandle, arg.Expression.GetLocation(), rentedHandle.Name);
                    }

                    AnalyzeExpression(arg.Expression, env, transfer: transfersOwnership, assignedInLoop: assignedInLoop);
                    index++;
                }
            }

            private void AnalyzePattern(PatternSyntax pattern, FlowEnv env, HashSet<Group>? assignedInLoop)
            {
                switch (pattern)
                {
                    case ConstantPatternSyntax constant:
                        AnalyzeExpression(constant.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        break;
                    case RelationalPatternSyntax relational:
                        AnalyzeExpression(relational.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                        break;
                    case ParenthesizedPatternSyntax parenthesized:
                        AnalyzePattern(parenthesized.Pattern, env, assignedInLoop);
                        break;
                    case BinaryPatternSyntax binary:
                        AnalyzePattern(binary.Left, env, assignedInLoop);
                        AnalyzePattern(binary.Right, env, assignedInLoop);
                        break;
                    case UnaryPatternSyntax unary:
                        AnalyzePattern(unary.Pattern, env, assignedInLoop);
                        break;
                    case RecursivePatternSyntax recursive when recursive.PositionalPatternClause != null:
                        foreach (var subpattern in recursive.PositionalPatternClause.Subpatterns)
                            AnalyzePattern(subpattern.Pattern, env, assignedInLoop);
                        break;
                }
            }

            private enum ParameterContract { Unknown, Consumes, Forwards, Drops }

            private ParameterContract GetParameterContract(IMethodSymbol method, string parameterName)
            {
                var syntax = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                if (syntax == null)
                    return ParameterContract.Unknown;

                var identifierUses = syntax.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .Where(id => id.Identifier.ValueText == parameterName).ToArray();
                foreach (var id in identifierUses)
                {
                    for (SyntaxNode? current = id.Parent; current != null && current != syntax; current = current.Parent)
                    {
                        if (current is AwaitExpressionSyntax)
                            return ParameterContract.Consumes;
                        if (current is InvocationExpressionSyntax invocation)
                        {
                            var name = GetInvokedMemberName(invocation);
                            if (s_consumingTaskApis.Contains(name) || name == "GetResult" || s_tcsCompleteApis.Contains(name))
                                return ParameterContract.Consumes;
                        }
                    }
                }

                if (syntax.DescendantNodes().OfType<ReturnStatementSyntax>().Any(r =>
                    r.Expression is IdentifierNameSyntax returned && returned.Identifier.ValueText == parameterName))
                    return ParameterContract.Forwards;

                if (syntax is MethodDeclarationSyntax methodDeclaration &&
                    methodDeclaration.ExpressionBody?.Expression is IdentifierNameSyntax expressionBody &&
                    expressionBody.Identifier.ValueText == parameterName)
                    return ParameterContract.Forwards;

                return ParameterContract.Drops;
            }

            private TaskKind AnalyzeMemberAccess(
                MemberAccessExpressionSyntax ma,
                FlowEnv env,
                bool bare,
                HashSet<Group>? assignedInLoop)
            {
                var symbol = _model.GetSymbolInfo(ma).Symbol;

                // LuminTask<T>.Result contract.
                if (symbol is IPropertySymbol prop &&
                    prop.Name == "Result" &&
                    prop.ContainingType?.OriginalDefinition != null &&
                    _types.TaskOfTType != null &&
                    SymEquals.Equals(prop.ContainingType.OriginalDefinition, _types.TaskOfTType))
                {
                    bool fromResult = ma.Expression is InvocationExpressionSyntax finv &&
                                      _model.GetSymbolInfo(finv).Symbol is IMethodSymbol fm &&
                                      fm.Name == "FromResult";
                    if (!fromResult)
                        Report(ContractDiagnostics.ResultAccess, ma.GetLocation(), prop.Name);
                    return TaskKind.NotTask;
                }

                // tcs.Task → a new LuminTask handle that must itself be consumed.
                if (ma.Name.Identifier.ValueText == "Task" && TryGetTcsGroup(ma.Expression, out _))
                {
                    if (bare)
                    {
                        Report(ContractDiagnostics.UnconsumedTask, ma.GetLocation(), "Task",
                            "（任务在表达式中被丢弃）");
                    }
                    return TaskKind.NewTask;
                }

                AnalyzeExpression(ma.Expression, env, transfer: false, assignedInLoop: assignedInLoop);
                return TaskKind.NotTask;
            }

            // ----------------------------------------------------------------------------
            // Declarations
            // ----------------------------------------------------------------------------

            private void HandleDeclaration(VariableDeclarationSyntax decl, FlowEnv env, HashSet<Group>? assignedInLoop)
            {
                foreach (var v in decl.Variables)
                {
                    var symbol = _model.GetDeclaredSymbol(v) as ILocalSymbol;
                    if (symbol == null) continue;
                    // GetTypeInfo(v).Type is null for 'var' declarators in Roslyn 4.3.0;
                    // the symbol always carries the (possibly inferred) type.
                    var type = symbol.Type;

                    if (IsTaskType(type))
                    {
                        var group = RegisterTask(symbol, v.Identifier.GetLocation(), symbol.Name);
                        if (assignedInLoop != null)
                        {
                            group.LoopScoped = true;
                            assignedInLoop.Add(group);
                        }

                        var init = v.Initializer;
                        if (init == null) continue;

                        var kind = AnalyzeExpression(init.Value, env, transfer: false, assignedInLoop: assignedInLoop);
                        if (TryGetTaskGroup(init.Value, out var srcGroup))
                        {
                            Remap(symbol, srcGroup);
                        }
                        else if (kind == TaskKind.TrivialTask)
                        {
                            MakeTrivial(symbol);
                        }
                        else
                        {
                            // A real task now lives in this slot on this path.
                            env.Initialized.Add(group);
                        }
                    }
                    else if (IsTcsType(type))
                    {
                        var tcsGroup = RegisterTcs(symbol, v.Identifier.GetLocation(), symbol.Name);
                        if (assignedInLoop != null)
                        {
                            tcsGroup.LoopScoped = true;
                            assignedInLoop.Add(tcsGroup);
                        }
                        if (v.Initializer != null)
                            AnalyzeExpression(v.Initializer.Value, env, transfer: false, assignedInLoop: assignedInLoop);
                    }
                    else if (v.Initializer != null)
                    {
                        AnalyzeExpression(v.Initializer.Value, env, transfer: false, assignedInLoop: assignedInLoop);
                    }
                }
            }

            private void HandleForeachVariable(ForEachStatementSyntax fe, HashSet<Group>? assignedInLoop)
            {
                var symbol = _model.GetDeclaredSymbol(fe);
                if (symbol == null) return;

                if (IsTaskType(symbol.Type))
                {
                    var group = RegisterTask(symbol, fe.Identifier.GetLocation(), symbol.Name);
                    if (assignedInLoop != null)
                    {
                        group.LoopScoped = true;
                        assignedInLoop.Add(group);
                    }
                }
                else if (IsTcsType(symbol.Type))
                {
                    RegisterTcs(symbol, fe.Identifier.GetLocation(), symbol.Name);
                }
            }

            private void RegisterCatchVariable(CatchDeclarationSyntax decl)
            {
                var symbol = _model.GetDeclaredSymbol(decl);
                if (symbol == null) return;
                if (IsTcsType(symbol.Type))
                    RegisterTcs(symbol, decl.Identifier.GetLocation(), symbol.Name);
            }

            // ----------------------------------------------------------------------------
            // Group bookkeeping
            // ----------------------------------------------------------------------------

            private Group RegisterTask(ISymbol symbol, Location location, string name)
            {
                var g = new Group(name, location);
                _taskGroups[symbol] = g;
                _allGroups.Add(g);
                return g;
            }

            private Group RegisterTcs(ISymbol symbol, Location location, string name)
            {
                var g = new Group(name, location) { IsTcs = true };
                _tcsGroups[symbol] = g;
                _allGroups.Add(g);
                return g;
            }

            /// <summary>t2 = t1: the new holder joins the source task's consumption group.</summary>
            private void Remap(ISymbol symbol, Group sourceGroup)
            {
                if (_taskGroups.TryGetValue(symbol, out var old) && !ReferenceEquals(old, sourceGroup))
                {
                    old.Members--;
                    if (old.Members <= 0) _allGroups.Remove(old);
                }
                sourceGroup.Members++;
                _taskGroups[symbol] = sourceGroup;
            }

            private void MakeTrivial(ISymbol symbol)
            {
                if (_taskGroups.TryGetValue(symbol, out var g) && g.Members == 1)
                {
                    g.Trivial = true;
                    _allGroups.Remove(g);
                }
            }

            private void ResetOrReplace(ISymbol symbol, FlowEnv env, HashSet<Group>? assignedInLoop)
            {
                if (_taskGroups.TryGetValue(symbol, out var g) && g.Members == 1)
                {
                    // If the slot currently holds a live task that was never consumed on this
                    // path, overwriting it leaks that task's source.
                    if (!g.Trivial && env.Initialized.Contains(g) && env.Get(g) == 0)
                    {
                        Report(ContractDiagnostics.UnconsumedTask, g.Declaration, g.Name,
                            env.Partial.Contains(g) ? "（部分路径上未消费即被重新赋值丢弃）" : "（未消费即被重新赋值丢弃）");
                    }

                    // Same slot re-purposed for a fresh task: reset its accounting.
                    g.Trivial = false;
                    g.CapturedConsumed = false;
                    g.LoopScoped = false;
                    env.Set(g, 0);
                    env.Partial.Remove(g);
                    env.Initialized.Add(g);
                    assignedInLoop?.Add(g);
                    return;
                }

                if (g != null && g.Members > 1)
                {
                    g.Members--;
                }

                var fresh = new Group("reassigned", symbol.Locations.FirstOrDefault() ?? Location.None);
                _taskGroups[symbol] = fresh;
                _allGroups.Add(fresh);
                env.Initialized.Add(fresh);
                assignedInLoop?.Add(fresh);
            }

            private void Consume(Group g, Location site, FlowEnv env)
            {
                if (g.Trivial) return;

                if (g.CapturedConsumed || env.Get(g) > 0 || env.Partial.Contains(g))
                {
                    Report(ContractDiagnostics.DoubleConsumedTask, site, g.Name,
                        env.Partial.Contains(g)
                            ? "（此前在部分路径上已被消费/转移）"
                            : "（首次消费/转移位于此前的位置）");
                    return;
                }

                env.Set(g, 1);
                g.Site = site;
            }

            private void TransferTcs(Group g, Location site, FlowEnv env)
            {
                if (env.Get(g) > 0)
                {
                    Report(ContractDiagnostics.DoubleCompletedSource, site, g.Name,
                        "（完成后又被转移/使用）");
                    return;
                }
                env.Set(g, 1);
            }

            private void CompleteTcs(Group g, Location site, FlowEnv env)
            {
                if (env.Get(g) > 0)
                {
                    Report(ContractDiagnostics.DoubleCompletedSource, site, g.Name);
                    return;
                }
                env.Set(g, 1);
                g.Site = site;
            }

            // ----------------------------------------------------------------------------
            // Finalization
            // ----------------------------------------------------------------------------

            internal void Finalize(FlowEnv env)
            {
                var seen = new HashSet<Group>();
                foreach (var g in _allGroups)
                {
                    if (!seen.Add(g)) continue;
                    if (g.Trivial) continue;
                    if (g.CapturedConsumed && env.Get(g) == 0) continue;
                    if (g.LoopScoped) continue;  // handled by WalkLoop's per-iteration check
                    if (g.LoopManaged) continue; // reset & consumed inside a loop — lifecycle closed there

                    if (env.Get(g) != 0) continue;

                    // A slot that never held a task on this path has nothing to leak.
                    if (!g.IsTcs && !env.Initialized.Contains(g)) continue;

                    string note = env.Partial.Contains(g)
                        ? (g.IsTcs ? "（部分路径未完成）" : "（部分路径未消费）")
                        : string.Empty;
                    if (g.IsTcs)
                        Report(ContractDiagnostics.UncompletedSource, g.Declaration, g.Name, note);
                    else
                        Report(ContractDiagnostics.UnconsumedTask, g.Declaration, g.Name, note);
                }
            }

            // ----------------------------------------------------------------------------
            // Helpers
            // ----------------------------------------------------------------------------

            private bool IsTaskType(ITypeSymbol? type)
                => ContractEngine.IsTaskType(_types, type);

            private bool IsTcsType(ITypeSymbol? type)
                => ContractEngine.IsTcsType(_types, type);

            private ISymbol? ResolveSymbol(ExpressionSyntax expr)
            {
                if (expr is IdentifierNameSyntax id)
                    return _model.GetSymbolInfo(id).Symbol;

                if (expr is MemberAccessExpressionSyntax ma)
                    return _model.GetSymbolInfo(ma).Symbol;

                return null;
            }

            private bool TryGetTaskGroup(ExpressionSyntax expr, out Group group)
            {
                var symbol = ResolveSymbol(expr);
                if (symbol != null && _taskGroups.TryGetValue(symbol, out var g))
                {
                    group = g;
                    return true;
                }
                group = null!;
                return false;
            }

            private bool TryGetTcsGroup(ExpressionSyntax expr, out Group group)
            {
                var symbol = ResolveSymbol(expr);
                if (symbol != null && _tcsGroups.TryGetValue(symbol, out var g))
                {
                    group = g;
                    return true;
                }
                group = null!;
                return false;
            }

            /// <summary>把 `tcs.Handle` 解析回租出该 handle 的完成源分组。</summary>
            private bool TryGetTcsHandleOwner(ExpressionSyntax expr, out Group group)
            {
                group = null!;
                if (expr is not MemberAccessExpressionSyntax handleAccess)
                    return false;
                if (handleAccess.Name.Identifier.ValueText != "Handle")
                    return false;
                return TryGetTcsGroup(handleAccess.Expression, out group);
            }

            /// <summary>判断是否调用了 LuminTaskSource 上的指定成员（Rent / Create / Set*）。</summary>
            private bool IsSourceApi(IMethodSymbol? method, string name)
                => method != null &&
                   _types.SourceType != null &&
                   SymEquals.Equals(method.ContainingType?.OriginalDefinition, _types.SourceType) &&
                   method.Name == name;

            /// <summary>识别 `Rent(out var h)` 形式租出的句柄变量。显式类型与 var 形式都归到 SingleVariableDesignation。</summary>
            private bool TryGetRentedHandle(ExpressionSyntax expr, out ISymbol symbol)
            {
                symbol = null!;
                if (expr is not DeclarationExpressionSyntax declaration)
                    return false;
                if (declaration.Designation is not SingleVariableDesignationSyntax single)
                    return false;
                if (_model.GetDeclaredSymbol(single) is not ILocalSymbol local)
                    return false;
                symbol = local;
                return true;
            }

            private void RegisterHandle(ISymbol symbol, Location location, string name)
            {
                if (_handleGroups.ContainsKey(symbol))
                    return;

                var g = new Group(name, location) { IsHandle = true };
                _handleGroups[symbol] = g;
                _handleGroupList.Add(g);
            }

            /// <summary>
            /// 句柄只要在方法体内（含 lambda / 局部函数 / 状态对象赋值）被引用过任意一次，
            /// 就认为它已交给某条完成路径；因此这里做一次整树扫描，而不是走路径敏感的消费计数。
            /// </summary>
            internal void MarkHandleUsages(SyntaxNode body)
            {
                if (_handleGroupList.Count == 0)
                    return;

                foreach (var id in body.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    var symbol = ResolveSymbol(id);
                    if (symbol == null)
                        continue;
                    if (_handleGroups.TryGetValue(symbol, out var g))
                        g.Used = true;
                }
            }

            /// <summary>方法体收尾：标记句柄使用情况，产出任务/完成源诊断，最后产出句柄漏报。</summary>
            internal void FinishBody(SyntaxNode body, FlowEnv env)
            {
                MarkHandleUsages(body);
                Finalize(env);

                foreach (var g in _handleGroupList)
                {
                    if (!g.Used)
                        Report(ContractDiagnostics.UnusedRentedSource, g.Declaration, g.Name);
                }
            }

            /// <summary>
            /// Unwraps 'task.Forget' / 'a.GetAwaiter().GetResult' receiver chains down to the
            /// tracked expression the invocation operates on.
            /// </summary>
            private ExpressionSyntax? UnwrapReceiver(InvocationExpressionSyntax inv)
            {
                ExpressionSyntax current = inv;
                while (true)
                {
                    if (current is InvocationExpressionSyntax i && i.Expression is MemberAccessExpressionSyntax m)
                    {
                        current = m.Expression;
                        continue;
                    }
                    if (current is InvocationExpressionSyntax i2 && i2.Expression is InvocationExpressionSyntax nested)
                    {
                        current = nested;
                        continue;
                    }
                    if (current is InvocationExpressionSyntax)
                        return null; // delegate invocation etc.
                    return current;  // identifier / member access / this / element access ...
                }
            }

            private static string GetInvokedMemberName(InvocationExpressionSyntax inv)
            {
                if (inv.Expression is MemberAccessExpressionSyntax m)
                    return m.Name.Identifier.ValueText;

                if (inv.Expression is IdentifierNameSyntax id)
                    return id.Identifier.ValueText;

                return string.Empty;
            }

            private void Report(DiagnosticDescriptor descriptor, Location location, string name, string? note = null)
            {
                string formattedNote = string.IsNullOrEmpty(note) ? string.Empty : note;
                _report(Diagnostic.Create(descriptor, location, new object[] { name, formattedNote }));
            }
        }
    }

    /// <summary>
    /// Per-path flow environment: consumption counts, partial-path flags, and initialized
    /// (holds-a-live-task) tracking per group. Branches clone it; merges fold clones back.
    /// </summary>
    internal sealed class FlowEnv
    {
        internal readonly Dictionary<object, int> Counts = new();
        internal readonly HashSet<object> Partial = new();
        internal readonly HashSet<object> Initialized = new();

        internal FlowEnv Clone()
        {
            var clone = new FlowEnv();
            foreach (var kv in Counts) clone.Counts[kv.Key] = kv.Value;
            foreach (var g in Partial) clone.Partial.Add(g);
            foreach (var g in Initialized) clone.Initialized.Add(g);
            return clone;
        }

        internal int Get(object group)
            => Counts.TryGetValue(group, out var c) ? c : 0;

        internal void Set(object group, int count)
        {
            if (count == 0) Counts.Remove(group);
            else Counts[group] = count;
        }
    }
}
}
