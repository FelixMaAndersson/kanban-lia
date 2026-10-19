using Dapper;
using kanban_lia.Infrastructure.Repositories.Placements;
using kanban_lia.Integration.Tests.Fixture;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using Microsoft.Data.SqlClient;

namespace kanban_lia.Integration.Tests.Repositories;

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
}
