using kanban_lia.Integration.Tests.Fixture;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
namespace kanban_lia.Integration.Tests.Endpoints
{
    [Collection(DatabaseCollection.Name)]
    public sealed class BoardEndpointTests(DatabaseFixture database) : IAsyncLifetime
    {
        private readonly DatabaseFixture _database = database;
        private readonly KanbanApiFactory _factory = new(database);

        public async ValueTask InitializeAsync() => await _database.ResetAsync();

        public ValueTask DisposeAsync()
        {
            _factory.Dispose();
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task GetBoard_Unknown_Returns404_WithProblemTitle()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();

            var response = await client.GetAsync($"/api/boards/{Guid.NewGuid()}", cancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            Assert.Equal("Board could not be found", problem!.Title);
        }

        [Fact]
        public async Task CreateBoard_WhitespaceTitle_Returns400()
        {
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();

            // 'string title' binds from QUERY in minimal APIs (no [FromBody])
            var response = await client.PostAsync("/api/boards/create?title=%20%20%20", null, cancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
