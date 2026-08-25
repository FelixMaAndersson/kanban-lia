using kanban_lia.Services;

namespace kanban_lia.Endpoints;

public static class CardPlacementEndpoints
{
    public static void MapCardPlacementEndpoints(WebApplication app)
    {
        app.MapPost("/api/cardplacements", async (
            Domain.CardPlacement cardPlacement,
            ICardPlacementService cardPlacementService) =>
        {
            await cardPlacementService.CreateAsync(cardPlacement);

            return Results.Created(
                $"/api/cardplacements/{cardPlacement.Id}",
                cardPlacement);
        });

        app.MapGet("/api/cardplacements", async (
            ICardPlacementService cardPlacementService) =>
        {
            var placements = await cardPlacementService.GetAllPlacementsAsync();

            return Results.Ok(placements);
        });

        app.MapGet("/api/cardplacements/{id:guid}", async (
            Guid id,
            ICardPlacementService cardPlacementService) =>
        {
            var placement = await cardPlacementService.GetPlacementByIdAsync(id);

            if (placement is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(placement);
        });
    }
}