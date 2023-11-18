using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Models
{
    public class AlgorithmDto
    {
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
        [Required]
        [MaxLength(30)]
        public string FileName { get; set; }
    }
}
