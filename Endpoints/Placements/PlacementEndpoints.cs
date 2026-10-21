using AutoMapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using kanban_lia.Endpoints.Placements.Requests;
using kanban_lia.Hubs;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Models.Events;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.IntegrationEvents.Models;
using kanban_lia.Services.Placements;
using kanban_lia.Services.Placements.DTOs;

namespace kanban_lia.Endpoints.Placements
{

    public static class PlacementEndpoints
    {
        /// <summary>
        /// Maps placement-related HTTP endpoints under /api/placements.
        /// </summary>
        /// <remarks>Registers endpoints for creating and retrieving placements: POST /create, GET /get, GET
        /// /board/{boardId}, and GET /column/{columnId}. The POST endpoint validates column write access, resolves
        /// connected source and target columns across boards, computes placement operations, invokes IPlacementService to
        /// create placements, and broadcasts a PlacementCreated event via SignalR. Correlation-Id, Causation-Id, Actor-Id,
        /// and Actor-Type headers are read when present and used for causation/correlation and actor information.</remarks>
        /// <param name="app">The WebApplication on which placement endpoints are configured.</param>
        /// <exception cref="ColumnNotFoundException">Thrown when a required column referenced during endpoint processing cannot be found in the repository.</exception>
        public static void MapPlacementEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/placements");
            var logger = app.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("PlacementEndpoints");

            group.MapPost("/create", async (
                HttpRequest httpRequest,
                [FromBody] CreatePlacementRequest request,
                IPlacementService placementService,
                CancellationToken cancellationToken,
                IColumnRepository columnRepository,
                IColumnEdgeRepository columnEdgeRepository,
                IHubContext<BoardHub> hub) =>
            {
                var mappedRequest = await MapRequest(request, columnEdgeRepository);

                var mappedPlacementResult = await BuildPlacementContextAsync(
                    mappedRequest,
                    placementService,
                    columnRepository,
                    columnEdgeRepository);

                var (columnPairs, affectedBoardIds) = await SelectColumnPairsAsync(
                    mappedRequest,
                    mappedPlacementResult,
                    placementService);

                var placementPlan = await BuildPlacementOperations(
                    mappedRequest,
                    new ColumnPairSelection(columnPairs, affectedBoardIds),
                    columnRepository);

                var changes = CreatePlacementChanges(columnPairs);

                var (correlationId, causationEventId, actor) = GetEventMetadata(httpRequest);

                var result = await placementService.CreateAsync(
                    placementPlan.Operations,
                    correlationId,
                    causationEventId,
                    actor,
                    cancellationToken,
                    mappedRequest.BoardId);

                var failureResponse = LogPlacementFailures(result, logger);

                if (failureResponse is not null)
                {
                    return failureResponse;
                }

                var succeededEntityIds = result.SucceededEntityIds
                    .Select(entityId => entityId.Id)
                    .ToArray();

                await BroadcastPlacementCreatedAsync(
                     hub,
                     placementPlan.AffectedBoardIds,
                     succeededEntityIds,
                     changes,
                     actor,
                     logger,
                     cancellationToken);

                return Results.Ok(result);
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

                var placements = await placementService.GetCurrentAsync(dto);

                return Results.Ok(placements);
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

            return [.. connectedColumns.Distinct()];
        }


        private static Column SelectTargetColumn(
        IEnumerable<Column> targetGroup,
        List<Column> matchingSources,
        Column? currentSource)
        {
            var targets = targetGroup.ToList();

            if (matchingSources.Count == 0)
            {
                return targets
                    .OrderBy(target => target.Position)
                    .First();
            }

            if (targets.Count == 1)
            {
                return targets[0];
            }

            var sourcePosition = currentSource is not null
                ? currentSource.Position
                : matchingSources
                    .OrderBy(source => source.Position)
                    .First()
                    .Position;

            return targets
                .OrderBy(target =>
                    Math.Abs(target.Position - sourcePosition))
                .First();
        }

        private static List<PlacementChange> CreatePlacementChanges(
            List<(ColumnId Target, List<ColumnId> Sources)> columnPairs)
        {
            var changes = new List<PlacementChange>();
            foreach (var (Target, Sources) in columnPairs)
            {
                if (Sources.Count == 0)
                {
                    changes.Add(new PlacementChange(null, Target.Id));
                    continue;
                }

                foreach (var source in Sources)
                {
                    changes.Add(new PlacementChange(
                        source.Id,
                        Target.Id
                        ));
                }
            }
            return changes;
        }

        private static (
            Guid CorrelationId,
            Guid? CausationEventId,
            Actor Actor)
            GetEventMetadata(HttpRequest httpRequest)
        {
            Guid correlationId = Guid.NewGuid();
            Guid? causationEventId = null;
            var actorId = "-1";
            var actorType = "";

            if (httpRequest.Headers.TryGetValue(
                    "Correlation-Id",
                    out var correlationHeader) &&
                Guid.TryParse(correlationHeader, out var parsedCorrelationId))
            {
                correlationId = parsedCorrelationId;
            }

            if (httpRequest.Headers.TryGetValue(
                    "Causation-Id",
                    out var causationHeader) &&
                Guid.TryParse(causationHeader, out var parsedCausationEventId))
            {
                causationEventId = parsedCausationEventId;
            }

            if (httpRequest.Headers.TryGetValue("Actor-Id", out var actorIdHeader))
            {
                actorId = actorIdHeader.ToString();
            }

            if (httpRequest.Headers.TryGetValue("Actor-Type", out var actorTypeHeader))
            {
                actorType = actorTypeHeader.ToString();
            }

            var actor = new Actor(actorId, actorType);

            return (correlationId, causationEventId, actor);
        }

        private static async Task<PlacementMapped> MapRequest(
            [FromBody] CreatePlacementRequest request,
            IColumnEdgeRepository columnEdgeRepository)
        {
            var entityIds = (request.EntityIds ?? [])
                    .Select(id => new EntityId(id))
                    .ToList();
            var afterEntityIds = request.AfterEntityIds ?? [];
            var beforeEntityIds = request.BeforeEntityIds ?? [];

            var boardId = new BoardId(request.BoardId);

            var connectedTargetColumnIds = await GetConnectedColumns(
                new ColumnId(request.ColumnId),
                columnEdgeRepository);

            return new PlacementMapped(
                entityIds,
                afterEntityIds,
                beforeEntityIds,
                boardId,
                connectedTargetColumnIds
            );
        }

        private static async Task<MappedPlacementResult> BuildPlacementContextAsync(
            PlacementMapped mappedRequest,
            IPlacementService placementService,
            IColumnRepository columnRepository,
            IColumnEdgeRepository columnEdgeRepository)
        {
            var connectedTargetColumns = new List<Column>();

            foreach (var columnId in mappedRequest.ConnectedTargetColumnIds)
            {
                var column =
                    await columnRepository.GetByIdAsync(columnId) ?? throw new ColumnNotFoundException(columnId);
                connectedTargetColumns.Add(column);
            }

            var currentPlacements = await placementService.GetCurrentAsync(
                new GetPlacementDto(
                    mappedRequest.EntityIds,
                    mappedRequest.BoardId
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
            return new MappedPlacementResult(connectedTargetColumns, sourceColumns);
        }

        private static async Task<ColumnPairSelection> SelectColumnPairsAsync(
        PlacementMapped mappedRequest,
        MappedPlacementResult mappedPlacementResult,
        IPlacementService placementService)
        {
            var targetGroups = mappedPlacementResult.ConnectedTargetColumns
                .GroupBy(column => column.BoardId);

            var affectedBoardIds = targetGroups
                .Select(group => group.Key.Id)
                .ToList();

            var columnPairs =
                new List<(ColumnId Target, List<ColumnId> Sources)>();

            foreach (var targetGroup in targetGroups)
            {
                var matchingSources = mappedPlacementResult.SourceColumns
                    .Where(source => source.BoardId == targetGroup.Key)
                    .ToList();

                var currentPlacementsOnTargetBoard =
                    await placementService.GetCurrentAsync(
                        new GetPlacementDto(
                            mappedRequest.EntityIds,
                            targetGroup.Key));

                var currentSource = matchingSources.FirstOrDefault(source =>
                    currentPlacementsOnTargetBoard.Any(
                        placement => placement.ColumnId == source.Id));

                var targetColumn = SelectTargetColumn(
                    targetGroup,
                    matchingSources,
                    currentSource);

                columnPairs.Add((
                    targetColumn.Id,
                    matchingSources.Select(source => source.Id).ToList()));
            }

            return new ColumnPairSelection(columnPairs, affectedBoardIds);
        }

        private static async Task<PlacementPlan> BuildPlacementOperations(
            PlacementMapped mappedRequest,
            ColumnPairSelection selection,
            IColumnRepository columnRepository)
        {
            var operations =
        new List<PlacementOperationDto>();

            foreach (var (targetId, sourceIds) in selection.ColumnPairs)
            {
                var targetColumn = await columnRepository.GetByIdAsync(targetId)
                    ?? throw new ColumnNotFoundException(targetId);

                var createDto = new CreatePlacementDto(
                   [.. mappedRequest.EntityIds],
                   targetColumn.BoardId,
                   targetId,
                   mappedRequest.AfterEntityIds.Select(id => new EntityId(id)),
                   mappedRequest.BeforeEntityIds.Select(id => new EntityId(id)));

                operations.Add(new PlacementOperationDto(createDto, sourceIds));

            }
            return new PlacementPlan(operations, selection.AffectedBoardIds);
        }

        private static IResult? LogPlacementFailures(PlacementCreateResult result, ILogger logger)
        {

            foreach (var failure in result.Failures)
            {
                logger.LogWarning(
                    "Placement failed for entity {EntityId}: {Error}",
                    failure.EntityId.Id,
                    failure.Error);
            }

            if (result.SucceededEntityIds.Count == 0 && result.Failures.Count > 0)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "No placements were created",
                    detail: string.Join("; ", result.Failures.Select(failure => failure.Error)));
            }
            return null;
        }

        private static async Task BroadcastPlacementCreatedAsync(
        IHubContext<BoardHub> hub,
        IEnumerable<Guid> affectedBoardIds,
        Guid[] succeededEntityIds,
        List<PlacementChange> changes,
        Actor actor,
        ILogger logger,
        CancellationToken cancellationToken)
        {
            try
            {
                var placementCreatedEvent = new PlacementCreatedEvent(
                    succeededEntityIds,
                    changes,
                    actor);

                foreach (var boardId in affectedBoardIds)
                {
                    await hub.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(
                            "PlacementCreated",
                            placementCreatedEvent,
                            cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(
                    ex,
                    "PlacementCreated broadcast failed after commit for {EntityCount} entities",
                    succeededEntityIds.Length);
            }
        }

        private sealed record PlacementPlan(
        List<PlacementOperationDto> Operations,
        List<Guid> AffectedBoardIds);

        public record ColumnPairSelection(
        List<(ColumnId Target, List<ColumnId> Sources)> ColumnPairs,
        List<Guid> AffectedBoardIds);

        public record MappedPlacementResult(
            List<Column> ConnectedTargetColumns,
            List<Column> SourceColumns);

        public record PlacementMapped(
            List<EntityId> EntityIds,
            Guid[] AfterEntityIds,
            Guid[] BeforeEntityIds,
            BoardId BoardId,
            List<ColumnId> ConnectedTargetColumnIds
        );
    }
}