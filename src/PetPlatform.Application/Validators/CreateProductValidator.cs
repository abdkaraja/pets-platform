using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class CreateProductValidator : AbstractValidator<AdminProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Name must be 200 characters or less.");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than 0.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Category is required.");

        RuleFor(x => x.BrandId)
            .GreaterThan(0).WithMessage("Brand is required.");

        RuleFor(x => x.PetType)
            .NotEmpty().WithMessage("Pet type is required.");
    }
}
