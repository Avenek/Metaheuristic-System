﻿using System.ComponentModel.DataAnnotations;

namespace Metaheuristic_system.Entities
{
    public class TestResults
    {
        public int Id { get; set; }
        [Required]
        public int TestId { get; set; }
        public string XBest{ get; set; }
        public double FBest { get; set; }
        public int NumberOfEvaluationFitnessFunction { get; set; }
        [Required]
        public string Parameters { get; set; }

        public virtual Tests Test { get; set; }
    }
}
