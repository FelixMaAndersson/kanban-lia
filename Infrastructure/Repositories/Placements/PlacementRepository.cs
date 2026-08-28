using Dapper;

using kanban_lia.Infrastructure.Database;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using kanban_lia.Infrastructure.Schemas;

namespace kanban_lia.Infrastructure.Repositories.Placements
{
    public class PlacementRepository(DbConnectionFactory connectionFactory) : IPlacementRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task CreateAsync(Placement placement)
        {
            using var connection = _connectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                $@"
                    INSERT INTO {Schema.Placements.Table} 
                    ({Schema.Placements.EntityId}, 
                     {Schema.Placements.ColumnId}, 
                     {Schema.Placements.Timestamp}, 
                     {Schema.Placements.SortKey}) 
                    VALUES (@EntityId, @ColumnId, @Timestamp, @SortKey)",
                new
                {
                    EntityId = placement.EntityId.Id,
                    ColumnId = placement.ColumnId.Id,
                    placement.Timestamp,
                    placement.SortKey
                }
            );
        }

        public async Task<Placement?> GetCurrentAsync(EntityId entityId, BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = $@"
                    SELECT TOP 1
                        p.{Schema.Placements.EntityId},
                        p.{Schema.Placements.ColumnId},
                        p.{Schema.Placements.SortKey},
                        p.{Schema.Placements.Timestamp}
                    FROM {Schema.Placements.Table} AS p
                    INNER JOIN {Schema.Columns.Table} AS c
                    ON p.{Schema.Placements.ColumnId} = c.{Schema.Columns.Id}
                    WHERE p.{Schema.Placements.EntityId} = @EntityId 
                    AND c.{Schema.Columns.BoardId} = @BoardId
                    ORDER BY p.{Schema.Placements.Timestamp} DESC
                ";

            return await connection.QuerySingleOrDefaultAsync<Placement>(
                sql, new
                {
                    EntityId = entityId.Id,
                    BoardId = boardId.Value
                });
        }

        public async Task<SortKeyRange> GetSortKeyRangeAsync(
    ColumnId columnId,
    SortKeyLookup lookup,
    EntityId? afterEntityId = null)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
        WITH TargetBoard AS
        (
            SELECT BoardId
            FROM Columns
            WHERE Id = @ColumnId
        ),
        CurrentPlacements AS
        (
            SELECT
                p.EntityId,
                p.ColumnId,
                p.SortKey,
                ROW_NUMBER() OVER
                (
                    PARTITION BY p.EntityId
                    ORDER BY p.Timestamp DESC
                ) AS rn
            FROM Placements p
            INNER JOIN Columns c
                ON p.ColumnId = c.Id
            INNER JOIN TargetBoard tb
                ON c.BoardId = tb.BoardId
        ),
        CurrentColumn AS
        (
            SELECT
                EntityId,
                SortKey
            FROM CurrentPlacements
            WHERE rn = 1
              AND ColumnId = @ColumnId
        )
        SELECT
            CASE
                WHEN @Lookup = 0 THEN NULL
                ELSE anchor.SortKey
            END AS Previous,

            CASE
                WHEN @Lookup = 0 THEN
                    (
                        SELECT TOP 1 SortKey
                        FROM CurrentColumn
                        ORDER BY SortKey COLLATE Latin1_General_100_BIN2 ASC
                    )
                ELSE
                    (
                        SELECT TOP 1 SortKey
                        FROM CurrentColumn
                        WHERE SortKey COLLATE Latin1_General_100_BIN2
                            > anchor.SortKey COLLATE Latin1_General_100_BIN2
                        ORDER BY SortKey COLLATE Latin1_General_100_BIN2 ASC
                    )
            END AS Next
        FROM
        (
            SELECT
                (
                    SELECT SortKey
                    FROM CurrentColumn
                    WHERE EntityId = @AfterEntityId
                ) AS SortKey
        ) anchor;
        """;

            return await connection.QuerySingleAsync<SortKeyRange>(
                sql,
                new
                {
                    ColumnId = columnId.Id,
                    Lookup = (int)lookup,
                    AfterEntityId = afterEntityId?.Id
                });
        }
    }
}