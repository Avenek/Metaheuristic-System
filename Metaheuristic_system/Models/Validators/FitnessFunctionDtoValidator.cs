using FluentValidation;
using Metaheuristic_system.Entities;

namespace Metaheuristic_system.Models.Validators
{
    public class FitnessFunctionDtoValidator : AbstractValidator<FitnessFunctionDto>
    {
        public FitnessFunctionDtoValidator(SystemDbContext dbContext)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(30)
                .Custom((value, context) =>
                 {
                     var nameInUse = dbContext.FitnessFunctions.Any(a => a.Name == value);
                     if (nameInUse)
                     {
                         context.AddFailure("Name", "Ta nazwa jest już zajęta.");
                     }
                 });

            RuleFor(x => x.FileName)
                .NotEmpty()
                .MaximumLength(30);

            RuleFor(x => x.Dimension)
                .Custom((value, context) =>
                {
                    var isValid = value == null || (value > 0 && value < 30);
                    if (!isValid) context.AddFailure("Dimension", "Podano zły wymiar.");
                });
        }
    }
}
