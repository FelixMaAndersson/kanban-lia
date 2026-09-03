using AutoMapper;

using kanban_lia.Endpoints.Boards.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Boards.DTOs;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Mappings
{
    public class BoardProfile : Profile
    {
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

            CreateMap<Board, BoardDto>();
        }
    }
}