using AutoMapper;
using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Domain.Placements.DTOs;
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

            // Hello David, first we get the entity ids that we need
            var entityIds = request.EntityIds
                .Select(id => new EntityId(id))
                .ToList();

            // then the board Id
            var boardId = new BoardId(request.BoardId);

            // then all the IDs for all the columns we have edges to.
            var connectedTargetColumnIds = await GetConnectedColumns(
                new ColumnId(request.ColumnId),
                columnEdgeRepository);


            // the we gets the column objects by looping over those IDs
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

            // the enitityIds gives us all the current placements.
            var currentPlacements = await placementService.GetCurrentAsync(
                new GetPlacementDto(
                    entityIds,
                    boardId
                )
            );

            // we use the current placements to find all source columnsIds, inclouding all connected columns on other boards.
            var sourceColumnIds = new List<ColumnId>();

            foreach (var placement in currentPlacements)
            {
                var connectedSourceColumns = await GetConnectedColumns(
                    placement.ColumnId,
                    columnEdgeRepository);

                sourceColumnIds.AddRange(connectedSourceColumns);
            }

            //you know this one
            sourceColumnIds = [.. sourceColumnIds.Distinct()];

            // we get the source column objects the same way we got the target columns.
            var sourceColumns = new List<Column>();

            foreach (var sourceColumnId in sourceColumnIds)
            {
                var column = await columnRepository.GetByIdAsync(sourceColumnId);
                if (column is not null)
                {
                    sourceColumns.Add(column);
                }
            }

            //a board kan have several posible targets. we group by boardId so we can choose a taget per board
            var targetGroups = connectedTargetColumns.GroupBy(column => column.BoardId);

            // this is old news; we need to have them in pairs with their target and all their possible sources.
            var columnPairs = new List<(ColumnId Target, List<ColumnId> Sources)>();

            // this is why we needed to deconstruct all the loops and what nots. We want to place a placement
            // to the left when comming from the left (inbox -> todo = inbox -> todo) and to the right
            // when comming from the right (done -> todo = done -> doing)
            foreach (var targetGroup in targetGroups)
            {

                // we start of by finding the sources on the board we want to create our column pairs.
                var matchingSources = sourceColumns
                    .Where(source => source.BoardId == targetGroup.Key)
                    .ToList();

                Column targetColumn;

                // if the group only has 1 target in it, it means that there is a 1-1 relationship and we can just
                // place our placement in the column with the edge (eg.g. 'done' in our case).
                if (targetGroup.Count() == 1)
                {
                    targetColumn = targetGroup.First();
                }

                // if that's not the case we need to take charge and do something! We start by ordering from left to right
                // (0,1,2,3,4,5 = 0 the left most and 5 the right most) 
                else
                {
                    var sourcePosition = matchingSources
                        .OrderBy(source => source.Position)
                        .First()
                        .Position;

                    // this is the most confusing bit of code, where we calculates which target is the closest to our source.
                    // that's why we ca use the same for both from left and from right, since we just place our placement in the 
                    // target nearest our source.
                    targetColumn = targetGroup
                        .OrderBy(target => Math.Abs(target.Position - sourcePosition))
                        .First();
                }
                columnPairs.Add((
                    Target: targetColumn.Id,
                    Sources: [.. matchingSources.Select(source => source.Id)]
                        ));
            }

            // the rest of the code is basically the same, but a bit more
            // convoluted since we now have even more nested lists.
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
                    request.AfterEntityId?[],
                    request.BeforeEntityId?[]
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