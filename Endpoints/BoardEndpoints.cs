using AutoMapper;
using kanban_lia.Endpoints.Requests.Board;
using kanban_lia.Models.Domain;
using kanban_lia.Services;
using kanban_lia.Services.DTOs;

// Lägg till BoardDto, ColumnDto och PlacementDto i Domain-mappen för att representera dataöverföringsobjekt (DTO) för respektive entitet.
// Dessa DTO:er används för att skicka data mellan klienten och servern utan att exponera de interna domänmodellerna direkt.

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

            var board = await boardService.CreateAsync(newBoard.Title);

            return Results.Created($"/api/boards/create", board);
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

            await boardService.RenameAsync(requestDto);

            return Results.NoContent();
        });

        // Add a new root to a board
        group.MapPut("/addroot", async (
            AddRootRequest request,
            IBoardService boardService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<AddRootDto>(request);

            await boardService.AddRootAsync(requestDto);

            return Results.Ok(requestDto);
        });

        // Remove a root from a board
        group.MapDelete("/removeroot", async (
            RemoveRootRequest request,
            IBoardService boardService,
            IMapper mapper) =>
        {
            var requestDto = mapper.Map<RemoveRootDto>(request);

            await boardService.RemoveRootAsync(requestDto);

            return Results.Ok(requestDto);
        });


        // Delete a board by its ID
        group.MapDelete("/delete/{id:guid}", async (
            Guid id,
            IBoardService boardService) =>
        {
            var boardId = new BoardId(id);

            await boardService.DeleteAsync(boardId);

            return Results.NoContent();
        });
    }
}