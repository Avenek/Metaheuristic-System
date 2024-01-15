using FluentValidation;
using Metaheuristic_system.Entities;

namespace Metaheuristic_system.Models.Validators
{
    public class UpdateFitnessFunctionDtoValidator : AbstractValidator<UpdateFitnessFunctionDto>
    {
        public UpdateFitnessFunctionDtoValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty()
               .MaximumLength(30);

            RuleFor(x => x.Dimension)
                .Custom((value, context) =>
                {
                    var isValid = value == null || (value > 0 && value < 30);
                    if(!isValid) context.AddFailure("Dimension", "Podano zły wymiar.");
                });

            RuleFor(x => x.DomainArray)
                .NotEmpty();
        }
    }
}
