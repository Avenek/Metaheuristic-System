namespace Metaheuristic_system.Models
{
    public class SessionDto
    {
        public int sessionId { get; set; }
        public string state { get; set; }
        public List<TestsDto> tests { get; set; }
    }
}
