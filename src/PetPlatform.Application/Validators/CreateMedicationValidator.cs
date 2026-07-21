using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class CreateMedicationValidator : AbstractValidator<CreateMedicationDto>
{
    public CreateMedicationValidator()
    {
        RuleFor(x => x.PetId)
            .GreaterThan(0).WithMessage("PetId must be greater than 0.");

        RuleFor(x => x.MedicationName)
            .NotEmpty().WithMessage("Medication name is required.")
            .MaximumLength(200).WithMessage("Medication name must be 200 characters or less.");

        RuleFor(x => x.Dosage)
            .NotEmpty().WithMessage("Dosage is required.")
            .MaximumLength(100).WithMessage("Dosage must be 100 characters or less.");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("Frequency is required.")
            .MaximumLength(100).WithMessage("Frequency must be 100 characters or less.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date must be after start date.");

        RuleFor(x => x.PrescribingReason)
            .MaximumLength(500).WithMessage("Prescribing reason must be 500 characters or less.");

        RuleFor(x => x.Instructions)
            .MaximumLength(1000).WithMessage("Instructions must be 1000 characters or less.");

        RuleFor(x => x.SideEffectsNoted)
            .MaximumLength(1000).WithMessage("Side effects noted must be 1000 characters or less.");
    }
}
