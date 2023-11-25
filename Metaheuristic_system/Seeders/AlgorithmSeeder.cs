using Metaheuristic_system.Entities;

namespace Metaheuristic_system.Seeders
{
    public class AlgorithmSeeder
    {
        private readonly AlgorithmDbContext dbContext;

        public AlgorithmSeeder(AlgorithmDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public void Seed()
        {
            if (dbContext.Database.CanConnect())
            {
                if (!dbContext.Algorithms.Any())
                {
                    Algorithm algorithm = new Algorithm() { Name = "CHOA", FileName = "CHOA.dll", Removeable = false };
                    dbContext.Algorithms.Add(algorithm);
                    dbContext.SaveChanges();
                }
            }
        }
    }
}
