using AutoMapper;

using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Mappings
{
    public class PlacementProfile : Profile
    {
        public PlacementProfile()
        {
            CreateMap<CreatePlacementRequest, CreatePlacementDto>()
                .ForCtorParam(
                    nameof(CreatePlacementDto.ColumnId),
                    opt => opt.MapFrom(src => new ColumnId(src.ColumnId)))
                .ForCtorParam(
                    nameof(CreatePlacementDto.BoardId),
                    opt => opt.MapFrom(src => new BoardId(src.BoardId)));

            CreateMap<GetPlacementRequest, GetPlacementDto>()
                .ForCtorParam(
                    nameof(GetPlacementDto.EntityId),
                    opt => opt.MapFrom(src => new EntityId(src.EntityId)))
                .ForCtorParam(
                    nameof(GetPlacementDto.BoardId),
                    opt => opt.MapFrom(src => new BoardId(src.BoardId)));

            CreateMap<Placement, PlacementDto>();
        }
    }
}