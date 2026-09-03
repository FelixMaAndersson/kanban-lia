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
                    nameof(CreateColumnDto.Id),
                    opt => opt.MapFrom(_ => (ColumnId?)null))
                .ForCtorParam(
                    nameof(CreateColumnDto.BoardId),
                    opt => opt.MapFrom(src => new BoardId(src.BoardId)));
            CreateMap<RenameColumnRequest, RenameColumnDto>()
                .ForCtorParam(
                    nameof(RenameColumnDto.Id),
                    opt => opt.MapFrom(src => new ColumnId(src.Id)));

            CreateMap<Column, ColumnDto>();
        }
    }
}