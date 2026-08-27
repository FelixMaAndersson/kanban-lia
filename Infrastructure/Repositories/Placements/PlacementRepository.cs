using Dapper;

using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public class PlacementRepository(DbConnectionFactory connectionFactory) : IPlacementRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task CreateAsync(Placement placement)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteScalarAsync<Guid>(
                $@"
                    INSERT INTO {Schema.Placements.Table} 
                             ({Schema.Placements.EntityId}, 
                              {Schema.Placements.ColumnId}, 
                              {Schema.Placements.Position}, 
                              {Schema.Placements.Timestamp}) 
                    VALUES (@EntityId, @ColumnId, @Position, @Timestamp)",
                new
                {
                    EntityId = placement.EntityId.Value,
                    ColumnId = placement.ColumnId.Value,
                    placement.Position,
                    placement.Timestamp
                }
            );
        }

        public async Task<Placement?> GetCurrentAsync(Guid id)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            return await connection.QuerySingleOrDefaultAsync<Placement>(
                $@"
                    SELECT TOP 1
                        p.{Schema.Placements.EntityId},
                        p.{Schema.Placements.ColumnId},
                        p.{Schema.Placements.Timestamp}
                    FROM {Schema.Placements.Table} AS p
                    INNER JOIN {Schema.Columns.Table}    AS c
                          ON p.{Schema.Placements.ColumnId} = c.Id
                    WHERE p.{Schema.Placements.EntityId} = @EntityId 
                      AND c.{Schema.Columns.BoardId}     = @BoardId
                    ORDER BY p.{Schema.Placements.Timestamp} DESC",
                new
                {
                    EntityId = id,
                    BoardId = id
                }
            );
        }
    }
}