using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace kanban_lia.Infrastructure.Database;

public class DbConnectionFactory(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public DbConnectionFactory(IConfiguration configuration)
        : this(configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string not found."))
    {
    }

    public DbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}