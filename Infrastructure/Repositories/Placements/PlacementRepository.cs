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
                    BoardId = boardId.Id
                });
        }

        public async Task<SortKeyRange> GetSortKeyRangeAsync(
            ColumnId columnId,
            SortKeyLookup lookup,
            EntityId? afterEntityId = null,
            EntityId? beforeEntityId = null)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = $@"
        WITH TargetBoard AS
        (
            SELECT {Schema.Columns.BoardId}
            FROM {Schema.Columns.Table}
            WHERE {Schema.Columns.Id} = @ColumnId
        ),
        CurrentPlacements AS
        (
            SELECT
                p.{Schema.Placements.EntityId},
                p.{Schema.Placements.ColumnId},
                p.{Schema.Placements.SortKey},
                ROW_NUMBER() OVER
                (
                    PARTITION BY p.{Schema.Placements.EntityId}
                    ORDER BY p.{Schema.Placements.Timestamp} DESC
                ) AS rn
            FROM {Schema.Placements.Table} p
            INNER JOIN {Schema.Columns.Table} c
                ON p.{Schema.Placements.ColumnId} = c.{Schema.Columns.Id}
            INNER JOIN TargetBoard tb
                ON c.{Schema.Columns.BoardId} = tb.{Schema.Columns.BoardId}
        ),
        CurrentColumn AS
        (
            SELECT
                {Schema.Placements.EntityId},
                {Schema.Placements.SortKey}
            FROM CurrentPlacements
            WHERE rn = 1
              AND {Schema.Placements.ColumnId} = @ColumnId
        ),
        Anchors AS
        (
            SELECT
                (
                    SELECT {Schema.Placements.SortKey}
                    FROM CurrentColumn
                    WHERE {Schema.Placements.EntityId} = @AfterEntityId
                ) AS AfterSortKey,

                (
                    SELECT {Schema.Placements.SortKey}
                    FROM CurrentColumn
                    WHERE {Schema.Placements.EntityId} = @BeforeEntityId
                ) AS BeforeSortKey
        )
        SELECT
            CASE
                WHEN @Lookup = 0 THEN
                    (
                        SELECT TOP 1 {Schema.Placements.SortKey}
                        FROM CurrentColumn
                        ORDER BY {Schema.Placements.SortKey} COLLATE Latin1_General_100_BIN2 DESC
                    )

                WHEN @Lookup = 1 THEN
                    AfterSortKey

                WHEN @Lookup = 2 THEN
                    (
                        SELECT TOP 1 {Schema.Placements.SortKey}
                        FROM CurrentColumn
                        WHERE {Schema.Placements.SortKey} COLLATE Latin1_General_100_BIN2
                            < BeforeSortKey COLLATE Latin1_General_100_BIN2
                        ORDER BY {Schema.Placements.SortKey} COLLATE Latin1_General_100_BIN2 DESC
                    )
            END AS Previous,

            CASE
                WHEN @Lookup = 0 THEN
                    NULL

                WHEN @Lookup = 1 THEN
                    (
                        SELECT TOP 1 {Schema.Placements.SortKey}
                        FROM CurrentColumn
                        WHERE {Schema.Placements.SortKey} COLLATE Latin1_General_100_BIN2
                            > AfterSortKey COLLATE Latin1_General_100_BIN2
                        ORDER BY {Schema.Placements.SortKey} COLLATE Latin1_General_100_BIN2 ASC
                    )

                WHEN @Lookup = 2 THEN
                    BeforeSortKey
            END AS Next
        FROM Anchors;
    ";

            return await connection.QuerySingleAsync<SortKeyRange>(
                sql,
                new
                {
                    ColumnId = columnId.Id,
                    Lookup = (int)lookup,
                    AfterEntityId = afterEntityId?.Id,
                    BeforeEntityId = beforeEntityId?.Id
                });
        }
    }
}