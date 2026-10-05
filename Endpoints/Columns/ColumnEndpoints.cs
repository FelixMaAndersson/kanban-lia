using AutoMapper;
using Microsoft.AspNetCore.Mvc;

using kanban_lia.Endpoints.Columns.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Endpoints.Columns;

public static class ColumnEndpoints
{
    public static void MapColumnEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/columns");

        // Create a new column
        group.MapPost("/create", async (
            HttpRequest httpRequest,
            [FromBody] CreateColumnRequest request,
            IColumnService columnService,
            IMapper mapper,
            CancellationToken cancellationToken) =>
        {
            var dto = mapper.Map<CreateColumnDto>(request);

            Guid correlationId = Guid.NewGuid();
            var actorId = -1;
            var actorType = "";

            if (httpRequest.Headers.TryGetValue(
                "Correlation-Id", 
                out var correlationHeader) &&
                Guid.TryParse(correlationHeader, out var parsedCorrelationId))
            {
                correlationId = parsedCorrelationId;
            }

            if (httpRequest.Headers.TryGetValue(
                "Actor-Id",
                out var actorHeader) &&
                int.TryParse(actorHeader, out var parsedActorId))
            {
                actorId = parsedActorId;
            }

            if (httpRequest.Headers.TryGetValue(
                "Actor-Type",
                out var actorTypeHeader))
            {
                actorType = actorTypeHeader.ToString();
            }

            var actor = new Actor(actorId, actorType);

            await columnService.CreateAsync(dto, correlationId, actor, cancellationToken);

            return Results.Ok();
        });

        // Get columns by board id
        group.MapGet("/boardid/{id:guid}", async (
            Guid id,
            IColumnService columnService) =>
        {
            var boardId = new BoardId(id);

            var columns = await columnService.GetByBoardIdAsync(boardId);

            return Results.Ok(columns);
        });

        // Get by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            IColumnService columnService) =>
        {
            var column = await columnService.GetByIdAsync(new ColumnId(id));

            return Results.Ok(column);
        });

        // Rename a column
        group.MapPut("/rename", async (
            [FromBody] RenameColumnRequest request,
            IColumnService columnService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<RenameColumnDto>(request);

            var result = await columnService.RenameAsync(requestDto);

            return Results.Ok(result);
        });

        // Delete a column
        group.MapDelete("/delete/{id:guid}", async (
            Guid id,
            IColumnService columnService) =>
        {
            var columnId = new ColumnId(id);

            var result = await columnService.DeleteAsync(columnId);

            return Results.Ok(result);
        });
    }
}