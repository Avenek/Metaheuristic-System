using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Models
{
    public class TestsDto
    {
        public int AlgorithmId { get; set; }
        public int FitnessFunctionId { get; set; }
        public double Progress { get; set; }
    }
}
