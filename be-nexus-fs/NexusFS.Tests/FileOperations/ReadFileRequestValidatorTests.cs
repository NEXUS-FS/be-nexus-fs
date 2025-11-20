using Application.DTOs.FileOperations;
using Application.DTOs.FileOperations.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace NexusFS.Tests.FileOperations
{
    public class ReadFileRequestValidatorTests
    {
        private readonly ReadFileRequestValidator _validator;

        public ReadFileRequestValidatorTests()
        {
            _validator = new ReadFileRequestValidator();
        }

        [Fact]
        public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var request = new ReadFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithEmptyProviderId_ShouldHaveValidationError()
        {
            // Arrange
            var request = new ReadFileRequest
            {
                ProviderId = "",
                FilePath = "/test/file.txt",
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
            var request = new ReadFileRequest
            {
                ProviderId = new string('a', 101), // 101 characters
                FilePath = "/test/file.txt",
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
            var request = new ReadFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "",
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
            var request = new ReadFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/" + new string('a', 500), // 501 characters
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
            var request = new ReadFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
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
            var request = new ReadFileRequest
            {
                ProviderId = "local-provider",
                FilePath = "/test/file.txt",
                UserId = new string('u', 101) // 101 characters
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("UserId cannot exceed 100 characters");
        }

        [Fact]
        public void Validate_WithMultipleErrors_ShouldHaveAllValidationErrors()
        {
            // Arrange
            var request = new ReadFileRequest
            {
                ProviderId = "",
                FilePath = "",
                UserId = ""
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ProviderId);
            result.ShouldHaveValidationErrorFor(x => x.FilePath);
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithMaximumAllowedLengths_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var request = new ReadFileRequest
            {
                ProviderId = new string('p', 100), // Exactly 100 characters
                FilePath = "/" + new string('f', 499), // Exactly 500 characters
                UserId = new string('u', 100) // Exactly 100 characters
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
