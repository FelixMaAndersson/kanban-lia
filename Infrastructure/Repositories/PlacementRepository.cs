using Dapper;

using kanban_lia.Domain;
using kanban_lia.Infrastructure.Database;

namespace kanban_lia.Infrastructure.Repositories
{
    public class PlacementRepository(DbConnectionFactory connectionFactory)
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task CreateAsync(Placement placement)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    INSERT INTO Placements 
                    (Id, ColumnId, Position) 
                    VALUES (@Id, @BoardId, @ColumnId, @Position)",
                placement
            );
        }

        public async Task<IEnumerable<Placement>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var placements = await connection.QueryAsync<Placement>(
                @"
                    SELECT * FROM Placements"
            );
            return placements;
        }

        public async Task<Placement?> GetByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var placement = await connection.QuerySingleOrDefaultAsync<Placement>(
                @"
                    SELECT * FROM Placements 
                    WHERE Id = @Id",
                new { Id = id }
            );
            return placement;
        }
    }
}