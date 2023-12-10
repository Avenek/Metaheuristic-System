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

        [HttpGet("{id}")]
        public async Task<ActionResult> GetResultsOfTestingAlgorithm([FromRoute] int id, [FromBody] int[] fitnessFunctionIds, CancellationToken cancellationToken)
        {
            var results = await taskService.GetResultsOfTestingAlgorithm(id, fitnessFunctionIds, cancellationToken);
            return Ok(results);
        }

    }
}
