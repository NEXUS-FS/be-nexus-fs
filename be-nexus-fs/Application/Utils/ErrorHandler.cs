/// <summary>
/// Centralized error handling and exception logging utility.
/// Implements Singleton pattern to ensure single shared instance.
/// </summary>

namespace Application.Utils
{
    public sealed class ErrorHandler
    {
        private static readonly Lazy<ErrorHandler> _instance =
            new Lazy<ErrorHandler>(() => new ErrorHandler());

        private ErrorHandler()
        {
            // Private constructor prevents external instantiation
        }

        public static ErrorHandler Instance => _instance.Value;

        public void LogError(Exception exception, string context)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public void LogWarning(string message, string context)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public void HandleException(Exception exception, string context)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }

        public string FormatErrorMessage(Exception exception)
        {
            // Will be implemented in Story 2
            throw new NotImplementedException();
        }
    }
}
