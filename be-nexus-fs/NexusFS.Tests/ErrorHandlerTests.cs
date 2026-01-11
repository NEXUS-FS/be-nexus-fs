using System;
using Application.Utils;
using FluentAssertions;

namespace NexusFS.Tests
{
    public class ErrorHandlerTests
    {
        [Fact]
        public void LogError_WritesMessageAndException()
        {
            var handler = new ErrorHandler();
            var sw = new System.IO.StringWriter();
            var original = Console.Out;
            Console.SetOut(sw);
            try
            {
                var ex = new InvalidOperationException("boom");
                handler.LogError("Something bad", ex);
                var output = sw.ToString();
                output.Should().Contain("[ERROR] Something bad");
                output.Should().Contain("Exception: boom");
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        [Fact]
        public void HandleError_WritesErrorCodePrefix()
        {
            var handler = new ErrorHandler();
            var sw = new System.IO.StringWriter();
            var original = Console.Out;
            Console.SetOut(sw);
            try
            {
                handler.HandleError("AUTH", "Unauthorized");
                sw.ToString().Should().Contain("[ERROR-AUTH] Unauthorized");
            }
            finally
            {
                Console.SetOut(original);
            }
        }
    }
}
