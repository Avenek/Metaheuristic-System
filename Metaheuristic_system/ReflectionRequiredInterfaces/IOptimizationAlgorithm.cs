public delegate double fitnessFunction(params double[] arg);

namespace Metaheuristic_system.ReflectionRequiredInterfaces
{

    public interface IOptimizationAlgorithm
    {
        string Name { get; set; }

        void Solve(fitnessFunction f, double[,] domain, params double[] parameters);

        ParamInfo[] ParamsInfo { get; set; }

        IStateWriter Writer { get; set; }

        IStateReader Reader { get; set; }
        IGenerateTextReport StringReportGenerator { get; set; }
        IGeneratePDFReport PdfReportGenerator { get; set; }

        double[] XBest { get; set; }
        double FBest { get; set; }
        int NumberOfEvaluationFitnessFunction { get; set; }

    }
}