# **C#基于ref struct的高性能0gc异步(async/await)库**


### **LuminTask有以下优点：**

*   基于ref struct的LuminTask和自定义AsyncMethodBuilder实现零分配
*   Runtime无GC
*   异步ReactiveProperty
*   与Task/ValueTask/IValueTaskSource/UniTask高度兼容

### **Quick Start**

```cpp
// extension awaiter/methods can be used by this namespace
using Lumin.Threading.Tasks;

// You can return type as ref struct LuminTask<T>(or LuminTask), it is unity specialized lightweight alternative of Task<T>
// zero allocation and fast excution for zero overhead async/await integrate with Unity
async LuminTask<string> DemoAsync()
{
    // delay 100ms
    await LuminTask.Delay(100); 

    await LuminTask.Yield();

    // You can await IEnumerator coroutines
    await FooCoroutineEnumerator();

    // You can await a standard task
    await Task.Run(() => 100);

    await LuminTask.Run(() => 100);

    // Multithreading, run on ThreadPool under this code
    await LuminTask.SwitchToThreadPool();

    /* work on ThreadPool */

    // return to MainThread(same as `ObserveOnMainThread` in UniRx)
    await LuminTask.SwitchToMainThread();

    var task1 = Foo1Async();
    var task2 = Foo2Async();
    
    // you can wait when all task complete
    await LuminTask.WhenAll(task1, task2);

    // you can wait when any task complete
    await LuminTask.WhenAll(task1, task2);

    // you can foreach when each
    await foreach(var v in LuminTask.WhenEach(task1, task2)) {}
}
```

