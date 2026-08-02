using AutoMapper;
using TaskBoard.Application.Features.Columns.DTOs;
using TaskBoard.Application.Features.Projects.DTOs;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            //Project
            CreateMap<Project, ProjectDto>();
            CreateMap<Project, ProjectDetailDto>()
                .ForMember(d => d.MemberCount, 
                opt => opt.MapFrom(src => src.Members.Count));

            // Columns
            CreateMap<TaskColumn, ProjectDto>();
            CreateMap<TaskColumn, ColumnDto>()
                .ForMember(dest => dest.TaskCount,
                    opt => opt.MapFrom(src => src.Tasks.Count));
        }
    }
}
