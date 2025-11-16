using LuminTaskUniTest;
using LuminThread;
using LuminThread.AsyncEx;
using LuminThread.Utility;

var watch = new LuminStopWatch();
watch.Start();

await LuminTask.Delay(1000);

Console.WriteLine(1111);

var count = new AsyncCountDownEvent(3);

var task1 = LuminTask.Run(async () =>
{
    await LuminTask.Delay(1000);
    count.Signal();
});

var task2 = LuminTask.Run(async () =>
{
    await LuminTask.Delay(2000);
    count.Signal();
});

var task3 = LuminTask.Run(async () =>
{
    await LuminTask.Delay(3000);
    count.Signal();
});

await count.WaitAsync();

watch.Stop();

Console.WriteLine(watch.ElapsedMilliseconds);

await TestAsyncLock();

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
        });
    }

    await Task.WhenAll(tasks);
            
    if (counter != 1000)
        throw new Exception($"AsyncLock 线程安全测试失败，期望 1000，实际 {counter}");
    
            
    Console.WriteLine("  ✅ AsyncLock 测试通过");
}