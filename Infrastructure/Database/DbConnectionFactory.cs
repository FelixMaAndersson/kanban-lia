using Microsoft.Data.SqlClient;

using System.Data.Common;

namespace kanban_lia.Infrastructure.Database
{
    /// <summary>
    /// Creates SQL Server database connections from a configured connection string.
    /// </summary>
    /// <remarks>Instances are typically registered once and shared; the factory itself is stateless apart from the
    /// connection string. Connections returned by <see cref="CreateConnection"/> are not opened.</remarks>
    /// <param name="connectionString">The SQL Server connection string used for every created connection.</param>
    public class DbConnectionFactory(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbConnectionFactory"/> class using the DefaultConnection
        /// connection string from configuration.
        /// </summary>
        /// <param name="configuration">Configuration that supplies the DefaultConnection connection string.</param>
        /// <exception cref="InvalidOperationException">The DefaultConnection connection string is not present in
        /// configuration.</exception>
        public DbConnectionFactory(IConfiguration configuration)
            : this(configuration.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("Connection string not found."))
        {
        }

        /// <summary>
        /// Creates a new, unopened SQL Server connection using the configured connection string.
        /// </summary>
        /// <returns>A new <see cref="SqlConnection"/> instance that the caller owns and must dispose.</returns>
        public DbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}