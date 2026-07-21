using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class CreateVetVisitNoteValidator : AbstractValidator<CreateVetVisitNoteDto>
{
    public CreateVetVisitNoteValidator()
    {
        RuleFor(x => x.PetId)
            .GreaterThan(0).WithMessage("PetId must be greater than 0.");

        RuleFor(x => x.VisitDate)
            .NotEmpty().WithMessage("Visit date is required.");

        RuleFor(x => x.Subjective)
            .NotEmpty().WithMessage("Subjective section is required.")
            .MaximumLength(4000).WithMessage("Subjective section must be 4000 characters or less.");

        RuleFor(x => x.Objective)
            .NotEmpty().WithMessage("Objective section is required.")
            .MaximumLength(4000).WithMessage("Objective section must be 4000 characters or less.");

        RuleFor(x => x.Assessment)
            .NotEmpty().WithMessage("Assessment section is required.")
            .MaximumLength(4000).WithMessage("Assessment section must be 4000 characters or less.");

        RuleFor(x => x.Plan)
            .NotEmpty().WithMessage("Plan section is required.")
            .MaximumLength(4000).WithMessage("Plan section must be 4000 characters or less.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must be 2000 characters or less.");
    }
}
