# LuminTask 2.0 核心架构决议

2026-11-20 · 最终设计 · 基线为 LuminTask 1.0.7，提交 `0e5fba3`。目标运行环境是 Unity IL2CPP、小游戏 WebAssembly 以及现有 .NET 宿主。文中的字节数区分了 1.0 实测、字段布局推算和 2.0 配置值。

## 1. 架构决定

**一个全局、非泛型的 `LuminTaskSource` 管全部异步任务。同步成功直接返回任务值。真正挂起时才使用托管槽位，只有编译器生成的 async 方法才需要状态机 Runner。** 业务继续写普通 C# `async/await`。

| 部件 | 唯一职责 | 持有的资源 |
| --- | --- | --- |
| `LuminTask`、`LuminTask<T>` | 任务值，同时作为 Builder 和 Awaiter | 64 位句柄。泛型值另有一个同步结果字段 |
| `LuminTaskSource` | 出租槽位、登记续体、发布结果、单次消费 | 同一份托管页目录、槽位及空闲索引 |
| `Runner<TStateMachine>` | 保存一次挂起调用的状态机，执行其 `MoveNext` | 强类型状态机和一个可复用的 `Action` |
| `IdleCache` | 跨线程复用已清空的托管对象，控制空闲字节数 | 全局一份按实际类型查找的空闲链 |

PlayerLoop、计时、Unity 事件、组合子和 AsyncEx 都是生产者，通过句柄访问同一个 Source。`LuminTaskCompletionSource<T>` 保留为用户可创建的生产端包装。创建两个包装意味着有两个操作、两个槽位，二者共享完成协议和存储。每个宿主、每种结果类型都使用同一任务身份空间。

沿用下载稿的单次消费、代际句柄、强类型状态机和重入保护。合并其 SlotArena 与 Kernel 的所有权，去掉 owner 本地库存、远端归还队列、独立原生 arena、通用操作记录管理器和强制启动预热。缓存直接还到所有线程可见的位置，组合操作使用普通托管引用表达存活关系。

## 2. 每条路径实际付什么成本

| 一次操作 | 2.0 的库内工作 |
| --- | --- |
| `CompletedTask()`、`FromResult(v)`、一路同步的 async 方法 | 句柄为零，结果在任务值内。零槽位、零 Runner、零池访问、零宿主初始化 |
| `LuminTaskSource.Create<int>` 后由引擎回调完成 | 一个槽位，结果写内联区。零 Runner、零结果盒 |
| `async LuminTask<int> F()` 第一次等到 pending 子任务 | F 自己租一个槽位和一个具体状态机 Runner。子操作保有自己的身份 |
| F 第二次及以后挂起 | 从 F 的槽取回原 Runner，重复使用同一个 Action。每次只登记新的等待关系 |
| `Delay`、`DelayFrame` | 一个槽位和宿主队列中的一个值类型条目。计时数据在槽内，零 Runner、零原生申请 |
| 完成时的内联恢复 | 先发布结果并取走续体，再在锁外调用。默认增加的调度跳数为零 |

这个边界来自现版的实际行为。现版已经有 8 字节非泛型任务、泛型同步结果和非泛型 CoreSource。Bag 首次初始化会建一整页 256 个 40 B 热项，另有目录、空闲链和冷侧元信息。Runner 的空闲上限是每种状态机每线程 16 个，StatePool 是每种 T 共 32 个。普通 Delay 申请 16 B 原生 scratch，DelayFrame 申请 4 B，超过 16 B 的无引用异步结果也会申请原生块。标准调度器的 Timer 已经延迟到首次入队创建，Unity 安装时仍为编译进来的各 timing 创建 strategy。[Bag](src/LuminTask/LuminTask.Bag.cs) · [Runner](src/LuminTask/CompilerServices/StateMachineBox.cs) · [StatePool](src/LuminTask/Utility/StatePool.cs) · [Delay](src/LuminTask/LuminTask.Delay.cs) · [Frame](src/LuminTask/LuminTask.Frame.cs) · [结果桥](src/LuminTask/TaskSource/LuminTaskResultBridge.cs) · [标准宿主](src/LuminTask/PlayerLoopHelper.cs) · [Unity 宿主](Unity/UnityPlayerLoop.cs)

**跨线程分配基线。** 本次用当前核心源码在 Windows x64、.NET 10.0.9、Release 下测了一个 `async LuminTask<int> Echo(LuminTask<int> child) => await child`。A 创建句柄源并启动 Echo，完成后 A 消费父任务，每次只有一个在途。跨线程组通过两个预建事件交给固定 B 线程完成。每组独立进程，预热 1,024 次，再测 10,000 次，重复三轮。结果三轮一致。

| 1.0.7 完成方式 | A 线程累计分配 | B 线程累计分配 | 结束 live slots |
| --- | ---: | ---: | ---: |
| A 租，A 完成 | 24 B | 无 B 线程 | 0 |
| A 租，B 完成 | 1,520,024 B | 0 B | 0 |

差额是 **152 B/操作**，与现版 Box 及绑定委托在 A 创建、归还到 B 的线程静态链吻合。采样使用 `GC.GetAllocatedBytesForCurrentThread`，只覆盖这两个参与线程，未测延迟。它给出了共享库存必须消除的分配斜率。

## 3. 唯一 Source 的托管存储

句柄低 16 位保存 `slotIndex + 1`，高 48 位保存代际。保留 `short Id` 的既有有效范围，逻辑槽位硬上限 32,767。句柄为零的同步值绕过 Source。数值句柄仅在当前运行域内有效。

每页 **64 个槽**。页内采用一个普通托管结构体数组，四个引用都由 GC 跟踪。下列字段表示存储契约，具体偏移由运行时决定。

```csharp
internal struct Inline16
{
    internal ulong First;
    internal ulong Second;
}

internal struct Slot
{
    internal long Control;                  // 代际、阶段、结果种类、登记标志、写入权
    internal Inline16 Data;                 // 小值结果，或等待期间的数字状态
    internal Action<object?>? Continuation;
    internal object? ContinuationState;
    internal object? Payload;               // 引用结果、强类型结果盒或故障信息
    internal object? ProducerState;         // Runner 或需要关闭的生产端绑定
}
```

在 .NET 10.0.9 x64 的独立纯托管布局探针中，相邻槽的实际数组步长是 **56 B**，一页元素合计 **3,584 B**。现版首个热数组的元素合计是 **10,240 B**；两数都未含对象头、目录和配套结构。探针以 `AllowUnsafeBlocks=false` 编译，12 B 的 Vector3 和 16 B 的 Quaternion 在写入、强制压缩 GC、按 ref 读回后均保真，含引用结构体及 64 B 的 Matrix4x4 均被排除出内联路径。新槽单项更大，换来一次寻址就能拿到 Runner、常见结果及等待状态，也省去每字段的块目录、位图和分配分支。32 位和 IL2CPP 以实机数组步长核对。

**租还与裁剪。** 页目录从首次租用时的小目录开始增长。页有未使用位置、空闲链、占用数，空闲槽借用自己的 Data 存下一索引。一个短的分配器锁保护有空位的页链、租还和目录变更，已经租出的任务各自独立完成。目录项保留该页号发过的最大代际，每租一次在锁内递增；页满代际后退休。扩目录时发布新目录，保留已有页及代际。宿主重装沿用原目录身份。

消费方清引用并将槽置为不可访问后，才在分配器锁内归还索引、递减占用数。`Trim` 在同一把锁下摘除整张空页，保留页号的代际高水位。解析方只在本次调用中持有读到的托管页引用，并以槽的完整代际及阶段取得写入权。迟到者即便持有旧页，只能看到失效的槽；同位置的新页是另一个数组，新租约代际更高。这样可以在其他页仍有长任务时回收空页，GC 负责已脱离目录的旧数组的物理寿命。

## 4. 完成、重入与回收

槽的逻辑阶段为 **Free → Pending → Succeeded / Faulted / Canceled → Consuming → Free**。Control 的一个 busy 位串行化同一槽上极短的字段更新。进入时 CAS 匹配完整控制字及代际，退出时 release 发布完整新控制字。跨线程状态查询用一次有 acquire 语义的 64 位原子读；32 位后端也要保证整字原子性。同槽竞争仅重试该槽，登记、完成与消费不获取全局分配器锁。全局租还发生在这些临界区之外。

| 操作 | 持有槽写入权时 | 退出以后 |
| --- | --- | --- |
| Register | 校验代际和唯一登记权。Pending 时写续体及状态；已终态时直接取得调用权 | 需要即时恢复时在当前线程调用局部续体 |
| TrySet | 仅 Pending 可以写入。填入已准备的结果，置终态，将已登记的续体、状态、待清理的生产端关联移入局部并清除槽中对应引用 | 完成端仅使用局部数据执行清理和回调；之后的所有路径均可面对已复用的槽 |
| GetResult / Drain | 仅终态可取得唯一消费权，将结果及回收所需的信息移入局部，转 Consuming | 读出 T，释放库拥有的载体，清根并归还索引。故障保留异常栈，取消保留原 token |

持槽权期间只做内部字段操作和对应 T 的值复制。池操作、捕获上下文、注册或注销取消、用户谓词、异常派发和续体均在外部执行。输入与载体检查先于改写；失败出口释放 busy 并保留原状态。对象准备失败时回滚未发布资源。已接受任务发布正常结果所需的存储在租用时备齐。重复完成返回 false，过期生产者不能修改新任务，重复登记和重复消费可检测时报告契约错误。`Forget` 对 pending 任务登记最终消费，任务保有生产资源直至终态。同步值无需回收。

**Runner 有独立于槽的退出点。** 第一次挂起时先租槽与 Runner，把句柄写回原 Builder，再复制状态机，最后才把缓存 Action 交给 awaiter。第一次注册持有一个执行计数；之后每次进入 Runner 的 `MoveNext` 加一，并在 `finally` 减一。完成时从槽摘下 Runner，标记其终态。终态和执行计数使用同一原子字，由使“已终态且计数为零”成立的一方唯一清空状态机并回池。回池无需再查原句柄。这样即使第三方 awaiter 在登记返回前跨线程恢复，或续体立刻消费并重租同一槽，原调用帧仍然安全。缓存 Action 的复用契约要求 awaiter 对每次登记只通知一次。

默认续体在通知线程内联执行。`ConfigureAwait(bool)` 延续现有的 ExecutionContext 捕获语义，在同一个槽协议内修改尚未登记的等待策略；显式线程切换仍由相应 awaiter 或宿主负责。仅在需要携带已捕获上下文时使用托管回调状态，随通知退出清空；默认路径直接使用缓存委托。用户回调抛出时保持已发布的终态，在现有异常边界上报。

## 5. 纯托管结果与 Unsafe 的使用范围

| T / 用途 | 存储规则 |
| --- | --- |
| 同步成功，任意 T | `LuminTask<T>` 的 T 字段，包含大值与含引用结构体 |
| 异步引用类型 | Payload 的普通托管引用，保留 null 成功结果 |
| 异步无引用值类型，大小 ≤ 16 B | Data 的两个整数，按实际大小复制。覆盖 int、long、Vector3、Quaternion |
| 异步含引用值类型，或无引用值 > 16 B | 租用现有 `StateTuple<T>`，以普通 `T Item` 字段读写。消费后清成 default，再还到同一 IdleCache |
| 异常与取消 | Payload 持有 EDI 或保存原 token 的托管载体。失败前准备，发布后按单次消费清理 |

结果分流只存在于窄的 `Prepare<T> / Write<T> / Read<T>` 边界。仅在 `!RuntimeHelpers.IsReferenceOrContainsReferences<T>()` 且大小在 1–16 B 内时，通过 `Unsafe.As<…, byte>(ref …)` 和 `Unsafe.CopyBlockUnaligned(ref byte, ref byte, uint)` 精确复制到 Inline16 本体。读回时从已清零的 T 局部开始。所有含引用字段的写入使用正常赋值，保留 GC 写屏障。释放根据内部载体标记进行，用户返回的 `IDisposable` 仍归用户所有。[引用分类](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.runtimehelpers.isreferenceorcontainsreferences?view=netstandard-2.1) · [Unsafe 的 byref 实现](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/Unsafe.cs)

槽访问优先写作 `ref Slot slot = ref page.Slots[index]`。`ref` 限定在同步访问及已取得的所有权内；跨 await、入队及事件回调用句柄或托管对象。2.0 的库代码以 `AllowUnsafeBlocks=false` 编译，包括 Core、AsyncEx 和随库发布的 Unity 适配源码。移除 `MemoryHelper`、`UnsafeMemory` 和公共原生分配器钩子；排除裸指针、函数指针、固定托管数组、以整数保存地址、`Unsafe.AsPointer` 以及间接申请原生内存的路径。Unity 提供的 JobHandle 等外部值继续按宿主声明的类型传递。

## 6. 启动、共享缓存和驻留

**启动按需求发生。** 同步任务类型自身没有创建委托、表、Timer 或宿主的静态初始化器。DelayFrame、Run 等 partial 成员的缓存委托属于对应功能的惰性 holder。第一次手动 Source 只建立任务页。第一次真实挂起才接触 Runner 缓存。Unity 启动仅安装既有 timing 所需的回调并登记主线程，队列和运行状态在该 timing 有工作时才建。.NET 默认 DotNet timing 保持可用，第一次入队才启动驱动，空闲停表。

**空闲对象只有一个库级所有者。** IdleCache 用普通托管类型标识（例如 `typeof(Runner<TStateMachine>)`）索引空闲链，类型必须精确匹配。租还使用短锁，临界区只查桶、摘挂节点和记账；创建对象、清理 T 和执行用户代码均在锁外。泛型叶节点直接构造自己的对象，库存里没有反射工厂或按线程复制的静态链。A 租 B 还后，A 的下一次租用能直接看到该对象。Runner 返回前清掉状态机，绑定的 Action 同 Runner 一起复用。原有 `StatePool<T>` 变为无静态库存的兼容转发层。Unity 扩展跨程序集租还专用对象时走 Core 的非泛型缓存桥，只传类型键、对象和计费大小。续体辅助对象及随库模块的空闲池都遵循同一额度。

| 资源 | 保留策略 |
| --- | --- |
| 空闲对象 | `MaxIdleObjectBytes` 默认 **64 KiB**，全局合计。入池前按对象头、对齐、强类型字段、缓存委托等目标平台保守费用扣额度；超额的归还清根后交 GC。冷桶清空后移除键，表容量按最大可缓存节点数封顶，Trim 时收缩 |
| 任务页 | 首次 64 槽，按需增长，硬上限 32,767 个有效槽。`Trim` 保留一个已使用过的基础页，其余全空页可释放。部分占用页的空洞始终计入 backing |
| 宿主缓冲 | 每个已激活 timing 使用可复用的托管环形入队缓冲和紧凑活动数组。扩容发生在接纳时，出队清引用。每帧排空继续复用；显式 Trim 在宿主安全点按剩余条目和已接纳预留量缩至能容纳它们的最小二次幂，基础容量 64。未激活 timing 的缓冲容量为零 |
| 活跃数据 | 已租槽、仍在执行的 Runner、已登记且待完成的观察者、用户尚未消费的结果各自计数。Trim 只释放没有活跃所有者的存储 |

`MaxIdleObjectBytes` 是对象缓存额度。总驻留还包含页、目录、桶表、宿主缓冲和活动对象；诊断分别给出数量、容量、估算字节与峰值，按物理所有者归类以免重复计费。64 KiB 是为小游戏选定的可调默认额度，费用按后端校准，RSS 和 GC 堆提交量另测。宿主在场景卸载或低内存通知时调用 `LuminTaskSource.Trim()`，主动清空空闲对象并裁剪页与缓冲。常态无需后台裁剪 Timer。业务可以在加载阶段跑代表性工作集预热；裁剪后重新预热的成本计入冷路径。

**0 GC 契约。** Release 构建、工作集和并发固定、具体状态机及载体已预热、缓存预算足够且无主动 Trim 的连续成功路径，库内托管分配为 **0 B/操作**；库自有原生申请为 **0 次**。缓存满仅影响空闲保留。达到逻辑槽硬限时在接纳方明确失败，回滚未发布资源；async Builder 连故障槽都无法取得时容量错误可以同步传出。已经可见的任务必须能通知并消费。用户结果数组、类版 CompletionSource、异常及立即取消的 BCL 注册成本须分别计入调用总量。返回新数组的兼容 API 按其输出所有权报告分配。

`WhenEach` 的可枚举对象、枚举器及待领取结果队列也单列为会话分配，具体应用需按每次枚举的子项数摊销。

## 7. 外围 API 与兼容边界

| 入口 | 2.0 实现契约 |
| --- | --- |
| `Delay / DelayFrame / Yield` | 沿用签名、默认 timing、零值、无限等待和取消语义。Delay 的起始时间和时长共 16 B，帧计数占 4 B，都在 Data 内。每次 tick 按句柄借用槽，检查代际及 Pending 后读取或递减；移出局部的到期决定以完整句柄尝试完成。迟到的 tick 无法写新租约 |
| `cancelImmediately`、外部事件 | 正常轮询只携带 token。立即取消走独立托管绑定，先建绑定再 Register。登记返回前若同步取消，由安装者在返回后完成注销；完成、回调退出、安装退出共同决定可复用时刻。注销及可能等待的 Dispose 均在槽外执行。第三方无法证明解绑后回调已退出时，回调保留本次不可变句柄，禁止复用仍可能被调用的目标。[CancellationToken.Register](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken.register?view=net-10.0) |
| `WhenAny / WhenAll` | 保留数组、枚举及异构元组的结果形状和既有胜者、首错行为。非泛型 Join 只负责计数和取得一次发布权，强类型叶节点负责读值及组装输出。同步子项直接读内联 T，pending 子项直接登记续体。胜者或首错发布后其余子项各消费一次 |
| Join 的存活 | 登记循环和每个已接管的 pending 子项各持一份所有权。父结果终态后在 Join 的同步边界移走输出、输入集合与用户引用，晚到者只消费自己的子任务。最后一个持有者退出才还 Join。永不完成的子项只保留必要观察状态，计入活动量。普通托管引用足以表达这段关系 |
| `WhenEach / Forget` | WhenEach 直接登记子项并保留完成顺序，先写 Current 再发布本次 MoveNext。提前退出后，已接管的子项继续排空。Forget 保留现有忽略异常或交给指定 handler 的语义。两者复用 Source 的同一登记入口 |
| `LuminTaskSource.Create/Rent/SetResult/ToTask`、AsyncEx、Unity | 保留句柄驱动的生产接口及 `LuminTask<T>.Result` 的现有约束。已知 T 的创建端使用 `Rent<T>/Create<T>` 预留载体。无类型 Rent 仍能发布 void、引用或可内联的小值，涵盖 Unity 的共享 AsyncOperation 适配。以往无类型 Rent 后写入含引用结构体或大值的低层调用，迁为 `Rent<T>`；AsyncEx 内部同理。裸 ulong 的生产者须匹配消费端 T，写入前校验存储种类、尺寸及托管载体类型。同宽无引用值的业务类型匹配由调用者保证 |
| 低层公有入口 | `Id` 和只读容量、live 等诊断保留。可写的 `LuminTaskBag.GetItem/GetCold`、手工 GetId/ResetId、旧 Item 布局和指针分配器退出 2.0 公共契约。依赖这些入口的扩展迁到句柄 Source 或只读快照，禁止绕过代际归还仍在使用的槽 |

宿主队列沿用 `IPlayLoopStrategy` 对外形状，维护 timing、帧边界和已有线程切换语义。每个 timing 的入队与取批在短锁中交接，调用 step、continuation 时释放锁；重入入队的条目归入对应的后续批次。库内的多处公共 partial 工厂继续留在原程序集，宿主和可选集成通过现有边界接入。关闭宿主应先停止接纳，在安全点取消或排完已接管工作，结清绑定，最后释放缓冲。

## 8. AOT 与性能验收

普通 C# 每个实际使用的 async 方法仍有编译器生成的状态机。`Runner<TStateMachine>` 只随状态机闭合；异步结果桥仅为含引用结构体与大值租用 `StateTuple<T>`。Builder 必需的 `Await[Unsafe]OnCompleted<TAwaiter,TStateMachine>` 保持薄入口；完成、登记、池策略、组合计数和调度循环编译为非泛型实现。保留公共异构重载所需的签名和强类型读写。发布物中实际出现哪些闭合类型及代码，取决于 IL2CPP 的泛型共享和裁剪，须从产物核实。

.NET 11 Runtime Async 把部分标准 Task/ValueTask 方法的挂起转换交给运行时；自定义 task-like 返回类型仍走编译器 Builder。这里借鉴同步直返、只在挂起时物化执行状态、缩短中间转发链。库可以直返底层 LuminTask 的调用直接转发。BCL 池化 Builder 的强类型状态机和缓存 Action 可用于普通 C#，其每线程及每核库存与本项目的全局驻留预算不相容。[Microsoft 的 Runtime Async 说明](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-11/) · [.NET 池化 Builder 源码](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/PoolingAsyncValueTaskMethodBuilderT.cs)

| 验收项 | 负载和通过条件 |
| --- | --- |
| 冷启动 | 新进程依次隔离测同步返回、首个 Source、首次挂起、首个 Delay、Unity 首场景。记录分配字节、首调时间、首帧耗时和已提交页/队列数。同步路径新增页、Runner、队列均为 0；首个 Source 只带一页 |
| 稳态分配 | 用手动 pump 和句柄 Source，覆盖 1/8/16/17/32/33/128/1024 并发，分别准备匹配预算。测同线程、A 租 B 还、多次挂起、16 B 边界、大值和含引用结构体。预热后计量窗口内库归因分配为零，跨线程 152 B/操作差额归零。用进程或 Unity profiler 补足线程级计数 |
| 热路径延迟 | 同一硬件、优化级别与宿主比较 1.0.7 和 2.0，拆测 rent、register、publish、resume、consume，并报告端到端吞吐和 p50/p95/p99。重点验证分配器/缓存短锁及同槽竞争。重复测量建立基线噪声带，核心用例超出噪声的退化阻止发布 |
| 驻留 | 固定并发，轮换 1/100/1000 种状态机；重复“突发—闲置—再突发”，留一个长任务再 Trim。分别核对空闲对象额度、部分占用页、空页、队列及目录。空页可在长任务运行期间释放，回池后对游戏对象的弱引用可被回收 |
| 并发与 GC 正确性 | 完成早于登记、登记中内联和跨线程恢复、完成与取消竞争、重复消费、过期 handle、页摘除后同索引重租、Register 内同步取消、回调抛错、WhenAny 晚到者、WhenEach 提前退出。强制 GC 后含引用 T 保真，终态及消费各一次，结算后 live 回到基线 |
| 包体与纯托管约束 | 相同业务方法集合、Unity 版本、stripping、泛型共享与构建选项，对比空包、仅 Core、Core 加组合子。从 IL2CPP 输出核对 `global-metadata.dat`、闭合泛型方法、native/Wasm 代码段、压缩下载包。代表性小游戏扣除相同空包后，元数据增量和代码段增量均须低于 1.0.7，另报压缩下载差值。Core/AsyncEx/Unity 适配禁用 unsafe 编译，并审计原生申请、地址转换和 Unsafe 调用点 |

落地沿一条依赖链进行：先锁定上述 1.0 基线，接通唯一 Source 的租还、完成和消费，再接 Builder、托管结果及共享缓存，最后让既有外围生产者接入并跑目标平台验收。各阶段共用这里规定的最终布局、句柄及所有权协议。[现有挂起基准](benchmarks/LuminTask.SuspensionBenchmarks/Program.cs) · [结果存储测试](tests/LuminTask.Tests/ResultStorageTests.cs) · [组合行为测试](tests/LuminTask.Tests/CombinatorTests.cs)
