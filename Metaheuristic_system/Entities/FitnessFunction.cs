using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metaheuristic_system.Entities
{
    public class FitnessFunction
    { 
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
        [Required]
        [MaxLength(50)]
        public string FileName { get; set; }
        public int? Dimension { get; set; }
        public string Domain { get; set; }

        [NotMapped]
        public List<List<double>> DomainArray { get; set; }
        public bool Removeable { get; set; }
    }
}
