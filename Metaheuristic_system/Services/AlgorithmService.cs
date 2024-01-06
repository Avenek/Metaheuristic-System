using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using Metaheuristic_system.Reflection;
using Metaheuristic_system.ReflectionRequiredInterfaces;
using Metaheuristic_system.Validators;

namespace Metaheuristic_system.Services
{
    public interface IAlgorithmService
    {
        IEnumerable<AlgorithmDto> GetAll();
        AlgorithmDto GetById(int id);
        void UpdateNameById(int id, string newName);
        void DeleteById(int id);
        int AddAlgorithm(string algorithmName, IFormFile file);
    }

    public class AlgorithmService : IAlgorithmService
    {
        private readonly SystemDbContext dbContext;
        private readonly IMapper mapper;

        public AlgorithmService(SystemDbContext dbContext, IMapper mapper)
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
                throw new BadRequestException("Podano zbyt długą nazwę.");
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
                throw new BadRequestException("Algorytmu o podanym id nie można usunąć.");
            }
            string path = "/dll/algorithm";
            string fileName = algorithm.FileName;
            string fullPath = $"{path}/{fileName}";
            File.Delete(fullPath);
            dbContext.Algorithms.Remove(algorithm);
            dbContext.SaveChanges();
        }

        public int AddAlgorithm(string algorithmName, IFormFile file)
        {
            if (file == null || file.Name.Length == 0) throw new BadRequestException("Napotkano problemy z plikiem.");
            if (Path.GetExtension(file.FileName) != ".dll") throw new BadRequestException($"Plik posiada złe rozszerzenie.");
            if(File.Exists("./dll/algorithm/" + file.FileName)) throw new BadRequestException("Plik o podanej nazwie już istnieje na serwerze.");
            string path = "./dll/algorithm";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fileName = file.FileName;
            string fullPath = $"{path}/{fileName}";
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            FileLoader fileLoader = new(fullPath);
            fileLoader.Load();
            var types = fileLoader.file.GetTypes();
            var optimizationType = types.FirstOrDefault(type => 
                type.GetInterfaces().Any(interfaceType => 
                ReflectionValidator.ImplementsInterface(interfaceType, typeof(IOptimizationAlgorithm))
                ));
            if (optimizationType == null)
            {
                File.Delete(fullPath);
                throw new BadRequestException("Zawartość pliku nie implementuje wymaganego interfejsu.");
            }
            var algorithm = new Algorithm() { Name = algorithmName, FileName = fileName, Removeable = true };
            dbContext.Algorithms.Add(algorithm);
            dbContext.SaveChanges();

            return algorithm.Id;
        }


    }
}