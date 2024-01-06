using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Models
{
    public class AlgorithmDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Removeable { get; set; }
    }
}
