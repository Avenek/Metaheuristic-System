using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using Newtonsoft.Json;

namespace Metaheuristic_system.Services
{
    public interface IFitnessFunctionService
    {
        IEnumerable<FitnessFunctionDto> GetAll();
        FitnessFunctionDto GetById(int id);
        void UpdateNameById(int id, string newName);
        void DeleteById(int id);
        void UpdateDomainAndDimensionById(int id, DimensionAndDomainDto updatedFunction)
    }

    public class FitnessFunctionService : IFitnessFunctionService
    {
        private readonly AlgorithmDbContext dbContext;
        private readonly IMapper mapper;

        public FitnessFunctionService(AlgorithmDbContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public IEnumerable<FitnessFunctionDto> GetAll()
        {
            var fitnessFunctions = dbContext.FitnessFunctions.ToList();

            var fitnessFunctionsDtos = mapper.Map<List<FitnessFunctionDto>>(fitnessFunctions);
            return fitnessFunctionsDtos;
        }

        public FitnessFunctionDto GetById(int id)
        {
            var fitnessFunction = dbContext.FitnessFunctions.FirstOrDefault(f => f.Id == id);
            var fitnessFunctionDto = mapper.Map<FitnessFunctionDto>(fitnessFunction);

            return fitnessFunctionDto;
        }

        public void UpdateNameById(int id, string newName)
        {
            var fitnessFunction = dbContext.FitnessFunctions.FirstOrDefault(a => a.Id == id);
            if (fitnessFunction == null)
            {
                throw new NotFoundException($"Nie odnaleziono funkcji o id {id}.");
            }
            if (newName.Length > 30)
            {
                throw new TooLongNameException("Podano zbyt długą nazwę.");
            }
            fitnessFunction.Name = newName;
            dbContext.SaveChanges();
        }

        public void DeleteById(int id)
        {
            var fitnessFunction = dbContext.FitnessFunctions.FirstOrDefault(a => a.Id == id);
            if (fitnessFunction == null)
            {
                throw new NotFoundException($"Nie odnaleziono funkcji o id {id}.");
            }
            if (!fitnessFunction.Removeable)
            {
                throw new IsNotRemoveableException("Funkcji o podanym id nie można usunąć.");
            }
            dbContext.FitnessFunctions.Remove(fitnessFunction);
            dbContext.SaveChanges();
        }

        public void UpdateDomainAndDimensionById(int id, DimensionAndDomainDto updatedFunction)
        {
            var fitnessFunction = dbContext.FitnessFunctions.FirstOrDefault(a => a.Id == id);
            if (fitnessFunction == null)
            {
                throw new NotFoundException($"Nie odnaleziono funkcji o id {id}.");
            }
            if (updatedFunction.Dimension < 0)
            {
                throw new NegativeDimension("Wymiar nie może być mniejszy od 0.");
            }
            fitnessFunction.Dimension = updatedFunction.Dimension;
            string jsonDomain = JsonConvert.SerializeObject(updatedFunction.DomainArray);
            fitnessFunction.Domain = jsonDomain;
            dbContext.SaveChanges();
        }
    }
}