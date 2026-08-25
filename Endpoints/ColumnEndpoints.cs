using kanban_lia.Services;

namespace kanban_lia.Endpoints;

public static class ColumnEndpoints
{
    public static void MapColumnEndpoints(WebApplication app)
    {
        app.MapPost("/api/columns", async (
            Domain.Column column,
            IColumnService columnService) =>
        {
            await columnService.CreateAsync(column);

            return Results.Created(
                $"/api/columns/{column.Id}",
                column);
        });

        app.MapGet("/api/columns", async (
            IColumnService columnService) =>
        {
            var columns = await columnService.GetAllAsync();

            return Results.Ok(columns);
        });

        app.MapGet("/api/columns/{id:guid}", async (
            Guid id,
            IColumnService columnService) =>
        {
            var column = await columnService.GetByIdAsync(id);

            if (column is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(column);
        });

        app.MapPut("/api/columns/{id:guid}", async (
            Guid id,
            Domain.Column updatedColumn,
            IColumnService columnService) =>
        {
            var existingColumn = await columnService.GetByIdAsync(id);

            if (existingColumn is null)
            {
                return Results.NotFound();
            }

            updatedColumn.Id = id;

            await columnService.UpdateAsync(updatedColumn);

            return Results.NoContent();
        });

        app.MapDelete("/api/columns/{id:guid}", async (
            Guid id,
            IColumnService columnService) =>
        {
            var existingColumn = await columnService.GetByIdAsync(id);

            if (existingColumn is null)
            {
                return Results.NotFound();
            }

            await columnService.DeleteAsync(id);

            return Results.NoContent();
        });
    }
}