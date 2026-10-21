using AutoMapper;

using Microsoft.AspNetCore.Mvc;

using kanban_lia.Endpoints.Columns.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Endpoints.Columns
{
    /// <summary>
    /// Maps column-edge HTTP endpoints under /api/columnedges.
    /// </summary>
    /// <remarks>Registers POST /create to add a directed edge between two columns, GET /boardid/{id} to list all
    /// edges of a board, and DELETE /delete to remove the edge identified by the from/to column pair in the request
    /// body. Requests are mapped to <see cref="IColumnEdgeService"/> DTOs with AutoMapper. Creating an edge that
    /// already exists surfaces as a conflict-style error via the application's exception handler, and creation
    /// normalises the pair to a canonical direction, but deletion matches the stored from/to pair exactly, so the
    /// ids sent to DELETE must be in the same orientation.</remarks>
    public static class ColumnEdgeEndpoints
    {
        /// <summary>
        /// Registers the column-edge endpoints on the given application.
        /// </summary>
        /// <remarks>Creates the "/api/columnedges" route group and wires up the create, board lookup and delete
        /// handlers described on the containing type.</remarks>
        /// <param name="app">The WebApplication to configure.</param>
        /// <exception cref="ColumnEdgeAlreadyExistsException">
        /// Propagated from the service when POST /create targets an edge that already exists.
        /// </exception>
        public static void MapColumnEdgeEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/columnedges");

            // Create a new column edge
            group.MapPost("/create", async (
                [FromBody] ColumnEdgeRequest request,
                IColumnEdgeService columnEdgeService,
                IMapper mapper) =>
            {
                var dto = mapper.Map<CreateColumnEdgeDto>(request);

                await columnEdgeService.CreateAsync(dto);

                return Results.Ok();
            });

            // Get columns by board id
            group.MapGet("/boardid/{id:guid}", async (
                Guid id,
                IColumnEdgeService columnEdgeService) =>
            {
                var boardId = new BoardId(id);

                var columnEdges = await columnEdgeService.GetByBoardIdAsync(boardId);

                return Results.Ok(columnEdges);
            });

            // Delete a column edge
            group.MapDelete("/delete", async (
                [FromBody] ColumnEdgeRequest request,
                IColumnEdgeService columnEdgeService) =>
            {
                var edge = ColumnEdge.Create(
                    new ColumnId(request.FromColumnId),
                    new ColumnId(request.ToColumnId)
                );

                var result = await columnEdgeService.DeleteAsync(edge);

                return Results.Ok(result);
            });
        }
    }
}