using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Models
{
    public class FitnessFunctionDto
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
        [Required]
        [MaxLength(30)]
        public string FileName { get; set; }
        [Required]
        public int? Dimension { get; set; }
        [Required]
        public List<List<double>> DomainArray { get; set; }
        public bool Removeable { get; set; }
    }
}

