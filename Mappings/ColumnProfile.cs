using AutoMapper;
using kanban_lia.Endpoints.Column.Requests;
using kanban_lia.Models.Domain;
using kanban_lia.Models.DTOs.Column;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Mappings
{
    public class ColumnProfile : Profile
    {
        public ColumnProfile()
        {
            CreateMap<CreateColumnRequest, CreateColumnDto>();

            CreateMap<Column, ColumnDto>()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.Id.Value));
        }
    }
}