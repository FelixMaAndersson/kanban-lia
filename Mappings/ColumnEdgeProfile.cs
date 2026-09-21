using AutoMapper;
using kanban_lia.Endpoints.Columns.Requests;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Mappings
{
    public class ColumnEdgeProfile : Profile
    {
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
