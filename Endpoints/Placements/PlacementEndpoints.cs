using AutoMapper;
using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Hubs;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Events;
using kanban_lia.Services.Placements;
using kanban_lia.Services.Placements.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace kanban_lia.Endpoints.Placements;

public static class PlacementEndpoints
{
    public static void MapPlacementEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/placements");

        group.MapPost("/create", async (
            [FromBody] CreatePlacementRequest request,
            IPlacementService placementService,
            IMapper mapper,
            IHubContext<BoardHub> hub) =>
        {
            var requestDto = mapper.Map<CreatePlacementDto>(request);

            await placementService.CreateAsync(requestDto);

            await hub.Clients.All.SendAsync(
                "PlacementCreated",
                new PlacementCreatedEvent(
                    request.EntityIds,
                    request.SourceColumnId,
                    request.ColumnId
                )
            );

            return Results.Ok();
        });

        group.MapGet("/get", async (
            [FromQuery] Guid[] entityIds,
            [FromQuery] Guid boardId,
            IPlacementService placementService,
            IMapper mapper) =>
        {
            var request = new GetPlacementRequest(
                entityIds,
                boardId);

            var dto = mapper.Map<GetPlacementDto>(request);

            var placement = await placementService.GetCurrentAsync(dto);

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

        group.MapGet("/column/{columnId:guid}", async (
            Guid columnId,
            Guid boardId,
            IPlacementService placementService) =>
        {
            var placements = await placementService.GetCurrentByColumnAsync(
                new ColumnId(columnId),
                new BoardId(boardId));

            return Results.Ok(placements);
        });
    }
}