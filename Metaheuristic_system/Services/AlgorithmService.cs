using Metaheuristic_system.Entities;
using Metaheuristic_system.Models;

namespace Metaheuristic_system.Services
{
    public interface IAlgorithmService
    {
        IEnumerable<AlgorithmDto> GetAll();
        AlgorithmDto GetById(int id);

    }

    public class AlgorithmService : IAlgorithmService
    {
        private readonly AlgorithmDbContext dbContext;
        public AlgorithmService(AlgorithmDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IEnumerable<AlgorithmDto> GetAll()
        {
            throw new NotImplementedException();
        }

        public AlgorithmDto GetById(int id)
        {
            throw new NotImplementedException();
        }
    }
}