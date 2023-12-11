namespace Metaheuristic_system.Models
{
    public class ResultsDto
    {
        public int Id { get; set; }
        public int AlgorithmId { get; set; }
        public int FitnessFunctionId { get; set; }
        public Dictionary<string, double> BestParams { get; set; }
    }
}
