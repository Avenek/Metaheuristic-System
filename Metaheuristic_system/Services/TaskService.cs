using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using Metaheuristic_system.Reflection;
using Metaheuristic_system.ReflectionRequiredInterfaces;
using Metaheuristic_system.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
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
        private readonly DbContextFactory dbContextFactory;
        private readonly IMapper mapper;

        public TaskService(SystemDbContext dbContext, IMapper mapper, DbContextFactory dbContextFactory)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
            this.dbContextFactory = dbContextFactory;
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
                Sessions session;
                var runningSessions = dbContext.Sessions
                    .Where(s => s.State == "RUNNING")
                    .ToList();
                foreach (var s in runningSessions)
                {
                    s.State = "SUSPENDED";
                }
                if (!resume)
                {

                    session = new() { AlgorithmIds = id.ToString(), FitnessFunctionIds = String.Join(";", fitnessFunctionIds), State = "RUNNING" };
                    dbContext.Sessions.Add(session);
                }
                else
                {
                    session = dbContext.Sessions.FirstOrDefault(s => s.Id == sessionId);
                    session.State = "RUNNING";
                }
                dbContext.SaveChanges();
                                    sessionId = session.Id;
                List<ResultsDto> results = await PrepareDataAndInvokeTests(optimizationType, functionTypes, selectedFunctions, sessionId, id, cancellationToken, resume);
                foreach (var result in results)
                {
                    result.AlgorithmId = id;
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    session.State = "SUSPENDED";
                }
                else
                {
                    session.State = "FINISHED";
                }
                dbContext.SaveChanges();
                return results;
            }
            else
            {
                throw new NotImplementInterfaceException("Zawartość pliku nie implementuje wymaganego interfejsu.");
            }
        }

        async private Task<List<ResultsDto>> PrepareDataAndInvokeTests(Type algorithmType, Dictionary<int, Type> functionTypes, List<FitnessFunction> selectedFunctions, int sessionId, int algorithmId, CancellationToken cancellationToken, bool resume)
        {
            List<ResultsDto> results = new List<ResultsDto>();
            int availableProcessors = Environment.ProcessorCount;
            int maxParallelTasks = availableProcessors > 3 ? availableProcessors - 2 : 1;
            SemaphoreSlim semaphore = new SemaphoreSlim(maxParallelTasks);
            List<Task> solvingTasks = new List<Task>();
            foreach (var f in functionTypes.Keys)
            {
                await semaphore.WaitAsync();
                if (cancellationToken.IsCancellationRequested) return null;

                solvingTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using (var dbContext = dbContextFactory.CreateDbContext())
                        {
                            dynamic optimizationAlgorithm = Activator.CreateInstance(algorithmType);
                            Tests tests = new() { SessionId = sessionId, AlgorithmId = algorithmId, FitnessFunctionId = f, Progress = 0 };
                            await dbContext.Tests.AddAsync(tests);
                            await dbContext.SaveChangesAsync();
                            if (cancellationToken.IsCancellationRequested) return;
                            tests.FitnessFunctionId = f;
                            dynamic paramsData = optimizationAlgorithm.ParamsInfo;
                            dynamic fitnessFunction = Activator.CreateInstance(functionTypes[f]);
                            var function = dbContext.FitnessFunctions.FirstOrDefault(function => function.Id == f);
                            ResultsDto bestResult = await InvokeTest(optimizationAlgorithm, fitnessFunction, tests, sessionId, algorithmId, function, cancellationToken, resume, dbContext);
                            bestResult.SessionId = sessionId;
                            bestResult.FitnessFunctionId = f;
                            results.Add(bestResult);
                        }
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
                List<ResultsDto> results = await PrepareDataAndInvokeTests(functionType, algorithmTypes, fitnessFunction, session.Id, id, cancellationToken, resume);
                foreach(var result in results)
                {
                    result.FitnessFunctionId = id;
                }
                session.State = cancellationToken.IsCancellationRequested ? "SUSPENDED" : "FINISHED";
                dbContext.SaveChanges();
                return results;
            }
            else
            {
                throw new NotImplementInterfaceException("Zawartość pliku nie implementuje wymaganego interfejsu.");
            }
        }

        async private Task<List<ResultsDto>> PrepareDataAndInvokeTests(Type functionType, Dictionary<int, Type> algorithmTypes, FitnessFunction fitnessFunction, int sessionId, int fitnessFunctionId, CancellationToken cancellationToken, bool resume)
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
                        using (var dbContext = dbContextFactory.CreateDbContext())
                        {
                            dynamic functionInstance = Activator.CreateInstance(functionType);
                            if (cancellationToken.IsCancellationRequested) return;
                            dynamic optimizationAlgorithm = Activator.CreateInstance(algorithmTypes[a]);
                            dynamic paramsData = optimizationAlgorithm.ParamsInfo;
                            ResultsDto bestResult = await InvokeTest(optimizationAlgorithm, functionInstance, tests, sessionId, a, fitnessFunction, cancellationToken, resume, dbContext);
                            bestResult.SessionId = sessionId;
                            bestResult.AlgorithmId = a;
                            results.Add(bestResult);
                        }
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
        private ResultsDto InvokeTest(dynamic optimizationAlgorithm, dynamic fitnessFunction, Tests tests, int sessionId, int algorithmId, FitnessFunction function, CancellationToken cancellationToken, bool resume, SystemDbContext dbContext)
        {
            dynamic paramsData = optimizationAlgorithm.ParamsInfo;
            int? prepareDimension = function.Dimension;
            int dimensionIndex = -1;
            if (prepareDimension is null)
            {
                for (int i = 0; i < paramsData.Length; i++)
                {
                    if (paramsData[i].Name == "Dimension")
                    {
                        paramsData[i].LowerBoundary = 2;
                        paramsData[i].UpperBoundary = 27;
                        dimensionIndex = i;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < paramsData.Length; i++)
                {
                    if (paramsData[i].Name == "Dimension")
                    {
                        paramsData[i].LowerBoundary = (int)prepareDimension;
                        paramsData[i].UpperBoundary = (int)prepareDimension;
                        dimensionIndex = i;
                        break;
                    }
                }
            }
            double[] paramsValue = InitializeAlgorithmParams(paramsData, sessionId, algorithmId, function.Id, resume);
            int iteration = 1;
            List<AlgorithmBestParameters> bestParameters = new List<AlgorithmBestParameters>();

            if (dimensionIndex == -1) throw new NotFoundException("Brak parametru Dimension w ParamsInfo");
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) break;
                List<AlgorithmBestParameters> bestIter = new();
                
                double[,] domainArray = GetFunctionDomain(function, (int)paramsValue[dimensionIndex]);
                for (int iter = 0; iter < 10; iter++)
                {
                    optimizationAlgorithm.Solve(fitnessFunction, domainArray, paramsValue, resume);
                    AlgorithmBestParameters iterParams = new(optimizationAlgorithm.XBest, optimizationAlgorithm.FBest, paramsValue);
                    bestIter.Add(iterParams);
                }
                AlgorithmBestParameters currentParams = bestIter.OrderBy(param => param.FBest).First();
                bestParameters.Add(currentParams);
                Dictionary<string, double> resultsDict = new Dictionary<string, double>();
                for (int i = 0; i < paramsData.Length; i++)
                {
                    resultsDict[paramsData[i].Name] = currentParams.BestParams[i];
                }
                TestResults testResults = new() { TestId = tests.Id, XBest = String.Join(';', optimizationAlgorithm.XBest), FBest = optimizationAlgorithm.FBest, Parameters = JsonConvert.SerializeObject(resultsDict) };
                dbContext.TestResults.Add(testResults);
                double progress = iteration / Math.Pow(5, paramsValue.Length);
                tests.Progress = progress;
                dbContext.SaveChanges();
                iteration++;
                paramsValue = IncreaseParams(paramsValue, paramsData, dimensionIndex);
                if (paramsValue[0] > paramsData[0].UpperBoundary)
                {
                    break;
                }
            }
            ResultsDto bestResult = ChooseBestResult(bestParameters, paramsData);

            return bestResult;
        }
        private ResultsDto ChooseBestResult(List<AlgorithmBestParameters> bestParameters, dynamic paramsData)
        {
            Dictionary<string, double> bestResult = new Dictionary<string, double>();
            double[] bestParams = new double[paramsData.Length];
            AlgorithmBestParameters algorithmBestParameters = bestParameters.OrderBy(test => test.FBest).First();
            bestParams = algorithmBestParameters.BestParams;
            for(int i = 0; i < paramsData.Length; i++)
            {
                bestResult[paramsData[i].Name] = bestParams[i];
            }
            ResultsDto bestResults = new ResultsDto() { BestParams = bestResult, FBest = algorithmBestParameters.FBest, XBest = algorithmBestParameters.XBest };
            return bestResults;
        }

        private double[] IncreaseParams(double[] paramsValue, dynamic paramsData, int dimensionIndex)
        {
            for(int i = paramsValue.Length-1 ; i >= 0; i--)
            {
                if(i == dimensionIndex)
                {
                    paramsValue[i] += (int)(paramsData[i].UpperBoundary - paramsData[i].LowerBoundary) / 5;
                    if (paramsValue[i] < paramsData[i].UpperBoundary) break;
                    else
                    {
                        paramsValue[i] = paramsData[i].LowerBoundary;
                    }
                }
                else
                {
                    paramsValue[i] += (paramsData[i].UpperBoundary - paramsData[i].LowerBoundary) / 5;
                    if (paramsValue[i] < paramsData[i].UpperBoundary) break;
                    else
                    {
                        paramsValue[i] = paramsData[i].LowerBoundary;
                    }
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
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, double>>(dbContext.TestResults.LastOrDefault(t => t.TestId == testId).Parameters);
                paramsValue = parameters.Values.ToArray();
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
            int currentSessionId = dbContext.Sessions.FirstOrDefault(s => s.State == "RUNNING").Id;
            List<TestsDto> progressList = dbContext.Tests
                .Where(t => t.SessionId == currentSessionId)
                .Select(t => mapper.Map<TestsDto>(t))
                .ToList();
            return progressList;
            
        }
    }
}