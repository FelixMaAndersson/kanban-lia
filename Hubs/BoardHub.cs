using Microsoft.AspNetCore.SignalR;

namespace kanban_lia.Hubs
{
    public class BoardHub : Hub
    {
        public static string GroupName(Guid boardId) => $"board:{boardId}";

        public override async Task OnConnectedAsync()
        {
            var boardIdValue = Context.GetHttpContext()
                ?.Request.Query["boardId"]
                .ToString();

            if (Guid.TryParse(boardIdValue, out var boardId))
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    GroupName(boardId));
            }

            await base.OnConnectedAsync();
        }
    }
}
