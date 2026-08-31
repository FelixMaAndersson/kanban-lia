using AutoMapper;
using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements.DTOs;
using kanban_lia.Services.Placements;
using kanban_lia.Services.Placements.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace kanban_lia.Endpoints.Placements;

public static class PlacementEndpoints
{
    public static void MapPlacementEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/placements");

        group.MapPost("/create", async (
            [FromBody] CreatePlacementRequest request,
            IPlacementService placementService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<CreatePlacementDto>(request);

            await placementService.CreateAsync(requestDto);

            return Results.Ok();
        });

        group.MapGet("/get", async (
            Guid entityId,
            HashSet<Guid> columnIds,
            IPlacementService placementService,
            IMapper mapper) =>
        {
            var placement = await placementService.GetCurrentAsyncByColumn(entityId, columnIds.Select(id => new ColumnId(id)).ToHashSet());

            return placement is null
                ? Results.NotFound()
                : Results.Ok(mapper.Map<PlacementDto>(placement));
        });
    }
}