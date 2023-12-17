using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Entities
{
    public class Tests
    {
        public int Id { get; set; }
        [Required]
        public int SessionId { get; set; }
        [Required]
        public int AlgorithmId { get; set; }
        [Required]
        public int FitnessFunctionId { get; set; }
        [Required]
        public double Progress { get; set; }
    }
}
