using Dapper;

using kanban_lia.Infrastructure.Database;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public class PlacementRepository(DbConnectionFactory connectionFactory) : IPlacementRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task CreateAsync(Placement placement)
        {
            using var connection = _connectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                @"
                    INSERT INTO Placements 
                    (EntityId, ColumnId, Timestamp, SortKey) 
                    VALUES (@EntityId, @ColumnId, @Timestamp, @SortKey)",
                new
                {
                    EntityId = placement.EntityId.Value,
                    ColumnId = placement.ColumnId.Value,
                    placement.Timestamp,
                    placement.SortKey
                }
            );
        }

        public async Task<Placement?> GetCurrentAsync(EntityId entityId, BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();

 

            return await connection.QuerySingleOrDefaultAsync<Placement>(
                $@"
                    SELECT TOP 1
                        p.EntityId,
                        p.ColumnId,
                        p.SortKey,
                        p.Timestamp
                    FROM Placements AS p
                    INNER JOIN Columns AS c
                    ON p.ColumnId = c.Id
                    WHERE p.EntityId = @EntityId 
                    AND c.BoardId = @BoardId
                    ORDER BY Timestamp DESC
                ", new 
                {
                    EntityId = entityId.Value, 
                    BoardId = boardId.Value 
                });
        }

        public async Task<string?> GetCurrentSortKeyAsync(EntityId entityId, ColumnId columnId)
        {
            using var connection = _connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<string>(
               @"
                    WITH CurrentPlacement AS
                    (
                        SELECT TOP 1
                            ColumnId,
                            SortKey
                        FROM Placements
                        WHERE EntityId = @EntityId
                        ORDER BY Timestamp DESC
                    )
                    SELECT SortKey
                    FROM CurrentPlacement
                    WHERE ColumnId = @ColumnId;
                    ", new
                {
                    EntityId = entityId.Value,
                    ColumnId = columnId.Value
                });
        }

        public async Task<string?> GetNextSortKeyAsync(string sortKey, ColumnId columnId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = @"
            WITH CurrentPlacements AS
            (
                SELECT *,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY EntityId
                        ORDER BY Timestamp DESC
                    ) AS rn
                FROM Placements
            )
            SELECT TOP 1 SortKey
            FROM CurrentPlacements
            WHERE rn = 1
              AND ColumnId = @ColumnId
              AND SortKey COLLATE Latin1_General_100_BIN2
                > @SortKey COLLATE Latin1_General_100_BIN2
            ORDER BY SortKey COLLATE Latin1_General_100_BIN2 ASC
            ";

            return await connection.QuerySingleOrDefaultAsync<string>(
                sql,
                new
                {
                    ColumnId = columnId.Value,
                    SortKey = sortKey
                });
        }
        public async Task<string?> GetFirstSortKeyAsync(ColumnId columnId)
        {
            using var connection = _connectionFactory.CreateConnection();

            return await connection.QuerySingleOrDefaultAsync<string>(
                @"
                    WITH CurrentPlacements AS
                    (
                        SELECT *,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY EntityId
                                ORDER BY Timestamp DESC
                            ) AS rn
                        FROM Placements
                    )
                    SELECT TOP 1 SortKey
                    FROM CurrentPlacements
                    WHERE rn = 1
                      AND ColumnId = @ColumnId
                    ORDER BY SortKey COLLATE Latin1_General_100_BIN2 ASC;
                ",
                new
                {
                    ColumnId = columnId.Value
                });
        }
    }
}