using Microsoft.AspNetCore.Mvc;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using kanban_lia.Integration.Tests.Fixture;

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
        public async Task CreateBoard_Valid_ReturnsId_AndRoundTripsViaGet()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();

            // Act
            var createResponse = await client.PostAsync("/api/boards/create?title=Testboard", null, cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            var boardId = created.GetProperty("id").GetGuid();
            Assert.NotEqual(Guid.Empty, boardId);

            // Act
            var getResponse = await client.GetAsync($"/api/boards/{boardId}", cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var board = await getResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            Assert.Equal(boardId, board.GetProperty("id").GetGuid());
            Assert.Equal("Testboard", board.GetProperty("title").GetString());
            Assert.Empty(board.GetProperty("roots").EnumerateArray());
        }

        [Fact]
        public async Task CreateBoard_WhitespaceTitle_Returns400()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync("/api/boards/create?title=%20%20%20", null, cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetBoard_Unknown_Returns404_WithProblemTitle()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/api/boards/{Guid.NewGuid()}", cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            Assert.Equal("Board could not be found", problem!.Title);
        }

        [Fact]
        public async Task RenameBoard_Valid_Returns200_UpdatesTitle()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();
            var boardId = await EndpointArrange.CreateBoardAsync(client, "Old Title");

            // Act
            var renameResponse = await client.PutAsJsonAsync(
                "/api/boards/rename",
                new { id = boardId, newTitle = "New Title" },
                cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, renameResponse.StatusCode);
            Assert.True(await renameResponse.Content.ReadFromJsonAsync<bool>(cancellationToken));

            var getResponse = await client.GetAsync($"/api/boards/{boardId}", cancellationToken);
            var board = await getResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            Assert.Equal("New Title", board.GetProperty("title").GetString());
        }

        [Fact]
        public async Task RenameBoard_Unknown_Returns404()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();

            // Act
            var renameResponse = await client.PutAsJsonAsync(
                "/api/boards/rename",
                new { id = Guid.NewGuid(), newTitle = "New Title" },
                cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, renameResponse.StatusCode);
        }

        [Fact]
        public async Task AddRoot_WhenRootAlreadyExists_Returns400()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();
            var boardId = await EndpointArrange.CreateBoardAsync(client, "Testboard");
            var entityId = Guid.NewGuid();

            // Act
            var firstResponse = await client.PutAsJsonAsync(
                "/api/boards/addroot",
                new { boardId, entityId },
                cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.True(await firstResponse.Content.ReadFromJsonAsync<bool>(cancellationToken));

            // Act
            var secondResponse = await client.PutAsJsonAsync(
                "/api/boards/addroot",
                new { boardId, entityId },
                cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
            var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            Assert.Equal("Root already exists on this board", problem!.Title);
        }

        [Fact]
        public async Task RemoveRoot_WhenNoRoot_Returns404()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();
            var boardId = await EndpointArrange.CreateBoardAsync(client, "Testboard");
            var entityId = Guid.NewGuid();

            // Act — DELETE with a JSON body (endpoint is MapDelete + [FromBody])
            var removeRequest = new HttpRequestMessage(
                HttpMethod.Delete, "/api/boards/removeroot")
            {
                Content = JsonContent.Create(new { boardId, entityId })
            };

            var removeRootResponse = await client.SendAsync(
                removeRequest, cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, removeRootResponse.StatusCode);
            var problem = await removeRootResponse.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            Assert.Equal("Root could not be found", problem!.Title);
        }

        [Fact]
        public async Task DeleteBoard_Unknown_Returns404()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();

            // Act
            var deleteRootResponse = await client.DeleteAsync(
                $"/api/boards/delete/{Guid.NewGuid()}",
                cancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, deleteRootResponse.StatusCode);
            var problem = await deleteRootResponse.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
            Assert.Equal("Board could not be found", problem!.Title);
        }
    }
}
