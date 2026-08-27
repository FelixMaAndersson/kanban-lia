using AutoMapper;

using kanban_lia.Endpoints.Boards.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Boards.DTOs;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Mappings
{
    public class BoardProfile : Profile
    {
        public BoardProfile()
        {
            CreateMap<RenameBoardRequest, RenameBoardDto>();
            CreateMap<AddRootRequest, AddRootDto>();
            CreateMap<RemoveRootRequest, RemoveRootDto>();

            CreateMap<Board, BoardDto>()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.Id.Value));
        }
    }
}