using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using Metaheuristic_system.Reflection;
using Metaheuristic_system.ReflectionRequiredInterfaces;
using Metaheuristic_system.Validators;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;

namespace Metaheuristic_system.Services
{
    public interface IFitnessFunctionService
    {
        IEnumerable<FitnessFunctionDto> GetAll();
        FitnessFunctionDto GetById(int id);
        void DeleteById(int id);
        void UpdateFitnessFunctionById(int id, UpdateFitnessFunctionDto updatedFunction);
        int AddFitnessFunction(FitnessFunctionDto newFitnessFunctionDto);
        void UploadFitnessFunctionFile([FromForm] IFormFile file);
    }

    public class FitnessFunctionService : IFitnessFunctionService
    {
        private readonly SystemDbContext dbContext;
        private readonly IMapper mapper;

        public FitnessFunctionService(SystemDbContext dbContext, IMapper mapper)
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
        public void DeleteById(int id)
        {
            var fitnessFunction = dbContext.FitnessFunctions.FirstOrDefault(a => a.Id == id);
            if (fitnessFunction == null)
            {
                throw new NotFoundException($"Nie odnaleziono funkcji o id {id}.");
            }
            if (!fitnessFunction.Removeable)
            {
                throw new BadRequestException("Funkcji o podanym id nie można usunąć.");
            }
            dbContext.FitnessFunctions.Remove(fitnessFunction);
            dbContext.SaveChanges();
            string path = "/dll/fitnessFunction";
            string fileName = fitnessFunction.FileName;
            if (!dbContext.FitnessFunctions.Any(f => f.FileName == fileName)){
                string fullPath = $"{path}/{fileName}";
                File.Delete(fullPath);
            }
        }

        public void UpdateFitnessFunctionById(int id, UpdateFitnessFunctionDto updatedFunction)
        {
            var fitnessFunction = dbContext.FitnessFunctions.FirstOrDefault(a => a.Id == id);
            if (fitnessFunction == null)
            {
                throw new NotFoundException($"Nie odnaleziono funkcji o id {id}.");
            }
            fitnessFunction.Dimension = updatedFunction.Dimension;
            string jsonDomain = JsonConvert.SerializeObject(updatedFunction.DomainArray);
            fitnessFunction.Domain = jsonDomain;
            fitnessFunction.Name = updatedFunction.Name;
            dbContext.SaveChanges();
        }

        public void UploadFitnessFunctionFile(IFormFile file)
        {
            if (file.FileName.Length > 30) throw new BadRequestException("Podano zbyt długą nazwę pliku.");
            if (file == null || file.Name.Length == 0) throw new BadRequestException("Napotkano problemy z plikiem.");
            if (Path.GetExtension(file.FileName) != ".dll") throw new BadRequestException("Plik posiada złe rozszerzenie.");
            string path = "./dll/fitnessFunction";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string fileName = file.FileName;
            string fullPath = $"{path}/{fileName}";
            if (File.Exists(fullPath)) return;
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                file.CopyTo(stream);
            }
            FileLoader fileLoader = new(fullPath);
            fileLoader.Load();
            var types = fileLoader.file.GetTypes();
            var optimizationType = types.FirstOrDefault(type => type.GetInterfaces().Any(interfaceType => ReflectionValidator.ImplementsInterface(interfaceType, typeof(IFitnessFunction))));
            if (optimizationType == null)
            {
                File.Delete(fullPath);
                throw new BadRequestException("Zawartość pliku nie implementuje wymaganego interfejsu.");
            }

        }

        public int AddFitnessFunction(FitnessFunctionDto newFitnessFunctionDto)
        {
            string path = "./dll/fitnessFunction/";
            string fullPath = path + newFitnessFunctionDto.FileName;
            if (!File.Exists(path+newFitnessFunctionDto.FileName)) throw new NotFoundException("Nie odnaleziono wymaganego pliku.");

            FileLoader fileLoader = new(fullPath);
            fileLoader.Load();
            var types = fileLoader.file.GetTypes();
            var optimizationType = types.FirstOrDefault(type => type.GetInterfaces().Any(interfaceType => 
            ReflectionValidator.ImplementsInterface(interfaceType, typeof(IFitnessFunction)) && type.IsClass && type.Name == newFitnessFunctionDto.Name));
            if (optimizationType == null)
            {
                throw new BadRequestException("Zawartość pliku nie implementuje wymaganej klasy.");
            }
            var fitnessFunction = mapper.Map<FitnessFunction>(newFitnessFunctionDto);
            fitnessFunction.Removeable = true;
            dbContext.FitnessFunctions.Add(fitnessFunction);
            dbContext.SaveChanges();

            return fitnessFunction.Id;
        }
    }
}