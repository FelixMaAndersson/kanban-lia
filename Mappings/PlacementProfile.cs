using AutoMapper;

using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Mappings
{
    public class PlacementProfile : Profile
    {
        public PlacementProfile()
        {
            CreateMap<CreatePlacementRequest, CreatePlacementDto>();

            CreateMap<Placement, PlacementDto>();
        }
    }
}