using Microsoft.AspNetCore.SignalR;

namespace kanban_lia.Hubs
{
    /// <summary>
    /// SignalR hub that delivers real-time board updates to clients subscribed to a single board.
    /// </summary>
    /// <remarks>Clients connect to /hubs/board with a boardId query-string parameter. On connect the connection
    /// joins the SignalR group named by <see cref="GroupName"/>, which is the group server code uses to broadcast
    /// per-board events such as PlacementCreated. A connection whose boardId is missing or not a valid GUID is not
    /// added to any group and therefore receives no broadcasts. The hub itself defines no client-invocable methods.
    /// </remarks>
    public class BoardHub : Hub
    {
        /// <summary>
        /// Returns the SignalR group name used to target subscribers of the given board.
        /// </summary>
        /// <remarks>The same helper must be used by both <see cref="OnConnectedAsync"/> when adding connections and
        /// by server code broadcasting to a board, so that the names always agree.</remarks>
        /// <param name="boardId">Identifier of the board whose subscribers should receive a message.</param>
        /// <returns>The group name in the form "board:{boardId}".</returns>
        public static string GroupName(Guid boardId) => $"board:{boardId}";

        /// <summary>
        /// Adds the newly connected connection to its board's group before running the base pipeline.
        /// </summary>
        /// <remarks>Reads the boardId value from the query string of the underlying HTTP request. When the value is
        /// absent or cannot be parsed as a GUID, the connection is simply left out of every group rather than being
        /// disconnected.</remarks>
        /// <returns>A task that completes once the connection has been added to its group and the base
        /// implementation has run.</returns>
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
