using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using Metaheuristic_system.Reflection;
using Metaheuristic_system.ReflectionRequiredInterfaces;
using Metaheuristic_system.Validators;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Metaheuristic_system.Services
{
    public interface ITaskService
    {
        Task<List<ResultsDto>> GetResultsOfTestingAlgorithm(int id, int[] fitnessFunctionIds, CancellationToken cancellationToken);
    }

    public class TaskService : ITaskService
    {
        private readonly SystemDbContext dbContext;
        private readonly IMapper mapper;

        public TaskService(SystemDbContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        async public Task<List<ResultsDto>> GetResultsOfTestingAlgorithm(int id, int[] fitnessFunctionIds, CancellationToken cancellationToken)
        {
            var selectedFunctions = dbContext.FitnessFunctions.Where(f => fitnessFunctionIds.Contains(f.Id)).ToList();
            var algorithm = dbContext.Algorithms.First(a => a.Id == id);
            string algorithmPath = "./dll/algorithm/";
            string functionPath = "./dll/fitnessFunction/";
            FileLoader fileLoader = new(algorithmPath + algorithm.FileName);
            fileLoader.Load();
            var aTypes = fileLoader.file.GetTypes();
            var optimizationType = aTypes.FirstOrDefault(type =>
                type.GetInterfaces().Any(interfaceType =>
                ReflectionValidator.ImplementsInterface(interfaceType, typeof(IOptimizationAlgorithm))
                ));
            List<Type> functionTypes = GetSelectedFunctionsType(selectedFunctions, functionPath, fileLoader);

            if (optimizationType != null)
            {
                dynamic optimizationAlgorithm = Activator.CreateInstance(optimizationType);
                List<ResultsDto> results = await PrepareDataAndInvokeTests(optimizationAlgorithm, functionTypes, selectedFunctions);
                return results;
            }
            else
            {
                throw new NotImplementInterfaceException("Zawartość pliku nie implementuje wymaganego interfejsu.");
            }
        }

        async private Task<List<ResultsDto>> PrepareDataAndInvokeTests(dynamic optimizationAlgorithm, List<Type> functionTypes, List<FitnessFunction> selectedFunctions)
        {
            List<ResultsDto> results = new List<ResultsDto>();
            dynamic paramsData = optimizationAlgorithm.ParamsInfo;
            double[] paramsValue = InitializeAlgorithmParams(paramsData);
            int availableProcessors = Environment.ProcessorCount;
            int maxParallelTasks = availableProcessors > 3 ? availableProcessors - 2 : 1;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelTasks);
            List<Task> solvingTasks = new List<Task>();
            foreach (var function in functionTypes)
            {
                await semaphore.WaitAsync();

                solvingTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        int dimension = -1;
                        for (int i = 0; i < paramsData.Length; i++)
                        {
                            if (paramsData[i].Name == "Dimension")
                            {
                                dimension = (int)paramsValue[i];
                                break;
                            }
                        }
                        if (dimension == -1) throw new NotFoundException("Brak parametru Dimension w ParamsInfo");
                        double[,] domainArray = GetFunctionDomain(function, selectedFunctions, dimension);
                        dynamic fitnessFunction = Activator.CreateInstance(function);
                        List<ResultsDto> bestParameters = InvokeAlgorithmTest(optimizationAlgorithm, domainArray, fitnessFunction, paramsValue);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }


            return results;
        }

        private List<ResultsDto> InvokeAlgorithmTest(dynamic optimizationAlgorithm, double[,] domainArray, dynamic fitnessFunction, double[] paramsValue)
        {
            optimizationAlgorithm.Solve(fitnessFunction, domainArray, paramsValue);
            Console.WriteLine(optimizationAlgorithm.FBest);
            return new List<ResultsDto>();
        }

        private double[,] GetFunctionDomain(Type functionType, List<FitnessFunction> selectedFunctions, int dimension)
        {
            FitnessFunction function = selectedFunctions.FirstOrDefault(f => f.Name == functionType.Name);
            string domain = function.Domain;
            double[,] domainArray = JsonConvert.DeserializeObject<double[,]>(domain);
            if (domainArray == null || domainArray.GetLength(0) == 0 || domainArray.GetLength(1) == 0)
            {
                domainArray = new double[dimension, 2];
                for (int i = 0; i < dimension; i++)
                {
                    domainArray[i, 0] = -1000000;
                    domainArray[i, 1] = 1000000;
                }
            }
            return domainArray;
        }

        private List<Type> GetSelectedFunctionsType(List<FitnessFunction> selectedFunctions, string functionPath, FileLoader fileLoader)
        {
            List<Type> functionTypes = new();
            foreach (var function in selectedFunctions)
            {
                FileLoader loader = new(functionPath + function.FileName);
                fileLoader.Load();
                var fTypes = fileLoader.file.GetTypes();
                var searchType = fTypes.FirstOrDefault(type => type.GetInterfaces().Any(interfaceType =>
                    ReflectionValidator.ImplementsInterface(interfaceType, typeof(IFitnessFunction)) && type.IsClass && type.Name == function.Name));
                if (searchType != null)
                {
                    functionTypes.Add(searchType);
                }
                else
                {
                    throw new NotFoundException($"Nie znaleziono funkcji {function.Name} w pliku {function.FileName}.");
                }
            }
            return functionTypes;
        }
        private double[] InitializeAlgorithmParams(dynamic paramsData)
        {
            double[] paramsValue = new double[paramsData.Length];
            for (int i = 0; i < paramsData.Length; i++)
            {
                paramsValue[i] = paramsData[i].LowerBoundary;
            }
            return paramsValue;
        }
    }
}