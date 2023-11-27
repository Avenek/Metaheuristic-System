using Metaheuristic_system.Entities;
using Metaheuristic_system.Models;
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
        public ActionResult GetAll()
        {
            var algorithms = algorithmService.GetAll();
            return Ok(algorithms);
        }
        [HttpGet("{id}")]
        public ActionResult GetById([FromRoute] int id) 
        { 
            var algorithm = algorithmService.GetById(id);
            return Ok(algorithm);
        }
        [HttpPatch("{id}")]
        public ActionResult UpdateNameById([FromRoute] int id, [FromQuery] string newName)
        {
            algorithmService.UpdateNameById(id, newName);
            return Ok();
        }
        [HttpDelete("{id}")]
        public ActionResult DeleteById([FromRoute] int id)
        {
            algorithmService.DeleteById(id);
            return NoContent();
        }

        [HttpPost]
        public ActionResult AddAlgorithm([FromBody] AlgorithmDto newAlgorithmDto, [FromForm] IFormFile file)
        {
            int id = algorithmService.AddAlgorithm(newAlgorithmDto, file);
            return Created($"/api/algorithm/{id}", null);
        }
    }
}