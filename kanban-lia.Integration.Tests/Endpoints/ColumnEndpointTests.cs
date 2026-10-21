using kanban_lia.Integration.Tests.Fixture;

namespace kanban_lia.Integration.Tests.Endpoints
{
    [Collection(DatabaseCollection.Name)]
    public sealed class ColumnEndpointTests(DatabaseFixture database) : IAsyncLifetime
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
        public async Task CreateColumn_UnknownBoard_Returns409()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            var client = _factory.CreateClient();

            
        }
    }
}
