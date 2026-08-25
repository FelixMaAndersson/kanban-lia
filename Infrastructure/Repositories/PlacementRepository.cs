using Dapper;
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

        public async Task CreatePlacementAsync(Domain.CardPlacement placement)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    INSERT INTO Placements 
                    (Id, BoardId, ColumnId, Position) 
                    VALUES (@Id, @BoardId, @ColumnId, @Position)",
                placement
            );
        }

        public async Task<IEnumerable<Domain.CardPlacement>> GetAllPlacementsAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var placements = await connection.QueryAsync<Domain.CardPlacement>(
                @"
                    SELECT * FROM Placements"
            );
            return placements;
        }

        public async Task<Domain.CardPlacement?> GetPlacementByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var placement = await connection.QuerySingleOrDefaultAsync<Domain.CardPlacement>(
                @"
                    SELECT * FROM Placements 
                    WHERE Id = @Id",
                new { Id = id }
            );
            return placement;
        }

        public async Task UpdatePlacementAsync(Domain.CardPlacement placement)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    UPDATE Placements 
                    SET BoardId = @BoardId, 
                    ColumnId = @ColumnId, 
                    Position = @Position 
                    WHERE Id = @Id",
                placement
            );
        }

        public async Task DeletePlacementAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    DELETE FROM Placements 
                    WHERE Id = @Id",
                new { Id = id }
            );
        }
    }
}
