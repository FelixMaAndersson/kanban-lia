using Dapper;

using Microsoft.Data.SqlClient;

using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Integration.Tests.Fixture;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Integration.Tests.Repositories
{
    [Collection(DatabaseCollection.Name)]
    public class PlacementRepositoryTests(DatabaseFixture database) : IAsyncLifetime
    {
        private readonly DatabaseFixture _database = database;

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
        public async Task CreateAsync_WhenPlacementBoardDoesNotOwnColumn_DatabaseRejectsInsert()
        {
            var placementBoardId = Guid.NewGuid();
            var columnOwnerBoardId = Guid.NewGuid();
            var columnId = Guid.NewGuid();

            await using var connection = _database.DbFactory.CreateConnection();
            await connection.OpenAsync(TestContext.Current.CancellationToken);

            await connection.ExecuteAsync(
                """
            INSERT INTO dbo.Boards (Id, Title)
            VALUES (@PlacementBoardId, 'Placement board'),
                   (@ColumnOwnerBoardId, 'Column owner board');

            INSERT INTO dbo.Columns (Id, BoardId, Title, Position, RequestWritable)
            VALUES (@ColumnId, @ColumnOwnerBoardId, 'Column on another board', 1, 1);
            """,
                new
                {
                    PlacementBoardId = placementBoardId,
                    ColumnOwnerBoardId = columnOwnerBoardId,
                    ColumnId = columnId
                });

            var placement = Placement.Create(
                new EntityId(Guid.NewGuid()),
                new BoardId(placementBoardId),
                new ColumnId(columnId),
                "a0");

            var repository = new PlacementRepository(_database.DbFactory);
            using var transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<SqlException>(
                () => repository.CreateAsync([placement], transaction));

            Assert.Equal(547, exception.Number);

            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task GetCurrentAsync_WithTransaction_CanReadPlacementCreatedInSameTransaction()
        {
            // Arrange
            var cancellationToken = TestContext.Current.CancellationToken;
            var boardId = Guid.NewGuid();
            var columnId = Guid.NewGuid();
            var entityId = new EntityId(Guid.NewGuid());

            await using var connection = _database.DbFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await connection.ExecuteAsync(
                """
            INSERT INTO dbo.Boards (Id, Title)
            VALUES (@BoardId, 'Transaction read test');

            INSERT INTO dbo.Columns (Id, BoardId, Title, Position, RequestWritable)
            VALUES (@ColumnId, @BoardId, 'Column', 1, 1);
            """,
                new { BoardId = boardId, ColumnId = columnId });

            var repository = new PlacementRepository(_database.DbFactory);
            var board = new BoardId(boardId);
            var column = new ColumnId(columnId);

            await using var transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            var placement = Placement.Create(
                entityId,
                board,
                column,
                "a0");

            // Act
            await repository.CreateAsync([placement], transaction);

            var currentPlacements = await repository.GetCurrentAsync(
                [entityId],
                board,
                transaction,
                cancellationToken);

            // Assert
            var currentPlacement = Assert.Single(currentPlacements);
            Assert.Equal(entityId, currentPlacement.EntityId);
            Assert.Equal(column, currentPlacement.ColumnId);

            await transaction.RollbackAsync(cancellationToken);
        }
    }
}
