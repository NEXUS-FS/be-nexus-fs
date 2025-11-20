using Application.DTOs.FileOperations;
using Application.DTOs.FileOperations.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace NexusFS.Tests.FileOperations
{
    public class ListFilesRequestValidatorTests
    {
        private readonly ListFilesRequestValidator _validator;

        public ListFilesRequestValidatorTests()
        {
            _validator = new ListFilesRequestValidator();
        }

        [Fact]
        public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/test",
                Recursive = false,
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithRecursiveTrue_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/test",
                Recursive = true,
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
            var request = new ListFilesRequest
            {
                ProviderId = "",
                DirectoryPath = "/test",
                Recursive = false,
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
            var request = new ListFilesRequest
            {
                ProviderId = new string('a', 101),
                DirectoryPath = "/test",
                Recursive = false,
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ProviderId)
                .WithErrorMessage("ProviderId cannot exceed 100 characters");
        }

        [Fact]
        public void Validate_WithEmptyDirectoryPath_ShouldHaveValidationError()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "",
                Recursive = false,
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DirectoryPath)
                .WithErrorMessage("DirectoryPath is required");
        }

        [Fact]
        public void Validate_WithDirectoryPathTooLong_ShouldHaveValidationError()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/" + new string('a', 500),
                Recursive = false,
                UserId = "user-123"
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DirectoryPath)
                .WithErrorMessage("DirectoryPath cannot exceed 500 characters");
        }

        [Fact]
        public void Validate_WithEmptyUserId_ShouldHaveValidationError()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/test",
                Recursive = false,
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
            var request = new ListFilesRequest
            {
                ProviderId = "local-provider",
                DirectoryPath = "/test",
                Recursive = false,
                UserId = new string('u', 101)
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
            var request = new ListFilesRequest
            {
                ProviderId = "",
                DirectoryPath = "",
                Recursive = false,
                UserId = ""
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ProviderId);
            result.ShouldHaveValidationErrorFor(x => x.DirectoryPath);
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_WithMaximumAllowedLengths_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var request = new ListFilesRequest
            {
                ProviderId = new string('p', 100),
                DirectoryPath = "/" + new string('d', 499),
                Recursive = true,
                UserId = new string('u', 100)
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
