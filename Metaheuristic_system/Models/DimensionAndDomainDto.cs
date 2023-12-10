using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Models
{
    public class DimensionAndDomainDto
    {
        public int? Dimension { get; set; }
        public List<List<double>> DomainArray { get; set; }
    }
}
