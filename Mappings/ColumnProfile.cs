using AutoMapper;

using kanban_lia.Endpoints.Columns.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Mappings
{
    public class ColumnProfile : Profile
    {
        public ColumnProfile()
        {
            CreateMap<CreateColumnRequest, CreateColumnDto>()
                .ForCtorParam(
                    "Id",
                    opt => opt.MapFrom(_ => (ColumnId?)null))
                .ForCtorParam(
                    "BoardId",
                    opt => opt.MapFrom(src => new BoardId(src.BoardId)));
            CreateMap<RenameColumnRequest, RenameColumnDto>();

            CreateMap<Column, ColumnDto>()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.Id.Id));
        }
    }
}