using AutoMapper;

using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Services;
using kanban_lia.Services.DTOs;

namespace kanban_lia.Endpoints.Placements;

public static class PlacementEndpoints
{
    public static void MapPlacementEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/placements");

        group.MapPost("/create", async (
            CreatePlacementRequest request,
            IPlacementService placementService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<CreatePlacementDto>(request);

            var newPlacement = await placementService.CreateAsync(requestDto);

            return Results.Created($"/api/placements/create", newPlacement);
        });
    }
}