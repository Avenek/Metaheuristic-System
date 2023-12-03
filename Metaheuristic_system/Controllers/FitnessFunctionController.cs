using Metaheuristic_system.Models;
using Metaheuristic_system.Services;
using Microsoft.AspNetCore.Mvc;

namespace Metaheuristic_system.Controllers
{
    
    [Route("api/fitnessFunction")]
    [ApiController]
    public class FitnessFunctionController : ControllerBase
    {
        private readonly IFitnessFunctionService fitnessFunctionService;

        public FitnessFunctionController(IFitnessFunctionService fitnessFunctionService)
        {
            this.fitnessFunctionService = fitnessFunctionService;
        }

        [HttpGet]
        public ActionResult GetAll()
        {
            var fitnessFunctions = fitnessFunctionService.GetAll();
            return Ok(fitnessFunctions);
        }

        [HttpGet("{id}")]
        public ActionResult GetById([FromRoute] int id)
        {
            var fitnessFunction = fitnessFunctionService.GetById(id);
            return Ok(fitnessFunction);
        }

        [HttpPatch("{id}")]
        public ActionResult UpdateNameById([FromRoute] int id, [FromQuery] string newName)
        {
            fitnessFunctionService.UpdateNameById(id, newName);
            return Ok();
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteById([FromRoute] int id)
        {
            fitnessFunctionService.DeleteById(id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        public ActionResult UpdateDomainAndDimensionById([FromRoute] int id, [FromBody] DimensionAndDomainDto functionParams)
        {
            fitnessFunctionService.UpdateDomainAndDimensionById(id, functionParams);
            return Ok();
        }

        [HttpPost]
        public ActionResult UploadFitnessFunctionFile([FromForm] IFormFile file)
        {
            fitnessFunctionService.UploadFitnessFunctionFile(file);
            return Ok();
        }

        public ActionResult AddFitnessFunction([FromBody] FitnessFunctionDto newFitnessFunctionDto)
        {
            fitnessFunctionService.AddFitnessFunction(newFitnessFunctionDto);
            return Ok();
        }
    }
}

