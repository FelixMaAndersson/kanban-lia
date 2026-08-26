using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public class PlacementRepository(DbConnectionFactory connectionFactory) : IPlacementRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task<Placement> CreateAsync(Placement placement)
        {
            using var connection = _connectionFactory.CreateConnection();
            var id = await connection.ExecuteScalarAsync<Guid>(
                @"
                    INSERT INTO Placements 
                    (EntityId, ColumnId, Position) 
                    VALUES (@EntityId, @ColumnId, @Position)",
                placement
            );

            return placement;

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