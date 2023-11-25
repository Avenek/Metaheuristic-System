using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Models;

namespace Metaheuristic_system.MappingProfiles
{
    public class AlgorithmMappingProfile : Profile
    {
        public AlgorithmMappingProfile()
        {
            CreateMap<Algorithm, AlgorithmDto>();
        }
    }
}
