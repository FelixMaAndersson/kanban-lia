using System.Net.Http.Json;
using System.Text.Json;

namespace kanban_lia.Integration.Tests.Endpoints
{
    public static class EndpointArrange
    {
        public static async Task<Guid> CreateBoardAsync(HttpClient client, string title)
        {
            var response = await client.PostAsync(
                $"/api/boards/create?title={Uri.EscapeDataString(title)}", null);

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
            return payload.GetProperty("id").GetGuid();
        }

        public static async Task<Guid> CreateColumnAsync(
            HttpClient client, Guid boardId, string title, int position = 0)
        {
            var response = await client.PostAsJsonAsync(
                "/api/columns/create",
                new { boardId, title, position });

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
            return payload.GetProperty("id").GetGuid();
        }
    }
}
