using Metaheuristic_system.Entities;
using Metaheuristic_system.Models;
using Metaheuristic_system.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Metaheuristic_system.Controllers
{
    [Route("file")]
    public class FileController : ControllerBase
    {

        [HttpPost]
        public ActionResult Upload([FromForm] IFormFile file)
        {
            if(file != null && file.Length > 0)
            {
                string path = "/dll";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = file.FileName;
                string fullPath = $"{path}/{fileName}";
                using(var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                return Ok();
            }
            return BadRequest();
        }
    }
}
