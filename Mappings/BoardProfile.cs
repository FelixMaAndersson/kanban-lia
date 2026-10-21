using AutoMapper;

using kanban_lia.Endpoints.Boards.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Boards.DTOs;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Mappings
{
    /// <summary>
    /// AutoMapper profile defining the mappings for board-related request, DTO, and domain types.
    /// </summary>
    /// <remarks>Maps endpoint request records to service DTOs, wrapping raw Guid values in the
    /// <see cref="BoardId"/> and <see cref="EntityId"/> value types, and maps the <see cref="Board"/> aggregate
    /// to its response DTO. Discovered automatically when AutoMapper scans registered profiles.</remarks>
    public class BoardProfile : Profile
    {
        /// <summary>
        /// Initialises the profile with the board request, DTO, and domain mappings.
        /// </summary>
        /// <remarks>Defines maps from <c>RenameBoardRequest</c>, <c>AddRootRequest</c>, and
        /// <c>RemoveRootRequest</c> to their service DTOs (constructing the value-type identifiers from the
        /// raw Guids), and a map from <see cref="Board"/> to <see cref="BoardDto"/> including its roots.</remarks>
        public BoardProfile()
        {
            CreateMap<RenameBoardRequest, RenameBoardDto>()
                .ForCtorParam(
                    nameof(RenameBoardDto.Id),
                    opt => opt.MapFrom(src => new BoardId(src.Id)));
            CreateMap<AddRootRequest, AddRootDto>()
                .ForCtorParam(
                    nameof(AddRootDto.BoardId),
                    opt => opt.MapFrom(src => new BoardId(src.BoardId)))
                .ForCtorParam(
                    nameof(AddRootDto.EntityId),
                    opt => opt.MapFrom(src => new EntityId(src.EntityId)));
            CreateMap<RemoveRootRequest, RemoveRootDto>()
                .ForCtorParam(
                    nameof(RemoveRootDto.BoardId),
                    opt => opt.MapFrom(src => new BoardId(src.BoardId)))
                .ForCtorParam(
                    nameof(RemoveRootDto.EntityId),
                    opt => opt.MapFrom(src => new EntityId(src.EntityId)));

            CreateMap<Board, BoardDto>()
                .ForCtorParam(
                    nameof(BoardDto.Id),
                    opt => opt.MapFrom(src => src.Id))
                .ForCtorParam(
                    nameof(BoardDto.Title),
                    opt => opt.MapFrom(src => src.Title))
                .ForCtorParam(
                    nameof(BoardDto.Roots),
                    opt => opt.MapFrom(src => src.Roots));
        }
    }
}