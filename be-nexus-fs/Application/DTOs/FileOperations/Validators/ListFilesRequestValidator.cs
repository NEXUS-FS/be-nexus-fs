using FluentValidation;

namespace Application.DTOs.FileOperations.Validators
{
    /// <summary>
    /// Validator for ListFilesRequest.
    /// </summary>
    public class ListFilesRequestValidator : AbstractValidator<ListFilesRequest>
    {
        public ListFilesRequestValidator()
        {
            RuleFor(x => x.ProviderId)
                .NotEmpty()
                .WithMessage("ProviderId is required")
                .MaximumLength(100)
                .WithMessage("ProviderId cannot exceed 100 characters");

            RuleFor(x => x.DirectoryPath)
                .NotEmpty()
                .WithMessage("DirectoryPath is required")
                .MaximumLength(500)
                .WithMessage("DirectoryPath cannot exceed 500 characters");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required")
                .MaximumLength(100)
                .WithMessage("UserId cannot exceed 100 characters");
        }
    }
}
