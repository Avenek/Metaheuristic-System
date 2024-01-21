using Metaheuristic_system.Entities;

namespace Metaheuristic_system.Seeders
{
    public class FitnessFunctionSeeder
    {
        private readonly SystemDbContext dbContext;

        public FitnessFunctionSeeder(SystemDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public void Seed()
        {
            if (dbContext.Database.CanConnect())
            {
                if (!dbContext.FitnessFunctions.Any())
                {
                    FitnessFunction fitnessFunction = new FitnessFunction() { Name = "Rastrigin", Dimension = null, Domain = "[ [-5.12, 5.12] ]", FileName = "CHOA.dll", Removeable = false };
                    dbContext.FitnessFunctions.Add(fitnessFunction);
                    dbContext.SaveChanges();
                }
            }
        }
    }
}
