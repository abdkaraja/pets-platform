using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class CreateListingValidator : AbstractValidator<CreateListingDto>
{
    public CreateListingValidator()
    {
        RuleFor(x => x.PetId)
            .GreaterThan(0).WithMessage("PetId must be greater than 0.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must be 200 characters or less.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must be 2000 characters or less.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(200).WithMessage("Location must be 200 characters or less.");
    }
}
