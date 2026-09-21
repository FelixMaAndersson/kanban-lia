using AutoMapper;
using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Events;
using kanban_lia.Services.Columns.Exceptions;
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
    IColumnRepository columnRepository,
    IColumnEdgeRepository columnEdgeRepository,
    IHubContext<BoardHub> hub) =>
        {
            var entityIds = request.EntityIds
                .Select(id => new EntityId(id))
                .ToList();

            var boardId = new BoardId(request.BoardId);

            var connectedTargetColumnIds = await GetConnectedColumns(
                new ColumnId(request.ColumnId),
                columnEdgeRepository);

            var connectedTargetColumns = new List<Column>();

            foreach (var columnId in connectedTargetColumnIds)
            {
                var column =
                    await columnRepository.GetByIdAsync(columnId);

                if (column is null)
                {
                    throw new ColumnNotFoundException(columnId);
                }

                connectedTargetColumns.Add(column);

            }

            var currentPlacements = await placementService.GetCurrentAsync(
                new GetPlacementDto(
                    entityIds,
                    boardId
                )
            );

            var sourceColumnIds = new List<ColumnId>();

            foreach (var placement in currentPlacements)
            {
                var connectedSourceColumns = await GetConnectedColumns(
                    placement.ColumnId,
                    columnEdgeRepository);

                sourceColumnIds.AddRange(connectedSourceColumns);
            }

            sourceColumnIds = [.. sourceColumnIds.Distinct()];

            var sourceColumns = new List<Column>();

            foreach (var sourceColumnId in sourceColumnIds)
            {
                var column = await columnRepository.GetByIdAsync(sourceColumnId);
                if (column is not null)
                {
                    sourceColumns.Add(column);
                }
            }

            var targetGroups = connectedTargetColumns.GroupBy(column => column.BoardId);

            var columnPairs = new List<(ColumnId Target, List<ColumnId> Sources)>();

            foreach (var targetGroup in targetGroups)
            {

                var matchingSources = sourceColumns
                    .Where(source => source.BoardId == targetGroup.Key)
                    .ToList();

                Column targetColumn;

                if (targetGroup.Count() == 1)
                {
                    targetColumn = targetGroup.First();
                }

                else
                {
                    var sourcePosition = matchingSources
                        .OrderBy(source => source.Position)
                        .First()
                        .Position;

                    targetColumn = targetGroup
                        .OrderBy(target => Math.Abs(target.Position - sourcePosition))
                        .First();
                }
                columnPairs.Add((
                    Target: targetColumn.Id,
                    Sources: [.. matchingSources.Select(source => source.Id)]
                        ));
            }

            var placementOperations =
                new List<PlacementOperationDto>();

            foreach (var pair in columnPairs)
            {
                var targetColumn =
                    await columnRepository.GetByIdAsync(pair.Target);

                if (targetColumn is null)
                {
                    throw new ColumnNotFoundException(pair.Target);
                }

                var createDto = new CreatePlacementDto(
                    entityIds,
                    targetColumn.BoardId,
                    pair.Target,
                    request.AfterEntityIds.Select(id => new EntityId(id)),
                    request.BeforeEntityIds.Select(id => new EntityId(id))
                );

                var operationDto = new PlacementOperationDto(
                    createDto,
                    pair.Sources
                );

                placementOperations.Add(operationDto);
            }

            var changes = new List<PlacementChange>();

            foreach (var pair in columnPairs)
            {
                if (pair.Sources.Count == 0)
                {
                    changes.Add(new PlacementChange(null, pair.Target.Id));
                    continue;
                }

                foreach (var source in pair.Sources)
                {
                    changes.Add(new PlacementChange(
                        source.Id,
                        pair.Target.Id
                        ));
                }
            }

            await placementService.CreateAsync(placementOperations);

            await hub.Clients.All.SendAsync(
                "PlacementCreated",
                new PlacementCreatedEvent(
                    request.EntityIds,
                    changes
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