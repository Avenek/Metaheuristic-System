using Metaheuristic_system.Services;
using Microsoft.AspNetCore.Mvc;

namespace Metaheuristic_system.Controllers
{
    [Route("api/pausedSession")]
    [ApiController]
    public class PausedSessionController : ControllerBase
    {
        private readonly IPausedSessionService pausedSessionService;

        public PausedSessionController(IPausedSessionService pausedSessionService)
        {
            this.pausedSessionService = pausedSessionService;
        }

        [HttpGet]
        public ActionResult GetAll()
        {
            var pausedSessions = pausedSessionService.GetAll();
            return Ok(pausedSessions);
        }

        [HttpDelete]
        public ActionResult DeleteRunningSessionById([FromRoute] int id)
        {
            pausedSessionService.DeleteRunningSessionById(id);
            return NoContent();
        }
    }
}
