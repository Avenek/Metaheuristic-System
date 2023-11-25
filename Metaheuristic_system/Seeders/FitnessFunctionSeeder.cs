using Metaheuristic_system.Entities;

namespace Metaheuristic_system.Seeders
{
    public class FitnessFunctionSeeder
    {
        private readonly AlgorithmDbContext dbContext;

        public FitnessFunctionSeeder(AlgorithmDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public void Seed()
        {
            if (dbContext.Database.CanConnect())
            {
                if (!dbContext.FitnessFunctions.Any())
                {
                    FitnessFunction fitnessFunction = new FitnessFunction() { Name = "Rastrigin", FileName = "Rastrigin.dll", Removeable = false };
                    dbContext.FitnessFunctions.Add(fitnessFunction);
                    dbContext.SaveChanges();
                }
            }
        }
    }
}
