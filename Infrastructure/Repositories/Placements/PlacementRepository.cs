using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public class PlacementRepository(DbConnectionFactory connectionFactory) : IPlacementRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task<Placement> CreateAsync(Placement placement)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteScalarAsync<Guid>(
                @"
                    INSERT INTO Placements 
                    (EntityId, ColumnId, Position) 
                    VALUES (@EntityId, @ColumnId, @Position)",
                placement
            );

            return placement;
        }

        public async Task<Placement?> GetCurrentAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            return await connection.QuerySingleOrDefaultAsync<Placement>(
                @"
                    SELECT TOP 1
                        p.EntityId
                        p.ColumnId
                        p.Timestamp
                    FROM Placements AS p
                    INNER JOIN Columns AS c
                    ON p.ColumnId = c.Id
                    WHERE p.EntityId = @EntityId 
                    AND c.BoardId = @BoardId
                    ORDER BY Timestamp DESC"
            );
        }
    }
}