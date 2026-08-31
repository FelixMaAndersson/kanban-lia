using Dapper;
using Microsoft.Data.SqlClient;

using kanban_lia.Infrastructure.Database;

namespace kanban_lia.Integration.Tests;

public class DatabaseFixture : IAsyncLifetime
{
    public DbConnectionFactory DbFactory { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;

    private string _dbName = null!;

    private const string MasterConn =
        "Server=localhost,1434;Database=master;User Id=sa;Password=Password123!;TrustServerCertificate=True;";

    public async Task InitializeAsync()
    {
        _dbName = $"KanbanDbTest_{Guid.NewGuid():N}";
        ConnectionString =
            $"Server=localhost,1434;Database={_dbName};User Id=sa;Password=Password123!;TrustServerCertificate=True;";

        using (var conn = new SqlConnection(MasterConn))
            await conn.ExecuteAsync($"CREATE DATABASE [{_dbName}]");

        await ApplyMigrationsAsync();

        DbFactory = new DbConnectionFactory(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        using var conn = new SqlConnection(MasterConn);
        await conn.ExecuteAsync(
            $"ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_dbName}]");
    }

    private async Task ApplyMigrationsAsync()
    {
        using var conn = new SqlConnection(ConnectionString);
        var files = Directory
            .GetFiles(Path.Combine(AppContext.BaseDirectory, "migrations"), "V*.sql")
            .OrderBy(f => f, StringComparer.Ordinal);

        foreach (var file in files)
            await conn.ExecuteAsync(await File.ReadAllTextAsync(file));
    }
}