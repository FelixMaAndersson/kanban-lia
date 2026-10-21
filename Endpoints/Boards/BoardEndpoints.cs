using AutoMapper;

using Microsoft.AspNetCore.Mvc;

using kanban_lia.Endpoints.Boards.Requests;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Endpoints.Boards
{
    /// <summary>
    /// Maps board-related HTTP endpoints under /api/boards.
    /// </summary>
    /// <remarks>Registers endpoints for creating, retrieving, renaming, and deleting boards, as well as
    /// adding and removing roots: POST /create, GET /{id}, PUT /rename, PUT /addroot, DELETE /removeroot, and
    /// DELETE /delete/{id}. Request DTOs are mapped to service DTOs with AutoMapper before being passed to
    /// <see cref="IBoardService"/>.</remarks>
    public static class BoardEndpoints
    {
        /// <summary>
        /// Configures the board endpoints on the given application.
        /// </summary>
        /// <param name="app">The WebApplication on which board endpoints are configured.</param>
        public static void MapBoardEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/boards");

            // Create a new board
            group.MapPost("/create", async (
                string title,
                IBoardService boardService) =>
            {
                var newBoard = Board.Create(title);

                var boardId = await boardService.CreateAsync(newBoard.Title);

                return Results.Ok(new
                {
                    id = boardId.Id
                });
            });

            // Get a board by its ID
            group.MapGet("/{id:guid}", async (
                Guid id,
                IBoardService boardService) =>
            {
                var boardId = new BoardId(id);

                var board = await boardService.GetByIdAsync(boardId);

                return Results.Ok(board);
            });

            // Rename a board
            group.MapPut("/rename", async (
                [FromBody] RenameBoardRequest request,
                IBoardService boardService,
                IMapper mapper) =>
            {
                var requestDto = mapper.Map<RenameBoardDto>(request);

                var result = await boardService.RenameAsync(requestDto);

                return Results.Ok(result);
            });

            // Add a new root to a board
            group.MapPut("/addroot", async (
                [FromBody] AddRootRequest request,
                IBoardService boardService,
                IMapper mapper) =>
            {
                var requestDto = mapper.Map<AddRootDto>(request);

                var result = await boardService.AddRootAsync(requestDto);

                return Results.Ok(result);
            });

            // Remove a root from a board
            group.MapDelete("/removeroot", async (
                [FromBody] RemoveRootRequest request,
                IBoardService boardService,
                IMapper mapper) =>
            {
                var requestDto = mapper.Map<RemoveRootDto>(request);

                var result = await boardService.RemoveRootAsync(requestDto);

                return Results.Ok(result);
            });

            // Delete a board by its ID
            group.MapDelete("/delete/{id:guid}", async (
                Guid id,
                IBoardService boardService) =>
            {
                var boardId = new BoardId(id);

                var result = await boardService.DeleteAsync(boardId);

                return Results.Ok(result);
            });
        }
    }
}