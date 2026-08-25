using kanban_lia.Services;

namespace kanban_lia.Endpoints;

public static class BoardEndpoints
{
    public static void MapBoardEndpoints(WebApplication app)
    {
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
            var board = await boardService.GetBoardByIdAsync(id);

            if (board is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(board);
        });

        app.MapPut("/api/boards/{id:guid}", async (
            Guid id,
            Domain.Board updatedBoard,
            IBoardService boardService) =>
        {
            var existingBoard = await boardService.GetBoardByIdAsync(id);

            if (existingBoard is null)
            {
                return Results.NotFound();
            }

            updatedBoard.Id = id;

            await boardService.UpdateAsync(updatedBoard);

            return Results.NoContent();
        });

        app.MapDelete("/api/boards/{id:guid}", async (
            Guid id,
            IBoardService boardService) =>
        {
            var existingBoard = await boardService.GetBoardByIdAsync(id);

            if (existingBoard is null)
            {
                return Results.NotFound();
            }

            await boardService.DeleteAsync(id);

            return Results.NoContent();
        });
    }
}