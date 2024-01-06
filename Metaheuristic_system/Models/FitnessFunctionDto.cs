using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Models
{
    public class FitnessFunctionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FileName { get; set; }
        public int? Dimension { get; set; }
        public List<List<double>> DomainArray { get; set; }
        public bool Removeable { get; set; }
    }
}

