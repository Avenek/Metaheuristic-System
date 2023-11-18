using Metaheuristic_system.Entities;
using Metaheuristic_system.Services;
using Microsoft.AspNetCore.Mvc;

namespace Metaheuristic_system.Controllers
{
    [Route("api/algorithm")]
    [ApiController]
    public class AlgorithmController : ControllerBase
    {
        private readonly IAlgorithmService algorithmService;

        public AlgorithmController(IAlgorithmService algorithmService)
        {
            this.algorithmService = algorithmService;
        }

        [HttpGet]
        public ActionResult Get()
        {
            return Ok();
        }
    }
}