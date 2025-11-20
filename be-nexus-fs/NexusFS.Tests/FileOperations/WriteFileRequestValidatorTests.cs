using Application.DTOs.FileOperations;
using Application.DTOs.FileOperations.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace NexusFS.Tests.FileOperations
{
    public class WriteFileRequestValidatorTests
    {
        private readonly WriteFileRequestValidator _validator;

        public WriteFileRequestValidatorTests()
        {
            _validator = new WriteFileRequestValidator();
        }

        [Fact]
        public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                Content = "Hello World",
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyContent_ShouldNotHaveValidationError()
        {
            // Arrange - empty content should be allowed
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/empty.txt",
                Content = "",
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Content);
        }

        [Fact]
        public void Validate_WithEmptyProviderId_ShouldHaveValidationError()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "",
                FilePath = "/test/file.txt",
                Content = "content",
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ProviderId)
                .WithErrorMessage("ProviderId is required");
        }

        [Fact]
        public void Validate_WithProviderIdTooLong_ShouldHaveValidationError()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = new string('a', 101),
                FilePath = "/test/file.txt",
                Content = "content",
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ProviderId)
                .WithErrorMessage("ProviderId cannot exceed 100 characters");
        }

        [Fact]
        public void Validate_WithEmptyFilePath_ShouldHaveValidationError()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "",
                Content = "content",
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FilePath)
                .WithErrorMessage("FilePath is required");
        }

        [Fact]
        public void Validate_WithFilePathTooLong_ShouldHaveValidationError()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/" + new string('a', 500),
                Content = "content",
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FilePath)
                .WithErrorMessage("FilePath cannot exceed 500 characters");
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveValidationError()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                Content = "content",
                UserId = ""
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("UserId is required");
        }

        [Fact]
        public void Validate_WithUserIdTooLong_ShouldHaveValidationError()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                Content = "content",
                UserId = new string('u', 101)
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("UserId cannot exceed 100 characters");
        }

        [Fact]
        public void Validate_WithLargeContent_ShouldNotHaveValidationError()
        {
            // Arrange - large content should be allowed
            var request = new WriteFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/large.txt",
                Content = new string('x', 10000),
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Content);
        }

        [Fact]
        public void Validate_WithMultipleErrors_ShouldHaveAllValidationErrors()
        {
            // Arrange
            var request = new WriteFileRequest
            {
                ProviderId = "",
                FilePath = "",
                Content = "content",
                UserId = ""
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ProviderId);
            result.ShouldHaveValidationErrorFor(x => x.FilePath);
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }
    }
}
