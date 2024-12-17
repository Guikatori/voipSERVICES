using System.Timers;

namespace Helpers.TaskCompletionSource;

public class TasksCs
{

    public static void disposeResources(dynamic timer)
    {

        if (timer == null)
        {
            return;
        }
        timer.Stop();
        timer.Dispose();
    }
}