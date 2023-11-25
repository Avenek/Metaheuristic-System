using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Entities
{
    public class Algorithm
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
        [Required]
        [MaxLength(30)]
        public string FileName { get; set; }
        public bool Removeable { get; set; }
    }
}
