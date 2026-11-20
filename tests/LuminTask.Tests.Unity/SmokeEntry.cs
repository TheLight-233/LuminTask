using System;
using System.Collections;
using System.Threading;
using LuminThread;
using LuminThread.Unity;
using UnityEngine;
using UnityEngine.Events;

public sealed partial class SmokeEntry : MonoBehaviour
{
    private float _deadline;
    private bool _finished;
    private LuminTask _test;

    private void Start()
    {
        _deadline = Time.realtimeSinceStartup + 30;
        _test = Run();
    }

    private void Update()
    {
        if (_finished || (!_test.IsCompleted && Time.realtimeSinceStartup < _deadline)) return;
        _finished = true;
        try
        {
            if (!_test.IsCompleted) throw new TimeoutException("Unity smoke test did not complete.");
            _test.GetResult();
            LuminTaskSource.Trim();
            Check(LuminTaskSource.GetStorageStatistics().ActiveSlots == 0, "Player leaked task slots");
            Debug.Log("LUMIN_UNITY_TEST_PASS");
            Application.Quit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Application.Quit(1);
        }
    }

    private async LuminTask Run()
    {
        int start = Time.frameCount;
        await LuminTask.DelayFrame(2, PlayerLoopTiming.LastUpdate);
        Check(Time.frameCount > start, "DelayFrame completed in the original frame");
        await LuminTask.Delay(2, PlayerLoopTiming.Update);
        var asset = await Resources.LoadAsync<TextAsset>("lumin-smoke").ToLuminTask();
        Check(asset is TextAsset text && text.text == "lumin", "ResourceRequest result");

        await CheckEvents();
        CheckOptionalAdapters();
        await CheckCoroutines();
    }

    partial void CheckOptionalAdapters();

    private static async LuminTask CheckEvents()
    {
        var signal = new UnityEvent<int>();
        var fired = signal.OnInvokeAsync(default);
        signal.Invoke(42);
        Check(await fired == 42, "UnityEvent result");

        using var cancellation = new CancellationTokenSource();
        var stopped = signal.OnInvokeAsync(cancellation.Token);
        cancellation.Cancel();
        Check((await stopped.SuppressCancellationThrow()).IsCanceled, "UnityEvent cancellation");
        signal.Invoke(7);
        using var handler = signal.GetAsyncEventHandler(default);
        var next = handler.OnValueChangedAsync();
        signal.Invoke(87);
        Check(await next == 87, "Reusable handler result");
    }

    private async LuminTask CheckCoroutines()
    {
        bool finalized = false;
        using var cancellation = new CancellationTokenSource();
        var enumerator = Loop(() => finalized = true).WithCancellation(cancellation.Token);
        cancellation.Cancel();
        Check(await enumerator.SuppressCancellationThrow() && finalized, "IEnumerator cleanup");

        var host = new GameObject("Coroutine host");
        var runner = host.AddComponent<SmokeCoroutineHost>();
        var native = Loop(null).ToLuminTask(runner);
        Destroy(host);
        Check(await native.SuppressCancellationThrow(), "Native coroutine host cancellation");
    }

    private static IEnumerator Loop(Action cleanup)
    {
        try { while (true) yield return null; }
        finally { cleanup?.Invoke(); }
    }

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
