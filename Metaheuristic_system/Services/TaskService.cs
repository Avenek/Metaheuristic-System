using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using Metaheuristic_system.Reflection;
using Metaheuristic_system.ReflectionRequiredInterfaces;
using Metaheuristic_system.Validators;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace Metaheuristic_system.Services
{
    public interface ITaskService
    {
        Task<List<ResultsDto>> TestAlgorithm(int id, int[] fitnessFunctionIds, CancellationToken cancellationToken, int sessionId = 0, bool resume = false);
        Task<List<ResultsDto>> TestFitnessFunction(int id, int[] algorithmIds, CancellationToken cancellationToken, int sessionId = 0, bool resume = false);
        Task<List<ResultsDto>> ResumeSession(int sessionId, CancellationToken cancellationToken);
        List<TestsDto> GetCurrentProgress();
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

        #region Testing Algorithm
        async public Task<List<ResultsDto>> TestAlgorithm(int id, int[] fitnessFunctionIds, CancellationToken cancellationToken, int sessionId = 0, bool resume = false)
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
            Dictionary<int, Type> functionTypes = GetSelectedFunctionsType(selectedFunctions, functionPath);

            if (optimizationType != null)
            {
                dynamic optimizationAlgorithm = Activator.CreateInstance(optimizationType); 
                Sessions session;
                if (!resume)
                {
                    session = new() { AlgorithmIds = id.ToString(), FitnessFunctionIds = String.Join(";", fitnessFunctionIds), State = "RUNNING" };
                    dbContext.Sessions.Add(session);
                    dbContext.SaveChanges();
                    sessionId = session.Id;
                }
                else
                {
                    session = dbContext.Sessions.FirstOrDefault(s => s.Id == sessionId);
                }

                List<ResultsDto> results = await PrepareDataAndInvokeTests(optimizationAlgorithm, functionTypes, selectedFunctions, sessionId, id, cancellationToken, resume);
                foreach (var result in results)
                {
                    result.AlgorithmId = id;
                }
                session.State = "FINISHED";
                dbContext.SaveChanges();
                return results;
            }
            else
            {
                throw new NotImplementInterfaceException("Zawartość pliku nie implementuje wymaganego interfejsu.");
            }
        }

        async private Task<List<ResultsDto>> PrepareDataAndInvokeTests(dynamic optimizationAlgorithm, Dictionary<int, Type> functionTypes, List<FitnessFunction> selectedFunctions, int sessionId, int algorithmId, CancellationToken cancellationToken, bool resume)
        {
            List<ResultsDto> results = new List<ResultsDto>();
            dynamic paramsData = optimizationAlgorithm.ParamsInfo;
            int availableProcessors = Environment.ProcessorCount;
            int maxParallelTasks = availableProcessors > 3 ? availableProcessors - 2 : 1;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelTasks);
            List<Task> solvingTasks = new List<Task>();
            foreach (var f in functionTypes.Keys)
            {
                await semaphore.WaitAsync();
                if (cancellationToken.IsCancellationRequested) return null;
                Tests tests = new() { SessionId = sessionId, AlgorithmId = algorithmId, FitnessFunctionId = f, Progress = 0 };
                dbContext.Tests.Add(tests);
                dbContext.SaveChanges();
                solvingTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        tests.FitnessFunctionId = f;
                        int dimension = -1;
                        for (int i = 0; i < paramsData.Length; i++)
                        {
                            if (paramsData[i].Name == "Dimension")
                            {
                                dimension = (int)paramsData[i].LowerBoundary;
                                break;
                            }
                        }
                        if (dimension == -1) throw new NotFoundException("Brak parametru Dimension w ParamsInfo");
                        double[,] domainArray = GetFunctionDomain(functionTypes[f], selectedFunctions, dimension);
                        dynamic fitnessFunction = Activator.CreateInstance(functionTypes[f]);
                        Dictionary<string, double> bestParameters = await InvokeTest(optimizationAlgorithm, domainArray, fitnessFunction, tests, sessionId, algorithmId, f, cancellationToken, resume);
                        ResultsDto bestResult = new ResultsDto();
                        bestResult.FitnessFunctionId = f;
                        bestResult.BestParams = bestParameters;
                        results.Add(bestResult);
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
            await Task.WhenAll(solvingTasks);
            return results;
        }

        private Dictionary<int, Type> GetSelectedFunctionsType(List<FitnessFunction> selectedFunctions, string functionPath, FileLoader fileLoader)
        {
            Dictionary<int, Type> functionTypes = new();
            foreach (var function in selectedFunctions)
            {
                FileLoader loader = new(functionPath + function.FileName);
                fileLoader.Load();
                var fTypes = fileLoader.file.GetTypes();
                var searchType = fTypes.FirstOrDefault(type => type.GetInterfaces().Any(interfaceType =>
                    ReflectionValidator.ImplementsInterface(interfaceType, typeof(IFitnessFunction)) && type.IsClass && type.Name == function.Name));
                if (searchType != null)
                {
                    functionTypes[function.Id] = searchType;
                }
                else
                {
                    throw new NotFoundException($"Nie znaleziono funkcji {function.Name} w pliku {function.FileName}.");
                }
            }
            return functionTypes;
        }

        private Dictionary<int, Type> GetSelectedFunctionsType(List<FitnessFunction> selectedFunctions, string functionPath)
        {
            Dictionary<int, Type> functionTypes = new();
            foreach (var function in selectedFunctions)
            {
                FileLoader loader = new(functionPath + function.FileName);
                loader.Load();
                var fTypes = loader.file.GetTypes();
                var searchType = fTypes.FirstOrDefault(type => type.GetInterfaces().Any(interfaceType =>
                    ReflectionValidator.ImplementsInterface(interfaceType, typeof(IFitnessFunction)) && type.IsClass && type.Name == function.Name));
                if (searchType != null)
                {
                    functionTypes[function.Id] = searchType;
                }
                else
                {
                    throw new NotFoundException($"Nie znaleziono funkcji {function.Name} w pliku {function.FileName}.");
                }
            }
            return functionTypes;
        }

        #endregion

        #region Testing FitnessFunction
        async public Task<List<ResultsDto>> TestFitnessFunction(int id, int[] algorithmIds, CancellationToken cancellationToken, int sessionId = 0, bool resume = false)
        {
            var selectedAlgorithms = dbContext.Algorithms.Where(a => algorithmIds.Contains(a.Id)).ToList();
            var fitnessFunction = dbContext.FitnessFunctions.First(f => f.Id == id);
            string algorithmPath = "./dll/algorithm/";
            string functionPath = "./dll/fitnessFunction/";
            FileLoader fileLoader = new(functionPath + fitnessFunction.FileName);
            fileLoader.Load();
            var aTypes = fileLoader.file.GetTypes();
            var functionType = aTypes.FirstOrDefault(type =>
                type.GetInterfaces().Any(interfaceType =>
                ReflectionValidator.ImplementsInterface(interfaceType, typeof(IFitnessFunction))
                ));
            Dictionary<int, Type> algorithmTypes = GetSelectedAlgorithmsType(selectedAlgorithms, algorithmPath);

            if (functionType != null)
            {
                dynamic functionInstance = Activator.CreateInstance(functionType);
              
                Sessions session;
                if (!resume)
                {
                    session = new() { AlgorithmIds = String.Join(";", algorithmIds), FitnessFunctionIds = id.ToString(), State = "RUNNING" };
                    dbContext.Sessions.Add(session);
                    dbContext.SaveChanges();
                    sessionId = session.Id;
                }
                else
                {
                    session = dbContext.Sessions.FirstOrDefault(s => s.Id == sessionId);
                }
                List<ResultsDto> results = await PrepareDataAndInvokeTests(functionInstance, algorithmTypes, fitnessFunction, session.Id, id, cancellationToken, resume);
                foreach(var result in results)
                {
                    result.FitnessFunctionId = id;
                }
                session.State = "FINISHED";
                dbContext.SaveChanges();
                return results;
            }
            else
            {
                throw new NotImplementInterfaceException("Zawartość pliku nie implementuje wymaganego interfejsu.");
            }
        }

        async private Task<List<ResultsDto>> PrepareDataAndInvokeTests(dynamic functionInstance, Dictionary<int, Type> algorithmTypes, FitnessFunction fitnessFunction, int sessionId, int fitnessFunctionId, CancellationToken cancellationToken, bool resume)
        {
            List<ResultsDto> results = new List<ResultsDto>();
            int availableProcessors = Environment.ProcessorCount;
            int maxParallelTasks = availableProcessors > 3 ? availableProcessors - 2 : 1;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelTasks);
            List<Task> solvingTasks = new List<Task>();
            foreach (var a in algorithmTypes.Keys)
            {

                await semaphore.WaitAsync();
                if (cancellationToken.IsCancellationRequested) return null;
                Tests tests = new() { SessionId = sessionId, AlgorithmId = a, FitnessFunctionId = fitnessFunctionId, Progress = 0 };
                dbContext.Tests.Add(tests);
                dbContext.SaveChanges();
                solvingTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        dynamic optimizationAlgorithm = Activator.CreateInstance(algorithmTypes[a]);
                        dynamic paramsData = optimizationAlgorithm.ParamsInfo;
                        int dimension = -1;
                        for (int i = 0; i < paramsData.Length; i++)
                        {
                            if (paramsData[i].Name == "Dimension")
                            {
                                dimension = (int)paramsData[i].LowerBoundary;
                                break;
                            }
                        }
                        if (dimension == -1) throw new NotFoundException("Brak parametru Dimension w ParamsInfo");
                        double[,] domainArray = GetFunctionDomain(fitnessFunction, dimension);
                        Dictionary<string, double> bestParameters = await InvokeTest(optimizationAlgorithm, domainArray, functionInstance, tests, sessionId, a, fitnessFunctionId, cancellationToken, resume);
                        ResultsDto bestResult = new ResultsDto();
                        bestResult.AlgorithmId = a;
                        bestResult.BestParams = bestParameters;
                        results.Add(bestResult);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            await Task.WhenAll(solvingTasks);
            return results;
        }
        private Dictionary<int, Type> GetSelectedAlgorithmsType(List<Algorithm> selectedAlgorithms, string algorithmPath)
        {
            Dictionary<int, Type> algorithmTypes = new();
            foreach (var algorithm in selectedAlgorithms)
            {
                FileLoader loader = new(algorithmPath + algorithm.FileName);
                loader.Load();
                var aTypes = loader.file.GetTypes();
                var searchType = aTypes.FirstOrDefault(type => type.GetInterfaces().Any(interfaceType =>
                    ReflectionValidator.ImplementsInterface(interfaceType, typeof(IOptimizationAlgorithm)) && type.IsClass));
                if (searchType != null)
                {
                    algorithmTypes[algorithm.Id] = searchType;
                }
                else
                {
                    throw new NotFoundException($"Nie znaleziono funkcji {algorithm.Name} w pliku {algorithm.FileName}.");
                }
            }
            return algorithmTypes;
        }

        #endregion
        private Dictionary<string, double> InvokeTest(dynamic optimizationAlgorithm, double[,] domainArray, dynamic fitnessFunction, Tests tests, int sessionId, int algorithmId, int functionId, CancellationToken cancellationToken, bool resume)
        {
            dynamic paramsData = optimizationAlgorithm.ParamsInfo;
            double[] paramsValue = InitializeAlgorithmParams(paramsData, sessionId, algorithmId, functionId, resume);
            int iteration = 1;
            List<AlgorithmBestParameters> bestParameters = new List<AlgorithmBestParameters>();
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) break;
                List<AlgorithmBestParameters> bestIter = new();
                for (int iter = 0; iter < 10; iter++)
                {
                    optimizationAlgorithm.Solve(fitnessFunction, domainArray, paramsValue, resume);
                    AlgorithmBestParameters iterParams = new(optimizationAlgorithm.XBest, optimizationAlgorithm.FBest, paramsValue);
                    bestIter.Add(iterParams);
                }
                AlgorithmBestParameters currentParams = bestIter.OrderBy(param => param.FBest).First();
                bestParameters.Add(currentParams);
                double progress = iteration / Math.Pow(5, paramsValue.Length);
                tests.Progress = progress;
                dbContext.SaveChanges();
                iteration++;
                paramsValue = IncreaseParams(paramsValue, paramsData);
                if (paramsValue[0] > paramsData[0].UpperBoundary)
                {
                    break;
                }
            }
            Dictionary<string, double> bestResult = ChooseBestResult(bestParameters, paramsData);

            return bestResult;
        }
        private Dictionary<string, double> ChooseBestResult(List<AlgorithmBestParameters> bestParameters, dynamic paramsData)
        {
            Dictionary<string, double> bestResult = new Dictionary<string, double>();
            double[] bestParams = new double[paramsData.Length];

            for(int i = 0; i < paramsData.Length; i++)
            {
                bestResult[paramsData[i].Name] = bestParams[i];
            }
            return bestResult;
        }

        private double[] IncreaseParams(double[] paramsValue, dynamic paramsData)
        {
            for(int i = paramsValue.Length-1 ; i >= 0; i--)
            {
                paramsValue[i] += (paramsData[i].UpperBoundary + paramsData[i].LowerBoundary) / 5;
                if (paramsValue[i] < paramsData[i].UpperBoundary) break;
                else
                {
                    paramsValue[i] = paramsData[i].LowerBoundary;
                }
            }

            return paramsValue;
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
        private double[,] GetFunctionDomain(FitnessFunction function, int dimension)
        {
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
        private double[] InitializeAlgorithmParams(dynamic paramsData, int sessionId, int algorithmId, int functionId, bool resume)
        {
            double[] paramsValue = new double[paramsData.Length];
            if(resume)
            {
                var testId = dbContext.Tests.FirstOrDefault(t => t.SessionId == sessionId && t.AlgorithmId == algorithmId && t.FitnessFunctionId == functionId).Id;
                var parameters = dbContext.TestResults.LastOrDefault(t => t.TestId == testId).Parameters.Split(";");
                paramsValue = parameters.Select(double.Parse).ToArray();
            }
            else
            {
                for (int i = 0; i < paramsData.Length; i++)
                {
                    paramsValue[i] = paramsData[i].LowerBoundary;
                }
                
            }
            return paramsValue;
        }

        async public Task<List<ResultsDto>> ResumeSession(int sessionId, CancellationToken cancellationToken)
        {
            List<ResultsDto> results;
            var session = dbContext.Sessions.FirstOrDefault(s => s.Id == sessionId);
            string[] algorithmIds = session.AlgorithmIds.Split(";");
            string[] functionIds = session.FitnessFunctionIds.Split(";");
            if (algorithmIds.Length == 1 )
            {
                int algorithmid = int.Parse(algorithmIds[0]);
                int[] fitnessFunctionIds = functionIds.Select(int.Parse).ToArray();
                results = await TestAlgorithm(algorithmid, fitnessFunctionIds, cancellationToken, sessionId, true);
            }
            else
            {
                int fitnessFunctionId = int.Parse(functionIds[0]);
                int[] algortihms = algorithmIds.Select(int.Parse).ToArray();
                results = await TestFitnessFunction(fitnessFunctionId, algortihms, cancellationToken, sessionId, true);
            }
            return results;
        }

        public List<TestsDto> GetCurrentProgress()
        {
            int currentSessionId = dbContext.Sessions.LastOrDefault(s => s.State == "RUNNING").Id;
            List<TestsDto> progressList = dbContext.Tests
                .Where(t => t.SessionId == currentSessionId)
                .Select(t => mapper.Map<TestsDto>(t))
                .ToList();
            return progressList;
        }
    }
}