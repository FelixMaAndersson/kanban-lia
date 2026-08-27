using AutoMapper;
using kanban_lia.Endpoints.Requests.Board;
using kanban_lia.Models.Domain;
using kanban_lia.Services.Boards;
using kanban_lia.Services.Boards.DTOs;

namespace kanban_lia.Endpoints;

public static class BoardEndpoints
{
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

            return Results.Created($"/api/boards/create", boardId);
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
            RenameBoardRequest request,
            IBoardService boardService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<RenameBoardDto>(request);

            var success = await boardService.RenameAsync(requestDto);

            return Results.Ok(success);
        });

        // Add a new root to a board
        group.MapPut("/addroot", async (
            AddRootRequest request,
            IBoardService boardService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<AddRootDto>(request);

            var success = await boardService.AddRootAsync(requestDto);

            return Results.Ok(success);
        });

        // Remove a root from a board
        group.MapDelete("/removeroot", async (
            RemoveRootRequest request,
            IBoardService boardService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<RemoveRootDto>(request);

            var success = await boardService.RemoveRootAsync(requestDto);

            return Results.Ok(success);
        });


        // Delete a board by its ID
        group.MapDelete("/delete/{id:guid}", async (
            Guid id,
            IBoardService boardService) =>
        {
            var boardId = new BoardId(id);

            var success = await boardService.DeleteAsync(boardId);

            return Results.Ok(success);
        });
    }
}