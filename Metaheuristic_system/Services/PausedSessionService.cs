using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Exceptions;
using Metaheuristic_system.Models;
using Metaheuristic_system.ReflectionRequiredInterfaces;

namespace Metaheuristic_system.Services
{
    public interface IPausedSessionService
    {
        IEnumerable<PausedSessionDto> GetAll();
        void DeleteRunningSessionById(int id);
    }

    public class PausedSessionService : IPausedSessionService
    {
        private readonly SystemDbContext dbContext;
        private readonly IMapper mapper;

        public PausedSessionService(SystemDbContext dbContext, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.mapper = mapper;
        }

        public IEnumerable<PausedSessionDto> GetAll()
        {
            var pausedSessions = dbContext.Sessions.Where(s => s.State == "RUNNING").ToList();
            var pausedSessionDtos = new List<PausedSessionDto>();
            foreach (var pausedSession in pausedSessions)
            {
                // Pobierz powiązane testy dla sesji
                var testsForSession = dbContext.Tests
                    .Where(t => t.SessionId == pausedSession.Id)
                    .Select(t => new TestsDto
                    {
                        AlgorithmId = t.AlgorithmId,
                        FitnessFunctionId = t.FitnessFunctionId,
                        Progress = t.Progress
                    })
                    .ToList();

                var pausedSessionDto = new PausedSessionDto
                {
                    sessionId = pausedSession.Id,
                    tests = testsForSession
                };

                pausedSessionDtos.Add(pausedSessionDto);
            }

            return pausedSessionDtos;
        }

        public void DeleteRunningSessionById(int id)
        {
            var pausedSession = dbContext.Sessions.FirstOrDefault(s => s.Id == id);
            if(pausedSession is null)
            {
                throw new NotFoundException($"Nie odnaleziono sesji o id {id}.");
            }
            dbContext.Sessions.Remove(pausedSession);
            dbContext.SaveChanges();
        }
    }
}
