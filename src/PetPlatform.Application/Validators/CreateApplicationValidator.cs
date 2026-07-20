using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class CreateApplicationValidator : AbstractValidator<CreateApplicationDto>
{
    public CreateApplicationValidator()
    {
        RuleFor(x => x.ListingId)
            .GreaterThan(0).WithMessage("ListingId must be greater than 0.");

        RuleFor(x => x.Message)
            .MaximumLength(1000).WithMessage("Message must be 1000 characters or less.");

        RuleFor(x => x.HousingType)
            .IsInEnum().WithMessage("Invalid housing type.");

        RuleFor(x => x.HasYard)
            .NotNull().WithMessage("HasYard is required.");

        RuleFor(x => x.NumberOfOccupants)
            .GreaterThanOrEqualTo(1).WithMessage("NumberOfOccupants must be at least 1.");

        RuleFor(x => x.HasChildren)
            .NotNull().WithMessage("HasChildren is required.");

        RuleFor(x => x.PreviousPets)
            .MaximumLength(1000).WithMessage("PreviousPets must be 1000 characters or less.");

        RuleFor(x => x.CurrentPets)
            .MaximumLength(1000).WithMessage("CurrentPets must be 1000 characters or less.");

        RuleFor(x => x.ExperienceLevel)
            .MaximumLength(100).WithMessage("ExperienceLevel must be 100 characters or less.");
    }
}
