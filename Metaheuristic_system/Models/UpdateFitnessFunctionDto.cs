using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Models
{
    public class UpdateFitnessFunctionDto
    {
        public string Name { get; set; }
        public int? Dimension { get; set; }
        public List<List<double>> DomainArray { get; set; }
    }
}
