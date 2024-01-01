using AutoMapper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;



namespace Metaheuristic_system.Services
{
    public interface ISessionService
    {
        IEnumerable<SessionDto> GetAll();
        IEnumerable<SessionDto> GetAllByState(string state);
        void DeleteSessionById(int id);
        string GeneratePdf(int id);

    }

    public class SessionService : ISessionService
    {
        private readonly SystemDbContext dbContext;
        private readonly IMapper mapper;

        public SessionService(SystemDbContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }
        public IEnumerable<SessionDto> GetAll()
        {
            var sessions = dbContext.Sessions.ToList();
            var sessionDtos = new List<SessionDto>();
            foreach (var session in sessions)
            {
                var testsForSession = dbContext.Tests
                    .Where(t => t.SessionId == session.Id)
                    .Select(t => new TestsDto
                    {
                        AlgorithmId = t.AlgorithmId,
                        FitnessFunctionId = t.FitnessFunctionId,
                        Progress = t.Progress
                    })
                    .ToList();

                var sessionDto = new SessionDto
                {
                    SessionId = session.Id,
                    Tests = testsForSession,
                    State = session.State,
                };

                sessionDtos.Add(sessionDto);
            }

            return sessionDtos;
        }

        public IEnumerable<SessionDto> GetAllByState(string state)
        {
            string[] states = new string[] { "RUNNING, SUSPENDED, FINISHED" };
            if (!states.Contains(state)) throw new InvalidArgumentException("Podano niedozwolony typ.");
            var sessions = dbContext.Sessions.Where(s => s.State == state).ToList();
            var sessionDtos = new List<SessionDto>();
            foreach (var session in sessions)
            {
                // Pobierz powiązane testy dla sesji
                var testsForSession = dbContext.Tests
                    .Where(t => t.SessionId == session.Id)
                    .Select(t => new TestsDto
                    {
                        AlgorithmId = t.AlgorithmId,
                        FitnessFunctionId = t.FitnessFunctionId,
                        Progress = t.Progress
                    })
                    .ToList();

                var pausedSessionDto = new SessionDto
                {
                    SessionId = session.Id,
                    Tests = testsForSession,
                    State = session.State,
                };

                sessionDtos.Add(pausedSessionDto);
            }

            return sessionDtos;
        }

        public void DeleteSessionById(int id)
        {
            var session = dbContext.Sessions.FirstOrDefault(s => s.Id == id);
            if(session is null)
            {
                throw new NotFoundException($"Nie odnaleziono sesji o id {id}.");
            }
            dbContext.Sessions.Remove(session);
            dbContext.SaveChanges();
        }

        public string GeneratePdf(int id)
        {

            var session = dbContext.Sessions.Include(s => s.Tests).ThenInclude(t => t.Results).FirstOrDefault(s => s.Id == id);
            if (session is null)
            {
                throw new NotFoundException($"Nie odnaleziono sesji o id {id}.");
            }
            //if(session.State != "FINISHED")
            //{
            //    throw new InvalidArgumentException($"Sesja o id {id} nie została zakończona.");
            //}
            var tests = session.Tests;
            var directoryPath = "./wwwroot";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var pdfFilePath = $"{directoryPath}/{id}.pdf";
            var fullFilePath = Path.GetFullPath(pdfFilePath);
            if (File.Exists(fullFilePath))
            {
                return $"/{id}.pdf";
            }
            using (var stream = new MemoryStream())
            {
                var document = new Document();
                PdfWriter.GetInstance(document, new FileStream(fullFilePath, FileMode.Create));
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"a+rw {fullFilePath}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();
                document.Open();
                document.Add(new Paragraph($"Wyniki testu dla sesji: {id}"));
                document.Add(new Paragraph(" "));
                var algorithms = session.AlgorithmIds.Split(";");
                var functions = session.FitnessFunctionIds.Split(";");
                bool isAlgorithmTested = algorithms.Length == 1;
                if (isAlgorithmTested)
                {
                    var algorithm = dbContext.Algorithms.FirstOrDefault(a => a.Id == int.Parse(algorithms[0]));
                    document.Add(new Paragraph($"Testowany algorytm: {algorithm.Name}"));
                    var functionNames = dbContext.FitnessFunctions.Where(f => functions.Contains(f.Id.ToString())).Select(f => f.Name).ToList();
                    document.Add(new Paragraph($"Na podstawie funkcji testowych: {String.Join(", ", functionNames)}"));
                    document.Add(new Paragraph(" "));
                }
                else
                {
                    var function = dbContext.FitnessFunctions.FirstOrDefault(f => f.Id == int.Parse(functions[0]));
                    document.Add(new Paragraph($"Testowana funkcja: {function.Name}"));
                    if (function.Dimension is not null)
                    {
                        document.Add(new Paragraph($"Testowany wymiar funkcji: {function.Dimension}"));
                    }
                    else
                    {
                        document.Add(new Paragraph("Testowane wymiary funkcji: 2, 7, 12, 17, 22, 27"));
                    }
                    var algorithmNames = dbContext.Algorithms.Where(a => algorithms.Contains(a.Id.ToString())).Select(a => a.Name).ToList();
                    document.Add(new Paragraph($"Na podstawie algorytmów: {String.Join(", ", algorithmNames)}"));
                    document.Add(new Paragraph(" "));
                }
                foreach (var test in tests)
                {
                    var function = dbContext.FitnessFunctions.FirstOrDefault(f => f.Id == test.FitnessFunctionId);
                    if (isAlgorithmTested)
                    {
                        document.Add(new Paragraph($"Test na funkcji testowej {function.Name}"));
                    }
                    else
                    {
                        var algorithm = dbContext.Algorithms.FirstOrDefault(a => a.Id == test.AlgorithmId);
                        document.Add(new Paragraph($"Test na algorytmie: {algorithm.Name}"));
                    }
                    document.Add(new Paragraph(" "));
                    List<int> desiredDimensions = new List<int>();

                    if (function.Dimension is not null)
                    {
                        desiredDimensions.Add(function.Dimension.Value);
                    }
                    else
                    {
                        int[] dimensions = new int[] { 2, 7, 12, 17, 22, 27 };
                        desiredDimensions.AddRange(dimensions);
                    }
                    foreach (var dimension in desiredDimensions)
                    {
                        var topResultsForDimension = test.Results
                            .Where(result => JsonConvert.DeserializeObject<Dictionary<string, double>>(result.Parameters)["Dimension"] == dimension)
                            .OrderBy(result => result.FBest)
                            .Take(5)
                            .ToList();

                        foreach (var result in topResultsForDimension)
                        {
                            var parameters = JsonConvert.DeserializeObject<Dictionary<string, double>>(result.Parameters);
                            PdfPTable table = new PdfPTable(parameters.Keys.Count);
                            foreach (var key in parameters.Keys)
                            {
                                table.AddCell(key);
                            }
                            document.Add(new Paragraph(" "));
                            foreach (var value in parameters.Values)
                            {
                                table.AddCell(value.ToString());
                            }
                            document.Add(table);
                            document.Add(new Paragraph(" "));

                            table = new PdfPTable(2);
                            table.AddCell("XBest");
                            table.AddCell("FBest");

                            string[] xBests = result.XBest.ToString().Split(';');
                            table.AddCell(String.Join(";\n", xBests));
                            table.AddCell(result.FBest.ToString());


                            document.Add(table);
                            document.Add(new Paragraph(" "));
                            document.Add(new Paragraph(" "));
                        }
                    }
                }
                document.Close();
            }
            return $"/{id}.pdf";
        }
    }
}
