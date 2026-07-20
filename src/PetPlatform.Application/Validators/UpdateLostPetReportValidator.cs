using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class UpdateLostPetReportValidator : AbstractValidator<UpdateLostPetReportDto>
{
    public UpdateLostPetReportValidator()
    {
        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .MaximumLength(100).WithMessage("Color must be 100 characters or less.");

        RuleFor(x => x.Breed)
            .MaximumLength(100).WithMessage("Breed must be 100 characters or less.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(200).WithMessage("Location must be 200 characters or less.");

        RuleFor(x => x.DateReported)
            .NotEmpty().WithMessage("Date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Date cannot be in the far future.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must be 2000 characters or less.");
    }
}
