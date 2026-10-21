using AutoMapper;

using kanban_lia.Endpoints.Columns.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Mappings
{
    /// <summary>
    /// AutoMapper profile defining mappings for column requests, DTOs and domain objects.
    /// </summary>
    /// <remarks>Maps CreateColumnRequest onto CreateColumnDto (leaving the column ID null so the domain layer
    /// generates one) and RenameColumnRequest onto RenameColumnDto (wrapping the raw ID in a ColumnId), plus the
    /// domain Column onto its ColumnDto projection.</remarks>
    public class ColumnProfile : Profile
    {
        /// <summary>
        /// Creates the profile and registers the column mapping configuration.
        /// </summary>
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