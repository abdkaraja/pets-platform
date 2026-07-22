using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class UpdatePetValidator : AbstractValidator<UpdatePetDto>
{
    public UpdatePetValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Pet name is required.")
            .MaximumLength(100).WithMessage("Name must be 100 characters or less.");

        RuleFor(x => x.Age)
            .InclusiveBetween(0, 50).WithMessage("Age must be between 0 and 50.");

        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(0).WithMessage("Weight cannot be negative.")
            .LessThanOrEqualTo(500).WithMessage("Weight must be less than 500 kg.");
    }
}
