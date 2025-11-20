using FluentValidation;

namespace Application.DTOs.FileOperations.Validators
{
    /// <summary>
    /// Validator for DeleteFileRequest.
    /// </summary>
    public class DeleteFileRequestValidator : AbstractValidator<DeleteFileRequest>
    {
        public DeleteFileRequestValidator()
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

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required")
                .MaximumLength(100)
                .WithMessage("UserId cannot exceed 100 characters");
        }
    }
}
