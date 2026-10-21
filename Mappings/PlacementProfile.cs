using AutoMapper;

using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Mappings
{
    /// <summary>
    /// AutoMapper Profile that configures mappings between placement-related requests, domain models, and their DTO
    /// representations.
    /// </summary>
    /// <remarks>Configures CreateMap<CreatePlacementRequest, CreatePlacementDto> with constructor-parameter
    /// mappings for ColumnId, BoardId, EntityIds, AfterEntityIds, and BeforeEntityIds; configures
    /// CreateMap<GetPlacementRequest, GetPlacementDto> mapping EntityIds and BoardId; and configures
    /// CreateMap<Placement, PlacementDto>.</remarks>
    public class PlacementProfile : Profile
    {
        /// <summary>
        /// Configures the placement-related mappings registered by this profile.
        /// </summary>
        /// <remarks>Registers request-to-DTO mappings that wrap raw Guids in the domain identifier types
        /// (<see cref="EntityId"/>, <see cref="BoardId"/>, <see cref="ColumnId"/>) through constructor-parameter
        /// mappings, plus a straight projection from the <see cref="Placement"/> domain model to
        /// <see cref="PlacementDto"/>.</remarks>
        public PlacementProfile()
        {
            CreateMap<CreatePlacementRequest, CreatePlacementDto>()
                .ForCtorParam(
                    nameof(CreatePlacementDto.ColumnId),
                    opt => opt.MapFrom(src => new ColumnId(src.ColumnId)))
                .ForCtorParam(
                    nameof(CreatePlacementDto.BoardId),
                    opt => opt.MapFrom(src => new BoardId(src.BoardId)))
                .ForCtorParam(
                    nameof(CreatePlacementDto.EntityIds),
                    opt => opt.MapFrom(src =>
                        src.EntityIds
                            .Select(id => new EntityId(id))
                            .ToArray()))
                .ForCtorParam(
                    nameof(CreatePlacementDto.AfterEntityIds),
                    opt => opt.MapFrom(src =>
                        src.AfterEntityIds
                            .Select(id => new EntityId(id))
                            .ToArray()))
                .ForCtorParam(
                    nameof(CreatePlacementDto.BeforeEntityIds),
                    opt => opt.MapFrom(src =>
                        src.BeforeEntityIds
                            .Select(id => new EntityId(id))
                            .ToArray()));

            CreateMap<GetPlacementRequest, GetPlacementDto>()
                .ForCtorParam(
                    nameof(GetPlacementDto.EntityIds),
                    opt => opt.MapFrom(src => src.EntityIds.Select(id => new EntityId(id))))
                .ForCtorParam(
                    nameof(GetPlacementDto.BoardId),
                    opt => opt.MapFrom(src => new BoardId(src.BoardId)));

            CreateMap<Placement, PlacementDto>();
        }
    }
}