using Microsoft.Data.SqlClient;
using System.Data;

namespace kanban_lia.Infrastructure.Database;

public class DbConnectionFactory(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public DbConnectionFactory(IConfiguration configuration)
        : this(configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string not found."))
    {
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}