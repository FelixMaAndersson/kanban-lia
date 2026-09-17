using AutoMapper;
using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
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
            IColumnEdgeRepository columnEdgeRepository,
            IHubContext<BoardHub> hub) =>
        {
            var entityIds = request.EntityIds
                .Select(id => new EntityId(id))
                .ToList();

            var boardId = new BoardId(request.BoardId);

            var columnsToPlaceIn = await GetConnectedColumns(
                new ColumnId(request.ColumnId),
                columnEdgeRepository);

            var currentPlacements = await placementService.GetCurrentAsync(
                new GetPlacementDto(
                    entityIds,
                    boardId
                )
            );
   
            var currentSourceColumns = currentPlacements
                .Select(p => p.ColumnId)
                .Distinct()
                .ToList();


            var sourceColumns = new List<ColumnId>();

            foreach (var sourceColumn in currentSourceColumns)
            {
                var connectedSourceColumns = await GetConnectedColumns(
                    sourceColumn,
                    columnEdgeRepository);

                    sourceColumns.AddRange(connectedSourceColumns);

            }

            var placementOperations = new List<PlacementOperationDto>();


            for (var i = 0; i < columnsToPlaceIn.Count; i++)
            {
                var createDto = new CreatePlacementDto(
                    entityIds,
                    boardId,
                    columnsToPlaceIn[i],
                    request.AfterEntityId,
                    request.BeforeEntityId
                );

                var operationDto = new PlacementOperationDto(
                    createDto,
                    sourceColumns[i]
                );

                placementOperations.Add(operationDto);

            }

            await placementService.CreateAsync(placementOperations);

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
    private static async Task<List<ColumnId>> GetConnectedColumns(
    ColumnId columnId,
    IColumnEdgeRepository columnEdgeRepository)
    {
        var fromEdges =
            await columnEdgeRepository.GetByFromColumnIdAsync(columnId);

        var toEdges =
            await columnEdgeRepository.GetByToColumnIdAsync(columnId);

        var connectedColumns = new List<ColumnId>
    {
        columnId
    };

        connectedColumns.AddRange(
            fromEdges.Select(edge => edge.ToColumnId));

        connectedColumns.AddRange(
            toEdges.Select(edge => edge.FromColumnId));

        return connectedColumns.Distinct().ToList();
    }
}