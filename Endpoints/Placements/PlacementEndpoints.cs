using AutoMapper;
using Microsoft.AspNetCore.Mvc;

using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Services.Placements;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Endpoints.Placements;

public static class PlacementEndpoints
{
    public static void MapPlacementEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/placements");

        group.MapPost("/create", async (
            [FromBody]CreatePlacementRequest request,
            IPlacementService placementService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<CreatePlacementDto>(request);

            var newPlacement = await placementService.CreateAsync(requestDto);

            return Results.Created($"/api/placements/create", newPlacement);
        });
    }
}