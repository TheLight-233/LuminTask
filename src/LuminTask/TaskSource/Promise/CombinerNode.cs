using System;
using System.Runtime.CompilerServices;
using System.Threading;
using LuminThread.TaskSource;

namespace LuminThread.TaskSource.Promise;

// Shared, non-generic bookkeeping for every combinator (WhenAll / WhenAny, fixed arity and array
// forms alike).
//
// The rule the redux follows is that no arity and no result type keeps its own copy of the
// protocol: this node owns the combined task's handle, the child counter and the single-completion
// path, while the typed (and arity-shaped) writers only aggregate results and call Complete/Fault.
// That keeps the arity families thin instead of each carrying its own claim logic.
//
// Lifetime: child continuations keep the node alive after the combined task has been consumed, so
// a late child completion just finds a stale handle and TrySet* quietly returns false.
internal abstract class CombinerNode
{
    // 固定元数组合子共用的子任务完成入口：委托与 T 无关，4 个静态实例由全程序集复用，
    // 索引只是分派用的常量子任务编号，具体逻辑（聚合、竞速、失败处理）经 OnChildCompleted
    // 虚分派落到各自的 TryInvokeContinuationTN。这样每个 (T1..TN) 组合、每个非泛型元数
    // 都不再生成自己的 lambda 缓存类型（<>c）与静态委托字段。
    internal static readonly Action<object> ChildContinuation0 = static state => ((CombinerNode)state).OnChildCompleted(0);
    internal static readonly Action<object> ChildContinuation1 = static state => ((CombinerNode)state).OnChildCompleted(1);
    internal static readonly Action<object> ChildContinuation2 = static state => ((CombinerNode)state).OnChildCompleted(2);
    internal static readonly Action<object> ChildContinuation3 = static state => ((CombinerNode)state).OnChildCompleted(3);

    // The combined task. Never exposed; the writers hand it to LuminTask/LuminTask<T>.
    internal readonly LuminTaskHandle Handle;

    private int _completedCount;

    protected CombinerNode()
    {
        Handle = LuminTask.RentSlot();
    }

    // 仅固定元数组合子覆写；数组版组合子走各自的元组状态，不经过共享委托。
    // 默认实现直接抛出，误接线时立刻暴露，而不是静默丢失一次完成。
    internal virtual void OnChildCompleted(int index) => throw new NotSupportedException();

    // Increments the child counter, returning true when the caller is the one that brought the
    // count up to expected (i.e. it owns the aggregate publication).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IncrementCompleted(int expected) => Interlocked.Increment(ref _completedCount) == expected;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Complete() => LuminTaskCoreSource.TrySetResult(Handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Complete<TResult>(in TResult result) => LuminTaskResultBridge.TrySetResult(Handle, in result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Fault(Exception exception) => LuminTaskCoreSource.TrySetException(Handle, exception);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Cancel(CancellationToken cancellationToken) => LuminTaskCoreSource.TrySetCanceled(Handle, cancellationToken);
}