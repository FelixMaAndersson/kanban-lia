using AutoMapper;

using kanban_lia.Endpoints.Columns.Requests;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Mappings
{
    public class ColumnProfile : Profile
    {
        public ColumnProfile()
        {
            CreateMap<CreateColumnRequest, CreateColumnDto>();
            CreateMap<RenameColumnRequest, RenameColumnDto>();

            CreateMap<Column, ColumnDto>()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.Id.Value));
        }
    }
}