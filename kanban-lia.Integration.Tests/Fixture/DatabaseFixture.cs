using Dapper;

using Respawn;

using Testcontainers.MsSql;

using Microsoft.Data.SqlClient;

using kanban_lia.Infrastructure.Database;

namespace kanban_lia.Integration.Tests.Fixture
{
    public sealed class DatabaseFixture : IAsyncLifetime
    {
        private readonly MsSqlContainer _sqlServer =
            new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();

        public string ConnectionString { get; private set; } = null!;
        public DbConnectionFactory DbFactory { get; private set; } = null!;
        private Respawner _respawner = null!;

        public async ValueTask InitializeAsync()
        {
            try
            {
                await _sqlServer.StartAsync();

                await using (var connection =
                    new SqlConnection(_sqlServer.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    await connection.ExecuteAsync(
                        "CREATE DATABASE KanbanTestDb;");
                }

                var connectionString = new SqlConnectionStringBuilder(
                    _sqlServer.GetConnectionString())
                {
                    InitialCatalog = "KanbanTestDb"
                }.ConnectionString;

                DbFactory = new DbConnectionFactory(connectionString);
                ConnectionString = connectionString;

                await ApplyMigrationsAsync();

                await using var databaseConnection = DbFactory.CreateConnection();
                await databaseConnection.OpenAsync();

                _respawner = await Respawner.CreateAsync(
                    databaseConnection,
                    new RespawnerOptions
                    {
                        DbAdapter = DbAdapter.SqlServer,
                        SchemasToInclude = ["dbo"]
                    });
            }
            catch
            {
                await _sqlServer.DisposeAsync();
                throw;
            }
        }

        public async Task ResetAsync()
        {
            await using var connection = DbFactory.CreateConnection();
            await connection.OpenAsync();

            await _respawner.ResetAsync(connection);
        }

        public async ValueTask DisposeAsync()
        {
            await _sqlServer.DisposeAsync();
        }

        private async Task ApplyMigrationsAsync()
        {
            await using var connection = DbFactory.CreateConnection();
            await connection.OpenAsync();

            var files = Directory
                .GetFiles(
                    Path.Combine(AppContext.BaseDirectory, "migrations"),
                    "V*.sql")
                .OrderBy(file => Path.GetFileName(file), StringComparer.Ordinal);

            foreach (var file in files)
            {
                var sql = await File.ReadAllTextAsync(file);
                await connection.ExecuteAsync(sql);
            }
        }
    }
}

