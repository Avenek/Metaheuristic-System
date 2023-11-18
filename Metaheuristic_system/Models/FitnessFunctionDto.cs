using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Metaheuristic_system.Models
{
    public class FitnessFunctionDto
    {
        [Required]
        [MaxLength(30)]
        public string Name { get; set; }
        [Required]
        [MaxLength(30)]
        public string FileName { get; set; }
        public int Dimension { get; set; }
        public double[,] DomainArray { get; set; }
    }
}
