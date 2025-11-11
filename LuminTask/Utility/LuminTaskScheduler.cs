using System;

namespace LuminThread.Utility;

public static class LuminTaskScheduler
{
    public static event Action<Exception> UnobservedTaskException;
    
    public static bool PropagateOperationCanceledException = false;

    internal static void PublishUnobservedTaskException(Exception? ex)
    {
        if (ex == null) return;
        
        if (!PropagateOperationCanceledException && ex is OperationCanceledException)
        {
            return;
        }

        if (UnobservedTaskException != null)
        {
            UnobservedTaskException.Invoke(ex);
        }
        else
        {
            Console.WriteLine("UnobservedTaskException: " + ex);
        }
    }
}