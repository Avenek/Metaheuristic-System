using FluentValidation;
using Metaheuristic_system.Entities;

namespace Metaheuristic_system.Models.Validators
{
    public class AlgorithmDtoValidator : AbstractValidator<AlgorithmDto>
    {
        public AlgorithmDtoValidator(SystemDbContext dbContext)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(30)
                .Custom((value, context) =>
                {
                    var nameInUse = dbContext.Algorithms.Any(a => a.Name == value);
                    if (nameInUse)
                    {
                        context.AddFailure("Name", "Ta nazwa jest już zajęta.");
                    }
                });
        }
    }
}
