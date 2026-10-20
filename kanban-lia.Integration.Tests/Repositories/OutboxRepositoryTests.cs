using Dapper;

using kanban_lia.Infrastructure.Repositories.Outbox;
using kanban_lia.Integration.Tests.Fixture;

namespace kanban_lia.Integration.Tests.Repositories
{
    [Collection(DatabaseCollection.Name)]
    public sealed class OutboxRepositoryTests(DatabaseFixture database) : IAsyncLifetime
    {
        private readonly DatabaseFixture _database = database;
        private readonly OutboxRepository _repository = new(database.DbFactory);

        public async ValueTask InitializeAsync()
        {
            await _database.ResetAsync();
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }

        [Fact]
        public async Task GetUnprocessed_OrdersByOccurredOn_Ascending()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            await using var connection = _database.DbFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var second = new OutboxMessage(Guid.NewGuid(), "PlacementCreated", "{ }", now.AddMinutes(-1), null, null);
            var third = new OutboxMessage(Guid.NewGuid(), "PlacementCreated", "{ }", now, null, null);
            var first = new OutboxMessage(Guid.NewGuid(), "PlacementCreated", "{ }", now.AddMinutes(-2), null, null);

            await _repository.AddAsync(second, transaction, cancellationToken);
            await _repository.AddAsync(third, transaction, cancellationToken);
            await _repository.AddAsync(first, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Act
            var result = (await _repository.GetUnprocessedAsync(cancellationToken)).ToList();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { first.Id, second.Id, third.Id },
                result.Select(m => m.Id).ToArray());
        }

        [Fact]
        public async Task MarkAsFailed_IncrementsRetryCount_StaysUnprocessed()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            await using var connection = _database.DbFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var messageInsert = new OutboxMessage(Guid.NewGuid(), "PlacementCreated", "{ }", now, null, null);

            await _repository.AddAsync(messageInsert, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Act & Assert
            var messageBefore = Assert.Single(await _repository.GetUnprocessedAsync(cancellationToken));

            Assert.Equal(messageInsert.Id, messageBefore.Id);
            Assert.Equal(0, messageBefore.RetryCount);

            await _repository.MarkAsFailedAsync(messageBefore.Id, "Error", cancellationToken);

            var messageAfter = Assert.Single(await _repository.GetUnprocessedAsync(cancellationToken));

            Assert.Equal(messageInsert.Id, messageAfter.Id);
            Assert.Equal(1, messageAfter.RetryCount);
            Assert.Equal("Error", messageAfter.Error);
        }

        [Fact]
        public async Task MarkAsFailed_FifthAttempt_SetsProcessedOn()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            await using var connection = _database.DbFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var messageInsert = new OutboxMessage(Guid.NewGuid(), "PlacementCreated", "{ }", now, null, null);

            await _repository.AddAsync(messageInsert, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Act & Assert
            var message = Assert.Single(await _repository.GetUnprocessedAsync(cancellationToken));

            Assert.Equal(messageInsert.Id, message.Id);
            Assert.Equal(0, message.RetryCount);

            await _repository.MarkAsFailedAsync(message.Id, "Error", cancellationToken);
            await _repository.MarkAsFailedAsync(message.Id, "Error", cancellationToken);
            await _repository.MarkAsFailedAsync(message.Id, "Error", cancellationToken);
            await _repository.MarkAsFailedAsync(message.Id, "Error", cancellationToken);
            await _repository.MarkAsFailedAsync(message.Id, "Error", cancellationToken);

            Assert.Empty(await _repository.GetUnprocessedAsync(cancellationToken));

            await using var verifyConnection = _database.DbFactory.CreateConnection();
            await verifyConnection.OpenAsync(cancellationToken);

            var (processedOn, retryCount) = await verifyConnection.QuerySingleAsync<(DateTime? ProcessedOn, int RetryCount)>(
                "SELECT ProcessedOn, RetryCount FROM OutBoxMessages WHERE Id = @Id",
                new { messageInsert.Id });

            Assert.NotNull(processedOn);
            Assert.Equal(5, retryCount);
        }

        [Fact]
        public async Task MarkAsProcessed_SetsProcessedOn_ExcludedFromUnprocessed()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;

            await using var connection = _database.DbFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var messageInsert = new OutboxMessage(Guid.NewGuid(), "PlacementCreated", "{ }", now, null, null);

            await _repository.AddAsync(messageInsert, transaction, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Act & Assert
            var message = Assert.Single(await _repository.GetUnprocessedAsync(cancellationToken));

            Assert.Equal(messageInsert.Id, message.Id);
            Assert.Equal(0, message.RetryCount);

            await _repository.MarkAsProcessedAsync(message.Id, cancellationToken);

            Assert.Empty(await _repository.GetUnprocessedAsync(cancellationToken));

            var (processedOn, retryCount) = await connection.QuerySingleAsync<(DateTime? ProcessedOn, int RetryCount)>(
                "SELECT ProcessedOn, RetryCount FROM OutBoxMessages WHERE Id = @Id",
                new { messageInsert.Id });

            Assert.NotNull(processedOn);
            Assert.Equal(0, retryCount);
        }
    }
}
