using Microsoft.CodeAnalysis;

namespace LuminThread.Analyzer
{
    /// <summary>
    /// Compile-time contract diagnostics for the LuminTask single-consumption model.
    /// All rules mirror the runtime contract documented in the LuminTask README:
    /// a task must be consumed (awaited / Forget-ed / ownership-transferred) exactly once,
    /// and a completion source must be completed exactly once.
    /// </summary>
    internal static class ContractDiagnostics
    {
        internal const string Category = "LuminTask.Contract";

        /// <summary>LUM1001: a LuminTask is never consumed (or not consumed on all paths) — native source leak.</summary>
        internal static readonly DiagnosticDescriptor UnconsumedTask = new(
            id: "LUM1001",
            title: "LuminTask 未被消费",
            messageFormat: "LuminTask '{0}' 从未被消费（必须 await、Forget 或转移所有权），未消费的任务源将泄漏原生内存与任务槽位{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:
                "LuminTask 遵循单次消费约定：每个实例必须被 await、Forget() 消费一次，或通过参数/返回值/赋值转移所有权。" +
                "未消费的任务源位于非托管内存，GC 无法回收。");

        /// <summary>LUM1002: a LuminTask is consumed more than once — second consumer touches released memory.</summary>
        internal static readonly DiagnosticDescriptor DoubleConsumedTask = new(
            id: "LUM1002",
            title: "LuminTask 被重复消费",
            messageFormat: "LuminTask '{0}' 已被消费（或已转移所有权），此处构成第二次消费；任务源仅允许被消费一次，重复消费可能读取已释放的原生内存{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:
                "任务源在首次消费（await / Forget / 转移）后即被释放；再次消费同一实例（或其副本）属于未定义行为。");

        /// <summary>LUM1003: Result is read on a task that is not statically known to be synchronously completed.</summary>
        internal static readonly DiagnosticDescriptor ResultAccess = new(
            id: "LUM1003",
            title: "LuminTask<T>.Result 仅在同步完成时有效",
            messageFormat: "不能在此处访问 LuminTask<T>.Result：仅同步完成的任务可读取 Result（如 LuminTask.FromResult(...).Result）。请改用 await 获取结果{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:
                "对可能异步挂起的任务读取 Result 会抛出 InvalidOperationException。仅当任务由 LuminTask.FromResult " +
                "同步创建时才允许直接读取 Result；其余情况应使用 await 或 GetAwaiter().GetResult()。");

        /// <summary>LUM1004: a completion source is never completed — slot leak.</summary>
        internal static readonly DiagnosticDescriptor UncompletedSource = new(
            id: "LUM1004",
            title: "LuminTaskCompletionSource 从未完成",
            messageFormat: "LuminTaskCompletionSource '{0}' 从未调用 TrySetResult / TrySetException / TrySetCanceled 完成，其任务源将被泄漏{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:
                "创建后未被完成的完成源将永久占用任务槽位与原生内存。确保每个完成源恰好被完成一次，" +
                "或将其转移（参数 / 字段）由其他代码完成。");

        /// <summary>LUM1005: a LuminTask is assigned to a discard — the discard does not release the source.</summary>
        internal static readonly DiagnosticDescriptor DiscardedTask = new(
            id: "LUM1005",
            title: "禁止将 LuminTask 赋给丢弃符",
            messageFormat: "不能将 LuminTask 赋给丢弃符 '_'：丢弃不会消费任务、也不会释放任务源。请改用 Forget() 或 await{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:
                "与 Task 不同，丢弃 LuminTask 表达式不会触发任何释放逻辑；任务源将泄漏。" +
                "使用 task.Forget() 忽略结果，或 await 获取结果。");

        /// <summary>LUM1006: a completion source is completed more than once on the same path.</summary>
        internal static readonly DiagnosticDescriptor DoubleCompletedSource = new(
            id: "LUM1006",
            title: "LuminTaskCompletionSource 被多次完成",
            messageFormat: "LuminTaskCompletionSource '{0}' 在同一路径上被多次完成：TrySet* 仅对首次调用生效，多余的调用是潜在逻辑错误{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:
                "完成源只应完成一次。同一路径上的第二次 TrySet* 调用永远返回 false，通常意味着状态管理错误。");

        /// <summary>LUM1007: a LuminTask stored in a field escapes single-consumption lifecycle tracking.</summary>
        internal static readonly DiagnosticDescriptor FieldStoredTask = new(
            id: "LUM1007",
            title: "LuminTask 存储在字段中",
            messageFormat: "字段 '{0}' 持有 LuminTask：字段不受单次消费生命周期跟踪，容易泄漏或重复消费。建议改用局部变量并及时 await/Forget{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:
                "分析器只跟踪方法内的局部变量与参数。存放在字段中的 LuminTask 的所有权与消费时机无法静态确认，是泄漏与重复消费的高发来源。");

        /// <summary>LUM1008: a handle rented from LuminTaskSource is never used — nothing can complete its source.</summary>
        internal static readonly DiagnosticDescriptor UnusedRentedSource = new(
            id: "LUM1008",
            title: "LuminTaskSource 租出的句柄从未被使用",
            messageFormat: "LuminTaskSource 租出的句柄 '{0}' 在方法体内从未被引用：没有任何路径能完成它，该任务源将永久占用槽位与内存{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:
                "LuminTaskSource.Rent / Create 以 out 参数租出的 LuminTaskHandle 必须被交给完成路径" +
                "（LuminTaskSource.SetResult / SetException / SetCanceled），或存入状态对象、闭包由回调稍后完成。" +
                "句柄在方法内完全未被引用时，其槽位永远不会被归还，且 LuminTask.ToTask 也无法再重建该任务。");

        /// <summary>LUM9999: analyzer implementation failure.</summary>
        internal static readonly DiagnosticDescriptor InternalError = new(
            id: "LUM9999",
            title: "LuminTask 分析器内部错误",
            messageFormat: "LuminTask 契约分析器在处理此方法时遇到内部错误：{0}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "分析器遇到未处理的语法或符号形态；请提交可复现代码以便修复分析器。"
        );
    }
}
