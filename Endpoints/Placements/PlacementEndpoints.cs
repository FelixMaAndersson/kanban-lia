using AutoMapper;
using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
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
            [FromQuery] Guid entityId,
            [FromQuery] Guid boardId,
            IPlacementService placementService,
            IMapper mapper) =>
        {
            var request = new GetPlacementRequest(
                entityId,
                boardId);

            var requestDto = mapper.Map<GetPlacementDto>(request);

            var placement = await placementService.GetCurrentAsync(requestDto);

            return placement is null
                ? Results.NotFound()
                : Results.Ok(placement);
        });

        group.MapGet("/board/{boardId:guid}", async (
     Guid boardId,
     IPlacementService placementService) =>
        {
            var placements = await placementService.GetCurrentByBoardAsync(
                new BoardId(boardId));

            return Results.Ok(placements);
        });
    }
}