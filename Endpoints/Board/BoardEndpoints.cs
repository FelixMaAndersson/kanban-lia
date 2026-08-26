using kanban_lia.Domain;
using kanban_lia.Endpoints.Board.Requests;
using kanban_lia.Services;

// Lägg till BoardDto, ColumnDto och PlacementDto i Domain-mappen för att representera dataöverföringsobjekt (DTO) för respektive entitet.
// Dessa DTO:er används för att skicka data mellan klienten och servern utan att exponera de interna domänmodellerna direkt.

namespace kanban_lia.Endpoints.Board;

public static class BoardEndpoints
{
    public static void MapBoardEndpoints(WebApplication app)
    {

        var group = app.MapGroup("/api/boards");

        app.MapPost("/api/boards", async (
            Domain.Board board,
            IBoardService boardService) =>
        {
            await boardService.CreateAsync(board);

            return Results.Created(
                $"/api/boards/{board.Id}",
                board);
        });

        app.MapGet("/api/boards/{id:guid}", async (
            Guid id,
            IBoardService boardService) =>
        {
            var board = await boardService.GetByIdAsync(id);

            if (board is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(board);
        });

        // Alternativ 1. RenameBoardRequest är ett kontrakt som innehåller boardId och newName.
        group.MapPut("/rename", async (
            RenameBoardRequest request,
            IBoardService boardService) =>
        {
            var boardId = new BoardId(request.Id);
            var updatedBoard = await boardService.RenameAsync(boardId, request.NewName);

            return Results.Ok(updatedBoard);
        });

        //Alternativ 2.
        group.MapPut("/{id:guid}/addroot", async (
            Guid id,
            Guid newRootId,
            IBoardService boardService) =>
        {
            var boardId = new BoardId(id);
            var updatedBoard = await boardService.AddRootAsync(boardId, newRootId);

            return Results.Ok(updatedBoard.Roots);
        });

        app.MapDelete("/api/boards/{id:guid}", async (
            Guid id,
            IBoardService boardService) =>
        {
            var existingBoard = await boardService.GetByIdAsync(id);

            if (existingBoard is null)
            {
                return Results.NotFound();
            }

            await boardService.DeleteAsync(id);

            return Results.NoContent();
        });
    }
}