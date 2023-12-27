namespace Metaheuristic_system.Models
{
    public class ResultsDto
    {
        public int SessionId { get; set; }
        public int AlgorithmId { get; set; }
        public int FitnessFunctionId { get; set; }
        public Dictionary<string, double> BestParams { get; set; }

        public double FBest { get; set; }
        public double[] XBest {  get; set; }
    }
}
