namespace Infrastructure.AOP
{
    /// <summary>
    /// AOP Aspect for automatic retry logic on failures.
    /// Intercepts method calls to retry failed operations with configurable policies.
    /// 
    /// Join Points:
    /// - AROUND: Wrap method execution with retry logic
    /// - AFTER_THROWING: Determine if exception is retryable, execute retry
    /// 
    /// Retry Strategy:
    /// - Max attempts: 3 (configurable)
    /// - Delay: Fixed delay between retries (configurable)
    /// - Retryable exceptions: Network errors, timeouts, transient database errors
    /// 
    /// Target Classes: Provider, NexusApi, external API calls
    /// </summary>
    public class RetryAspect
    {
        // Placeholder for AOP implementation
        // Will be implemented using interceptor pattern or AOP framework
        // Example advice methods:
        // - OnMethodRetry(string methodName, int attemptNumber)
        // - OnMethodFailure(string methodName, Exception exception)
        // - ShouldRetry(Exception exception)
    }
}

// implementatin on next lab... just a skeleton here...
// using PostSharp.Aspects;
// using Infrastructure.Services.Observability;
// using System;
// using System.Reflection;
// using System.Threading;
// using System.Net.Http;
// using System.Net.Sockets;
// using System.IO;
// using System.Data.SqlClient;
//
// namespace Infrastructure.AOP
// {
//     /// <summary>
//     /// PostSharp-based retry aspect that can be applied as an attribute.
//     /// Automatically retries failed method calls with fixed delay.
//     /// </summary>
//     public class RetryAspect
//     {
//         // Configuration properties
//         public int MaxAttempts { get; set; } = 3;
//         public int DelayMs { get; set; } = 500;
//
//         [NonSerialized]
//         private Logger _logger;
//
//         public override void RuntimeInitialize(MethodBase method)
//         {
//             base.RuntimeInitialize(method);
//            
//             _logger = ServiceLocator.GetService<Logger>();
//         }
//
//         public override void OnInvoke(MethodInterceptionArgs args)
//         {
//             var methodName = $"{args.Method.DeclaringType.Name}.{args.Method.Name}";
//             int attempt = 0;
//
//             while (attempt < MaxAttempts)
//             {
//                 attempt++;
//                 
//                 try
//                 {
//                     OnMethodRetry(methodName, attempt);
//                     
//                     // Execute the actual method
//                     args.Proceed();
//                     
//                     // Success
//                     if (attempt > 1)
//                     {
//                         _logger?.LogInfo($"[RETRY SUCCESS] {methodName} succeeded on attempt {attempt}");
//                     }
//                     return;
//                 }
//                 catch (Exception ex)
//                 {
//                     // Check if exception is retryable
//                     if (!ShouldRetry(ex))
//                     {
//                         _logger?.LogWarning($"[RETRY ABORT] {methodName} - Non-retryable exception: {ex.GetType().Name}");
//                         throw;
//                     }
//
//                     // Last attempt - throw exception
//                     if (attempt >= MaxAttempts)
//                     {
//                         OnMethodFailure(methodName, ex);
//                         throw;
//                     }
//
//                     // Wait before retry
//                     _logger?.LogWarning($"[RETRY] {methodName} failed on attempt {attempt}. Retrying in {DelayMs}ms...");
//                     Thread.Sleep(DelayMs);
//                 }
//             }
//         }
//
//         private void OnMethodRetry(string methodName, int attemptNumber)
//         {
//             _logger?.LogInfo($"[RETRY] {methodName} - Attempt {attemptNumber}/{MaxAttempts}");
//         }
//
//         private void OnMethodFailure(string methodName, Exception exception)
//         {
//             _logger?.LogError($"[RETRY FAILURE] {methodName} failed after {MaxAttempts} attempts");
//             _logger?.LogError($"Exception: {exception.Message}");
//             _logger?.LogError($"Stack trace: {exception.StackTrace}");
//         }
//
//         private bool ShouldRetry(Exception exception)
//         {
//             //  retryable exception types
//             return exception is TimeoutException
//                 || exception is HttpRequestException
//                 || exception is SocketException
//                 || exception is IOException
//                 || (exception is SqlException sqlEx && IsTransientSqlError(sqlEx));
//         }
//
//
//         private bool IsTransientSqlError(SqlException sqlException)
//         {
//             // List of transient SQL error numbers.... need to see some ...
//             int[] transientErrorCodes = { ... real numbers here.. };
//             return Array.Exists(transientErrorCodes, code => code == sqlException.Number);
//         }
//     }
// }
