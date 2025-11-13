// See https://aka.ms/new-console-template for more information

using LuminTaskUniTest;
using LuminThread;
using LuminThread.AsyncEx;


await LuminTask.Delay(1000);
Console.WriteLine("Hello, World!");

await LuminTask.Yield();
Console.WriteLine("Hello World!");


var test = new Test();

LuminTask.Run(async () =>
{
    await LuminTask.Delay(2000);
    test.Value = 200;
});

await LuminTask.WaitWhile(test, t =>
{
    return t.Value == 200;
});

Console.WriteLine("执行完了");

Console.WriteLine(await FooAsync());



static async LuminTask<int> FooAsync()
{
    await LuminTask.Yield();
    var sum = 0;
    for (int i = 0; i < 100; i++)
    {
        sum += i;
        
    }
    return sum;
}

static async Task TestAsyncLock()
{
    Console.WriteLine("测试 AsyncLock...");
    
    var mutex = new AsyncLock();
    var counter = 0;
    var tasks = new Task[10];

    for (int i = 0; i < 10; i++)
    {
        tasks[i] = Task.Run(async () =>
        {
            for (int j = 0; j < 100; j++)
            {
                await using (await mutex.LockAsync())
                {
                    counter++; // 临界区操作
                }
            }
            Console.WriteLine("1");
        });
    }

    await Task.WhenAll(tasks);
            
    if (counter != 1000)
        throw new Exception($"AsyncLock 线程安全测试失败，期望 1000，实际 {counter}");
    
            
    Console.WriteLine("  ✅ AsyncLock 测试通过");
}


