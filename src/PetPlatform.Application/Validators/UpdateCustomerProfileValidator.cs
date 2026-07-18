using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class UpdateCustomerProfileValidator : AbstractValidator<UpdateCustomerProfileDto>
{
    public UpdateCustomerProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must be 100 characters or less.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must be 20 characters or less.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must be 100 characters or less.");
    }
}
