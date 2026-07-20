using FluentValidation;
using PetPlatform.Application.DTOs;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.Validators;

public class ReviewApplicationValidator : AbstractValidator<ReviewApplicationDto>
{
    public ReviewApplicationValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status.")
            .Must(s => s == ApplicationStatus.Approved || s == ApplicationStatus.Rejected)
            .WithMessage("You can only approve or reject an application.");

        RuleFor(x => x.ReviewNotes)
            .MaximumLength(500).WithMessage("ReviewNotes must be 500 characters or less.");
    }
}
