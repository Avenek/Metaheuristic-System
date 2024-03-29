﻿namespace Metaheuristic_system.Models
{
    public class AlgorithmBestParameters
    {
        public double[] XBest { get; set; }
        public double FBest { get; set; }
        public double[] BestParams { get; set; }
        public int NumberOfEvaluationFitnessFunction { get; set; }

       public AlgorithmBestParameters(double[] xBest, double fBest, double[] bestParams, int numberOfEvaluationFitnessFunction)
        {
            XBest = xBest;
            FBest = fBest;
            BestParams = bestParams;
            NumberOfEvaluationFitnessFunction = numberOfEvaluationFitnessFunction;
        }
    }
}
