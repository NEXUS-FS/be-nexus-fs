using FluentValidation;

namespace Application.DTOs.FileOperations.Validators
{
    /// <summary>
    /// Validator for WriteFileRequest.
    /// </summary>
    public class WriteFileRequestValidator : AbstractValidator<WriteFileRequest>
    {
        public WriteFileRequestValidator()
        {
            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("ProviderId is required")
                .MaximumLength(100)
                .WithMessage("ProviderId cannot exceed 100 characters");

            RuleFor(x => x.FilePath)
                .NotEmpty()
                .WithMessage("FilePath is required")
                .MaximumLength(500)
                .WithMessage("FilePath cannot exceed 500 characters");

            RuleFor(x => x.Content)
                .NotNull()
                .WithMessage("Content is required (can be empty string for creating empty file)");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required")
                .MaximumLength(100)
                .WithMessage("UserId cannot exceed 100 characters");
        }
    }
}
