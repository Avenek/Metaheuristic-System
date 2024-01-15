namespace Metaheuristic_system.Models
{
    public class SessionDto
    {
        public int SessionId { get; set; }
        public string State { get; set; }
        public bool IsAlgorithmTested { get; set; }
        public List<string> Algorithms { get; set; }
        public List<string> FitnessFunctions { get; set; }
    }
}
