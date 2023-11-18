using System.ComponentModel.DataAnnotations.Schema;

namespace Metaheuristic_system.Entities
{
    public class FitnessFunction
    { 
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public int Dimension { get; set; }
        [NotMapped]
        public double[,] Domain { get; set; }
        public bool Removeable { get; set; }
    }
}
