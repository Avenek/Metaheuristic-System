using Metaheuristic_system.Models;
using Metaheuristic_system.Services;
using Microsoft.AspNetCore.Mvc;

namespace Metaheuristic_system.Controllers
{
    [Route("api/task")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly ITaskService taskService;

        public TaskController(ITaskService taskService)
        {
            this.taskService = taskService;
        }

        [HttpPost("algorithm/{id}")]
        public async Task<IActionResult> TestAlgorithm([FromRoute] int id, [FromBody] int[] fitnessFunctionIds, CancellationToken cancellationToken)
        {
            var results = await taskService.TestAlgorithm(id, fitnessFunctionIds, cancellationToken);

            return Ok(results);
        }

        [HttpPost("fitnessFunction/{id}")]
        public async Task<IActionResult> TestFitnessFunction([FromRoute] int id, [FromBody] int[] algorithmIds, CancellationToken cancellationToken)
        {
            var results = await taskService.TestFitnessFunction(id, algorithmIds, cancellationToken);

            return Ok(results);
        }
        [HttpPost("{id}")]
        public async Task<IActionResult> ResumeSession([FromRoute] int id, [FromQuery] bool resume, CancellationToken cancellationToken)
        {
            if (resume)
            {
                var results = await taskService.ResumeSession(id, cancellationToken);
                return Ok(results);
            }
            else
            {
                return BadRequest("Wymagane jest ustawienie parametru resume na true.");
            }
        }

        [HttpGet]
        public ActionResult<List<TestsDto>> GetCurrentProgress()
        {
            var progressList = taskService.GetCurrentProgress();
            return Ok(progressList);
        }

    }
}
