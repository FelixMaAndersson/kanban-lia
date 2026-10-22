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
        /// /board/{boardId}, and GET /column/{columnId}. The POST endpoint expands the requested target column to every
        /// column connected to it by a column edge, resolves the columns the entities currently occupy (again across
        /// connected columns), selects one target column per affected board, and hands the resulting operations to
        /// IPlacementService. Failures are logged and, when nothing was created, returned as a 400 problem response;
        /// otherwise a PlacementCreated event is broadcast to each affected board's SignalR group. Correlation-Id,
        /// Causation-Id, Actor-Id, and Actor-Type headers are read when present and used for causation/correlation and
        /// actor information.</remarks>
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

        /// <summary>
        /// Returns the given column plus every column directly connected to it by a column edge.
        /// </summary>
        /// <remarks>Edges are treated as undirected: columns reachable through an outgoing or an incoming edge are
        /// both included, and the input column is always part of the result. Duplicates are removed, so the result is
        /// the distinct set of columns the entity can move between.</remarks>
        /// <param name="columnId">The column to expand.</param>
        /// <param name="columnEdgeRepository">Repository used to read the edges of the column.</param>
        /// <returns>The column id itself together with the ids of all connected columns.</returns>
        private static async Task<List<ColumnId>> GetConnectedColumns(
            ColumnId columnId,
            IColumnEdgeRepository columnEdgeRepository)
        {
            var fromEdges =
                await columnEdgeRepository.GetByFromColumnIdAsync(columnId);

            var toEdges =
                await columnEdgeRepository.GetByToColumnIdAsync(columnId);

            var connectedColumns = new List<ColumnId> { columnId };

            connectedColumns.AddRange(
                fromEdges.Select(edge => edge.ToColumnId));

            connectedColumns.AddRange(
                toEdges.Select(edge => edge.FromColumnId));

            return [.. connectedColumns.Distinct()];
        }

        /// <summary>
        /// Picks the single target column a board's placements should go to.
        /// </summary>
        /// <remarks>When none of the matching source columns currently holds any of the entities, the leftmost target
        /// (lowest position) wins. When the board exposes a single connected target column, it is used as-is.
        /// Otherwise the target closest to the source column the entities are moving from is chosen, using the first
        /// matching source column's position when the entities are not currently placed on that board.</remarks>
        /// <param name="targetGroup">The connected target columns that belong to the board.</param>
        /// <param name="matchingSources">The source columns on the same board that currently hold the entities.</param>
        /// <param name="currentSource">The source column the entities are placed on for this board, if any.</param>
        /// <returns>The column that the placements for this board should target.</returns>
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

        /// <summary>
        /// Converts the selected column pairs into placement change notifications for the SignalR broadcast.
        /// </summary>
        /// <param name="columnPairs">The target column of each board paired with the source columns the entities move
        /// from on that board.</param>
        /// <returns>One change per source/target combination, or a single change without a source column when the
        /// entities are created rather than moved.</returns>
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

        /// <summary>
        /// Reads the correlation, causation and actor information from the request headers.
        /// </summary>
        /// <remarks>A new correlation id is generated when the Correlation-Id header is missing or not a valid GUID,
        /// and the causation id stays null when the Causation-Id header is absent or invalid. Actor-Id and Actor-Type
        /// default to "-1" and the empty string when the headers are not sent.</remarks>
        /// <param name="httpRequest">The incoming request whose headers are inspected.</param>
        /// <returns>The correlation id, the optional causation event id, and the actor parsed from the headers.</returns>
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

        /// <summary>
        /// Normalises the create request and expands the target column to its connected columns.
        /// </summary>
        /// <remarks>Null entity id and anchor lists are treated as empty, and the requested target column is expanded
        /// with <see cref="GetConnectedColumns"/> so the client may name any column of a connected group.</remarks>
        /// <param name="request">The create request bound from the body.</param>
        /// <param name="columnEdgeRepository">Repository used to look up the edges of the target column.</param>
        /// <returns>The mapped request: entity ids, anchors, board id and the connected target column ids.</returns>
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

        /// <summary>
        /// Loads the connected target columns and the columns the entities currently occupy.
        /// </summary>
        /// <remarks>Every connected target column must exist or a <see cref="ColumnNotFoundException"/> is thrown,
        /// while source columns that no longer exist are skipped. The entities' current placements are read through
        /// <see cref="IPlacementService"/> and each occupied column is expanded to its connected columns, so a
        /// placement can be requested from any column the entity is reachable from.</remarks>
        /// <param name="mappedRequest">The normalised request.</param>
        /// <param name="placementService">Service used to read the entities' current placements.</param>
        /// <param name="columnRepository">Repository used to load columns by id.</param>
        /// <param name="columnEdgeRepository">Repository used to expand occupied columns to their connected
        /// columns.</param>
        /// <returns>The connected target columns and the candidate source columns.</returns>
        /// <exception cref="ColumnNotFoundException">Thrown when a connected target column does not exist.</exception>
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

        /// <summary>
        /// Groups the connected target columns per board and selects one target column per board.
        /// </summary>
        /// <remarks>A request may span several boards: for every board that owns at least one connected target
        /// column, the source columns on that board that currently hold the entities are paired with a single target
        /// column chosen by <see cref="SelectTargetColumn"/>. The placements recorded on the target board are re-read
        /// to find the source column the entities are moving from.</remarks>
        /// <param name="mappedRequest">The normalised request.</param>
        /// <param name="mappedPlacementResult">The connected target columns and the candidate source columns.</param>
        /// <param name="placementService">Service used to read the entities' current placements on each target
        /// board.</param>
        /// <returns>The selected target/source column pairs and the ids of all affected boards.</returns>
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

        /// <summary>
        /// Builds one placement operation per selected column pair.
        /// </summary>
        /// <remarks>The entity ids and the optional before/after anchors from the request are copied into every
        /// <see cref="CreatePlacementDto"/>, while the target board and target column come from the resolved target
        /// column of the pair.</remarks>
        /// <param name="mappedRequest">The normalised request supplying the entity ids and anchors.</param>
        /// <param name="selection">The column pairs selected per board.</param>
        /// <param name="columnRepository">Repository used to resolve the target column of each pair.</param>
        /// <returns>The operations to hand to <see cref="IPlacementService"/> and the affected board ids.</returns>
        /// <exception cref="ColumnNotFoundException">Thrown when a selected target column no longer exists.</exception>
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

        /// <summary>
        /// Logs every placement failure and builds an error response when nothing was created.
        /// </summary>
        /// <param name="result">The outcome of <see cref="IPlacementService.CreateAsync"/>.</param>
        /// <param name="logger">Logger used for the failure warnings.</param>
        /// <returns>A 400 problem response listing the failure messages when no entity was placed and at least one
        /// failure was recorded; otherwise null so the successful result can be returned.</returns>
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

        /// <summary>
        /// Broadcasts a PlacementCreated event to every affected board's SignalR group.
        /// </summary>
        /// <remarks>The broadcast happens after the placements were committed. Failures are logged and swallowed so a
        /// broken connection cannot fail the request, while cancellation still propagates.</remarks>
        /// <param name="hub">Hub context used to reach the board groups.</param>
        /// <param name="affectedBoardIds">The boards whose groups receive the event.</param>
        /// <param name="succeededEntityIds">The ids of the entities that were placed.</param>
        /// <param name="changes">The column changes to include in the event.</param>
        /// <param name="actor">The actor that caused the placements.</param>
        /// <param name="logger">Logger used when the broadcast fails.</param>
        /// <param name="cancellationToken">Token used to cancel the send.</param>
        private static async Task BroadcastPlacementCreatedAsync(
        IHubContext<BoardHub> hub,
        IEnumerable<Guid> affectedBoardIds,
        Guid[] succeededEntityIds,
        List<PlacementChange> changes,
        Actor actor,
        CancellationToken cancellationToken)
        {
            var placementCreatedEvent = new PlacementCreatedEvent(
                succeededEntityIds,
                changes,
                actor);

            foreach (var boardId in affectedBoardIds)
            {
                try
                {
                    await hub.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(
                            "PlacementCreated",
                            placementCreatedEvent,
                            cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new InvalidOperationException(
                        $"Placements were saved, but the SignalR notification failed for board with ID: '{boardId}'.",
                    ex);
                }
            }
        }

        /// <summary>
        /// The placement operations produced for a create request, together with the boards they affect.
        /// </summary>
        /// <param name="Operations">The operations handed to <see cref="IPlacementService"/>.</param>
        /// <param name="AffectedBoardIds">The ids of the boards the operations touch.</param>
        private sealed record PlacementPlan(
        List<PlacementOperationDto> Operations,
        List<Guid> AffectedBoardIds);

        /// <summary>
        /// The target column chosen per board, paired with the source columns the entities move from on that board.
        /// </summary>
        /// <param name="ColumnPairs">One entry per affected board: the selected target column and its source
        /// columns.</param>
        /// <param name="AffectedBoardIds">The ids of the boards that own the selected target columns.</param>
        public record ColumnPairSelection(
        List<(ColumnId Target, List<ColumnId> Sources)> ColumnPairs,
        List<Guid> AffectedBoardIds);

        /// <summary>
        /// The columns loaded while mapping a create request: where the entities may go and where they come from.
        /// </summary>
        /// <param name="ConnectedTargetColumns">Every column connected to the requested target column.</param>
        /// <param name="SourceColumns">The columns that currently hold the entities, expanded to their connected
        /// columns.</param>
        public record MappedPlacementResult(
            List<Column> ConnectedTargetColumns,
            List<Column> SourceColumns);

        /// <summary>
        /// A create request after null lists have been defaulted and the target column has been expanded.
        /// </summary>
        /// <param name="EntityIds">The entities to place.</param>
        /// <param name="AfterEntityIds">Raw anchor entity ids the placements should follow, when the client supplied
        /// them.</param>
        /// <param name="BeforeEntityIds">Raw anchor entity ids the placements should precede, when the client
        /// supplied them.</param>
        /// <param name="BoardId">The board the request targets.</param>
        /// <param name="ConnectedTargetColumnIds">The requested target column and every column connected to
        /// it.</param>
        public record PlacementMapped(
            List<EntityId> EntityIds,
            Guid[] AfterEntityIds,
            Guid[] BeforeEntityIds,
            BoardId BoardId,
            List<ColumnId> ConnectedTargetColumnIds
        );
    }
}