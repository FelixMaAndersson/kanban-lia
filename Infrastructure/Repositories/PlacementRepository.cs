using Dapper;

using kanban_lia.Domain;
using kanban_lia.Infrastructure.Database;

namespace kanban_lia.Infrastructure.Repositories
{
    public class PlacementRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        public PlacementRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreatePlacementAsync(CardPlacement placement)
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

        public async Task<IEnumerable<CardPlacement>> GetAllPlacementsAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var placements = await connection.QueryAsync<CardPlacement>(
                @"
                    SELECT * FROM Placements"
            );
            return placements;
        }

        public async Task<CardPlacement?> GetPlacementByIdAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var placement = await connection.QuerySingleOrDefaultAsync<CardPlacement>(
                @"
                    SELECT * FROM Placements 
                    WHERE Id = @Id",
                new { Id = id }
            );
            return placement;
        }
    }
}