using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using System.Security.Cryptography;

namespace Metaheuristic_system.Services
{
    public interface IAlgorithmService
    {
        IEnumerable<AlgorithmDto> GetAll();
        AlgorithmDto GetById(int id);
        void UpdateNameById(int id, string newName);
        void DeleteById(int id);
        void AddAlgorithm(AlgorithmDto newAlgorithmDto)
    }

    public class AlgorithmService : IAlgorithmService
    {
        private readonly AlgorithmDbContext dbContext;
        private readonly IMapper mapper;

        public AlgorithmService(AlgorithmDbContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public IEnumerable<AlgorithmDto> GetAll()
        {
            var algorithms = dbContext.Algorithms.ToList();

            var algorithmsDtos = mapper.Map<List<AlgorithmDto>>(algorithms);
            return algorithmsDtos;
        }

        public AlgorithmDto GetById(int id)
        {
            var algorithm = dbContext.Algorithms.FirstOrDefault(a => a.Id == id);
            if (algorithm == null)
            {
                throw new NotFoundException($"Nie odnaleziono algorytmu o id {id}");
            }
            var algorithmDto = mapper.Map<AlgorithmDto>(algorithm);

            return algorithmDto;
        }

        public void UpdateNameById(int id, string newName)
        {
            var algorithm = dbContext.Algorithms.FirstOrDefault(a => a.Id == id);
            if (algorithm == null)
            {
                throw new NotFoundException($"Nie odnaleziono algorytmu o id {id}.");
            }
            if(newName.Length > 30)
            {
                throw new TooLongNameException("Podano zbyt długą nazwę.");
            }
            algorithm.Name = newName;
            dbContext.SaveChanges();
        }

        public void DeleteById(int id)
        {
            var algorithm = dbContext.Algorithms.FirstOrDefault(a => a.Id == id);
            if (algorithm == null)
            {
                throw new NotFoundException($"Nie odnaleziono algorytmu o id {id}.");
            }
            if (!algorithm.Removeable)
            {
                throw new IsNotRemoveableException("Algorytmu o podanym id nie można usunąć.");
            }
            dbContext.Algorithms.Remove(algorithm);
            dbContext.SaveChanges();
        }

        public void AddAlgorithm(AlgorithmDto newAlgorithmDto)
        {
            if(newAlgorithmDto.Name.Length > 30)
            {
                throw new TooLongNameException("Podano zbyt długą nazwę algoryutmu.");
            }
            if(newAlgorithmDto.FileName.Length > 30)
            {
                throw new TooLongNameException("Podano zbyt długą nazwę pliku.");
            }
            var algorithm = mapper.Map<Algorithm>(newAlgorithmDto);
            dbContext.Algorithms.Add(algorithm);
            dbContext.SaveChanges();
        }


    }
}