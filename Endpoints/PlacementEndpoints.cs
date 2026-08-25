using kanban_lia.Services;

// Lägg till BoardDto, ColumnDto och PlacementDto i Domain-mappen för att representera dataöverföringsobjekt (DTO) för respektive entitet.
// Dessa DTO:er används för att skicka data mellan klienten och servern utan att exponera de interna domänmodellerna direkt.

namespace kanban_lia.Endpoints;

public static class PlacementEndpoints
{
    public static void MapPlacementEndpoints(WebApplication app)
    {

        app.MapGet("/api/placements", async (
            IPlacementService placementService) =>
        {
            var placements = await placementService.GetAllAsync();

            return Results.Ok(placements);
        });

        app.MapGet("/api/placements/{id:guid}", async (
            Guid id,
            IPlacementService placementService) =>
        {
            var placement = await placementService.GetByIdAsync(id);

            if (placement is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(placement);
        });
    }
}