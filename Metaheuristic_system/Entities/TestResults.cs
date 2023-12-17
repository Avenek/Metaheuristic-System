using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Entities
{
    public class TestResults
    {
        public int Id { get; set; }
        [Required]
        public int TestId { get; set; }
        [Required]
        public int AlgorithmId { get; set; }
        [Required]
        public int FitnessFunctionId { get; set; }
        public string XBest{ get; set; }
        public double FBest { get; set; }
        [Required]
        public string Parameters { get; set; }
    }
}
