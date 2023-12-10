using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Metaheuristic_system.MappingProfiles
{
    public class FitnessFunctionMappingProfile : Profile
    {
        public FitnessFunctionMappingProfile()
        {
            CreateMap<FitnessFunction, FitnessFunctionDto>()
                .ForMember(dest => dest.DomainArray, opt => opt.MapFrom(src => MapDomain(src.Domain)));

            CreateMap<FitnessFunctionDto, FitnessFunction>()
                .ForMember(dest => dest.Domain, opt => opt.MapFrom(src => MapDomainArray(src.DomainArray)));

        }

        private List<List<double>> MapDomain(string domain)
        {
            return JsonConvert.DeserializeObject<List<List<double>>>(domain);
        }

        private string MapDomainArray(List<List<double>> domain)
        {
            return JsonConvert.SerializeObject(domain);
        }
    }
}
