using AutoMapper;

using kanban_lia.Endpoints.Columns.Requests;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Mappings
{
    /// <summary>
    /// AutoMapper profile defining the mappings for column edges.
    /// </summary>
    /// <remarks>Registers the maps used by the column-edge endpoints and services so raw Guids from HTTP requests
    /// are converted into <see cref="ColumnId"/> value types and domain edges can be projected to response
    /// DTOs.</remarks>
    public class ColumnEdgeProfile : Profile
    {
        /// <summary>
        /// Registers the column-edge maps: <see cref="ColumnEdgeRequest"/> to
        /// <see cref="CreateColumnEdgeDto"/>, wrapping the request's Guid ids in <see cref="ColumnId"/>, and
        /// <see cref="ColumnEdge"/> to <see cref="ColumnEdgeDto"/>.
        /// </summary>
        public ColumnEdgeProfile()
        {
            CreateMap<ColumnEdgeRequest, CreateColumnEdgeDto>()
                .ForCtorParam(
                    nameof(CreateColumnEdgeDto.FromColumnId),
                    opt => opt.MapFrom(src => new ColumnId(src.FromColumnId)))
                .ForCtorParam(
                    nameof(CreateColumnEdgeDto.ToColumnId),
                    opt => opt.MapFrom(src => new ColumnId(src.ToColumnId)));

            CreateMap<ColumnEdge, ColumnEdgeDto>();

        }
    }
}
