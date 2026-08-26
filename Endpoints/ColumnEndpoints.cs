using AutoMapper;
using kanban_lia.Endpoints.Requests.Column;
using kanban_lia.Models.Domain;
using kanban_lia.Services;
using kanban_lia.Services.DTOs;

// Lägg till BoardDto, ColumnDto och PlacementDto i Domain-mappen för att representera dataöverföringsobjekt (DTO) för respektive entitet.
// Dessa DTO:er används för att skicka data mellan klienten och servern utan att exponera de interna domänmodellerna direkt.

namespace kanban_lia.Endpoints;

public static class ColumnEndpoints
{
    public static void MapColumnEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/columns");

        group.MapPost("/create", async (
            CreateColumnRequest request,
            IColumnService columnService,
            IMapper mapper) =>
        {
            var dto = mapper.Map<CreateColumnDto>(request);

            var column = await columnService.CreateAsync(dto);

            return Results.Created($"/api/columns/create", column);
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
            Column updatedColumn,
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