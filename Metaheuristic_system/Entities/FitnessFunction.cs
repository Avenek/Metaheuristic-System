using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace Metaheuristic_system.Entities
{
    public class FitnessFunction
    { 
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public int Dimension { get; set; }
        public string Domain
        {
            get => JsonConvert.SerializeObject(DomainArray);
            set => DomainArray = JsonConvert.DeserializeObject<double[,]>(value);
        }
        [NotMapped]
        public double[,] DomainArray { get; set; }
        public bool Removeable { get; set; }
    }
}
