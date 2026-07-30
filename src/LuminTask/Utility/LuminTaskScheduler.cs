using System;

namespace LuminThread.Utility;

public static class LuminTaskScheduler
{
    public static event Action<Exception>? UnobservedTaskException;

    public static bool PropagateOperationCanceledException = false;

    internal static void PublishUnobservedTaskException(Exception? ex)
    {
        if (ex == null) return;

        if (!PropagateOperationCanceledException && ex is OperationCanceledException)
        {
            return;
        }

        var handler = UnobservedTaskException;
        if (handler != null)
        {
            try
            {
                handler(ex);
            }
            catch (Exception observerException)
            {
                // Diagnostic observers must not terminate the shared PlayerLoop worker.
                Console.WriteLine("UnobservedTaskException handler failed: " + observerException);
            }
        }
        else
        {
            Console.WriteLine("UnobservedTaskException: " + ex);
        }
    }
}
