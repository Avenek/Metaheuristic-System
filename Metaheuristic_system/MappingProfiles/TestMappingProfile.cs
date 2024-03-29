﻿using AutoMapper;
using Metaheuristic_system.Entities;
using Metaheuristic_system.Models;

namespace Metaheuristic_system.MappingProfiles
{
    public class TestMappingProfile : Profile
    {
        public TestMappingProfile()
        {
            CreateMap<Tests, TestsDto>()
                .ForMember(dest => dest.Progress, opt => opt.MapFrom(src => Math.Round(src.Progress, 5)));
        }
    }
}
