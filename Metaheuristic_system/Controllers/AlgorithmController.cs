using Metaheuristic_system.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Metaheuristic_system.Controllers
{
    [Route("api/algorithm")]
    public class AlgorithmController : ControllerBase
    {
        private readonly AlgorithmDbContext dbContext;

        public AlgorithmController(AlgorithmDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public ActionResult Get()
        {
            var algorithms = dbContext.Algorithms.ToList();

            return Ok(algorithms);
        }
    }
}
