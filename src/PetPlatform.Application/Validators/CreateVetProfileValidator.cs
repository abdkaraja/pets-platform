using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class CreateVetProfileValidator : AbstractValidator<CreateVetProfileDto>
{
    public CreateVetProfileValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must be 200 characters or less.");

        RuleFor(x => x.Clinic)
            .MaximumLength(200).WithMessage("Clinic must be 200 characters or less.");

        RuleFor(x => x.Specialty)
            .MaximumLength(100).WithMessage("Specialty must be 100 characters or less.");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must be 2000 characters or less.");

        RuleFor(x => x.ServicesOffered)
            .MaximumLength(500).WithMessage("Services offered must be 500 characters or less.");
    }
}
