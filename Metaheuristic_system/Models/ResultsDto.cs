namespace Metaheuristic_system.Models
{
    public class ResultsDto
    {
        public int Id { get; set; }
        public int AlgorithmId { get; set; }
        public int FitnessFunctionId { get; set; }
        Dictionary<string, dynamic> BestParams { get; set; }
    }
}
