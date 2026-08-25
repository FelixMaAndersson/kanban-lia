using kanban_lia.Services;

namespace kanban_lia.Endpoints;

public static class PlacementEndpoints
{
    public static void MapPlacementEndpoints(WebApplication app)
    {
        app.MapPost("/api/placements", async (
            Domain.Placement placement,
            IPlacementService placementService) =>
        {
            await placementService.CreateAsync(placement);

            return Results.Created(
                $"/api/placements/{placement.Id}",
                placement);
        });

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