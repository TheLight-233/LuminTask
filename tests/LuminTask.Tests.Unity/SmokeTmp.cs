using LuminThread.Unity;
using TMPro;
using UnityEngine.Events;

public sealed partial class SmokeEntry
{
    partial void CheckOptionalAdapters()
    {
        var selected = new TMP_InputField.TextSelectionEvent();
        using var handler = new TextMeshProAsyncLuminTask.AsyncTmpTextSelectionLuminTaskEventHandler(selected, default, false);
        var selection = handler.OnTextSelectionAsync();
        selected.Invoke("lumin", 1, 4);
        Check(selection.GetResult() == ("lumin", 1, 4), "TMP text selection");

        var signal = new UnityEvent<string>();
        using TextMeshProAsyncLuminTask.IAsyncSubmitLuminTaskEventHandler<string> submit =
            new UnityAsyncLuminTask.AsyncUnityLuminTaskEventHandler<string>(signal, default, false);
        var result = submit.OnSubmitAsync();
        signal.Invoke("submit");
        Check(result.GetResult() == "submit", "TMP generic event adapter");
    }
}
