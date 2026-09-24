using kanban_lia.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace kanban_lia.Endpoints.RandomWords
{
    public static class RandomWordEndpoints
    {
        public static void MapRandomWordEndpoints(WebApplication app)
        {
            app.MapPost(
            "/api/randomword",
            async (
                RandomWordRequest request,
                IHubContext<BoardHub> hubContext,
                CancellationToken cancellationToken) =>
            {
                await hubContext.Clients.All.SendAsync(
                    "RandomWordReceived",
                    request,
                    cancellationToken);

                return Results.Ok();
            });
        }
    }
}
