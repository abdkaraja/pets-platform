using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class CreateVaccinationValidator : AbstractValidator<CreateVaccinationDto>
{
    public CreateVaccinationValidator()
    {
        RuleFor(x => x.PetId)
            .GreaterThan(0).WithMessage("PetId must be greater than 0.");

        RuleFor(x => x.VaccineName)
            .NotEmpty().WithMessage("Vaccine name is required.")
            .MaximumLength(200).WithMessage("Vaccine name must be 200 characters or less.");

        RuleFor(x => x.DateAdministered)
            .NotEmpty().WithMessage("Date administered is required.");

        RuleFor(x => x.BatchLotNumber)
            .MaximumLength(100).WithMessage("Batch/lot number must be 100 characters or less.");

        RuleFor(x => x.NextDueDate)
            .GreaterThan(x => x.DateAdministered)
            .When(x => x.NextDueDate.HasValue)
            .WithMessage("Next due date must be after date administered.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must be 2000 characters or less.");
    }
}
