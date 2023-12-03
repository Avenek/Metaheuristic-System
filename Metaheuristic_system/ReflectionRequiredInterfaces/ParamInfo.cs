namespace Metaheuristic_system.ReflectionRequiredInterfaces
{
    public class ParamInfo
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public double LowerBoundary { get; set; }
        public double UpperBoundary { get; set; }


        public ParamInfo(string name, string description, double lowerBoundary, double upperBoundary)
        {
            Name = name;
            Description = description;
            LowerBoundary = lowerBoundary;
            UpperBoundary = upperBoundary;
        }
    }
}
