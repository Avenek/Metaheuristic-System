using Metaheuristic_system.Services;
using Microsoft.AspNetCore.Mvc;

namespace Metaheuristic_system.Controllers
{
    [Route("api/session")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService sessionService;

        public SessionController(ISessionService sessionService)
        {
            this.sessionService = sessionService;
        }

        [HttpGet]
        public ActionResult GetAll()
        {
            var sessions = sessionService.GetAll();
            return Ok(sessions);
        }
        [HttpGet("{state}")]
        public ActionResult GetAllByState([FromRoute] string state)
        {
            var sessions = sessionService.GetAllByState(state);
            return Ok(sessions);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteSessionById([FromRoute] int id)
        {
            sessionService.DeleteSessionById(id);
            return NoContent();
        }
        [HttpGet("{id}/pdf")]
        public ActionResult GeneratePdf([FromRoute] int id)
        {
            string link = sessionService.GeneratePdf(id);
            return Ok(link);
        }
    }
}
