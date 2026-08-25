using Microsoft.Data.SqlClient;
using System.Data;

namespace kanban_lia.Infrastructure.Database;

public class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string not found.");

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}