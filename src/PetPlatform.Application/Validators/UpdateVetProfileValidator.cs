using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class UpdateVetProfileValidator : AbstractValidator<UpdateVetProfileDto>
{
    public UpdateVetProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name must be 200 characters or less.");

        RuleFor(x => x.Clinic)
            .MaximumLength(200).WithMessage("Clinic name must be 200 characters or less.");

        RuleFor(x => x.Specialty)
            .MaximumLength(200).WithMessage("Specialty must be 200 characters or less.");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must be 2000 characters or less.");

        RuleFor(x => x.ServicesOffered)
            .MaximumLength(2000).WithMessage("Services offered must be 2000 characters or less.");
    }
}
