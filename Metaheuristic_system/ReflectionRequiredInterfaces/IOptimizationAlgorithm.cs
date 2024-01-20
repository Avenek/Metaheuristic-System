namespace Metaheuristic_system.ReflectionRequiredInterfaces
{

    public interface IOptimizationAlgorithm
    {
        string Name { get; set; }

        void Solve(dynamic f, double[,] domain, double[] parameters, bool resume);

        ParamInfo[] ParamsInfo { get; set; }

        double[] XBest { get; set; }
        double FBest { get; set; }
        int NumberOfEvaluationFitnessFunction { get; set; }

    }
}