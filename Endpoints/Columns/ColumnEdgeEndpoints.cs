using AutoMapper;
using kanban_lia.Endpoints.Columns.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Services.Columns;
using kanban_lia.Services.Columns.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace kanban_lia.Endpoints.Columns
{
    public class ColumnEdgeEndpoints
    {
        public static void MapColumnEdgeEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/columnEdges");

            // Create a new column edge
            group.MapPost("/create", async (
                [FromBody] CreateColumnEdgeRequest request,
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
        }
    }
}