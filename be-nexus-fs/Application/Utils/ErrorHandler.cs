namespace Application.Utils;

public class ErrorHandler
{
    // Parameterless constructor for DI
    public ErrorHandler()
    {
    }

    public void LogError(string message, Exception? exception = null)
    {
        Console.WriteLine($"[ERROR] {message}");
        if (exception != null)
            Console.WriteLine($"Exception: {exception.Message}\n{exception.StackTrace}");
    }

    public void HandleError(string errorCode, string message)
    {
        Console.WriteLine($"[ERROR-{errorCode}] {message}");
    }
}
