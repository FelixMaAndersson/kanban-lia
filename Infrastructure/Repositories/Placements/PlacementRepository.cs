using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using System.Data;

namespace kanban_lia.Infrastructure.Repositories.Placements

// Concurrent moves of the same entity to different columns can have identical
// timestamps. We have no business rule that determines which move should win,
// so we accept that either placement may be treated as current in this case.
// ROW_NUMBER still returns one row, but without a tie-breaker SQL Server does
// not guarantee which tied row it selects. If deterministic selection becomes
// a requirement, add a unique placement sequence and use it as a tie-breaker.
// Jocke thinks that ActorID is a good tie-breaker, but we don't have that in
// the placement model yet.
{
    public class PlacementRepository(DbConnectionFactory connectionFactory) : IPlacementRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;
        private const string MissingTransactionConnectionMessage = "The transaction has no connection.";

        /// <summary>
        /// Acquires an exclusive SQL Server application lock for the sort keys in the specified column.
        /// </summary>
        /// <remarks>
        /// The lock serializes operations that calculate or insert sort keys for the same column,
        /// provided they acquire the same lock. It is owned by the transaction and is released when
        /// that transaction commits or rolls back. SQL Server waits up to 15 seconds to grant the lock.
        /// </remarks>
        /// <param name="transaction">The active database transaction that will own the lock.</param>
        /// <param name="columnId">The ID of the column whose sort keys are being protected.</param>
        /// <param name="cancellationToken">A token used to cancel the lock request.</param>
        /// <returns>A task that completes when the lock has been acquired.</returns>
        /// <exception cref="InvalidOperationException">
        /// The transaction has no connection, or SQL Server reports that the lock could not be acquired.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// The operation is canceled through <paramref name="cancellationToken"/>.
        /// </exception>
        public async Task AcquireSortKeyLockAsync(
            IDbTransaction transaction,
            ColumnId columnId,
            CancellationToken cancellationToken)
        {
            var connection = transaction.Connection
                ?? throw new InvalidOperationException(
                    MissingTransactionConnectionMessage);

            const string sql = """
                DECLARE @LockResult int;
                EXEC @LockResult = sys.sp_getapplock
                    @Resource = @Resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 15000;
                SELECT @LockResult;
                """;

            var result = await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    sql,
                    new { Resource = $"PlacementSortKey:{columnId.Id:D}" },
                    transaction,
                    cancellationToken: cancellationToken));

            if (result < 0)
            {
                throw new InvalidOperationException(
                    $"Could not acquire sort-key lock for column '{columnId.Id}'. SQL Server returned {result}.");
            }
        }

        public async Task CreateAsync(
            IEnumerable<Placement> placements,
            IDbTransaction transaction)
        {
            var connection = transaction.Connection
                ?? throw new InvalidOperationException(
                    MissingTransactionConnectionMessage);

            var sql = $"""
                INSERT INTO {Schema.Placements.Table}
                (
                    {Schema.Placements.EntityId},
                    {Schema.Placements.BoardId},
                    {Schema.Placements.ColumnId},
                    {Schema.Placements.Timestamp},
                    {Schema.Placements.SortKey}
                )
                VALUES
                (
                    @EntityId,
                    @BoardId,
                    @ColumnId,
                    @Timestamp,
                    @SortKey
                );
                """;

            var parameters = placements.Select(p => new
            {
                EntityId = p.EntityId.Id,
                BoardId = p.BoardId.Id,
                ColumnId = p.ColumnId.Id,
                p.Timestamp,
                p.SortKey
            });

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    parameters,
                    transaction));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityIds"></param>
        /// <param name="boardId"></param>
        /// <param name="transaction"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<IEnumerable<Placement>> GetCurrentAsync(
            IEnumerable<EntityId> entityIds,
            BoardId boardId,
            IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            var connection = transaction.Connection
                ?? throw new InvalidOperationException(
                    MissingTransactionConnectionMessage);

            const string sql = $@"
                WITH CurrentPlacements AS
                (
                    SELECT
                        p.{Schema.Placements.EntityId},
                        p.{Schema.Placements.BoardId},
                        p.{Schema.Placements.ColumnId},
                        p.{Schema.Placements.SortKey},
                        p.{Schema.Placements.Timestamp},
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY p.{Schema.Placements.EntityId}
                            ORDER BY p.{Schema.Placements.Timestamp} DESC
                        ) AS rn
                    FROM {Schema.Placements.Table} AS p
                    WHERE p.{Schema.Placements.EntityId} IN @EntityIds
                      AND p.{Schema.Placements.BoardId} = @BoardId
                )
                SELECT
                    {Schema.Placements.EntityId},
                    {Schema.Placements.BoardId},
                    {Schema.Placements.ColumnId},
                    {Schema.Placements.SortKey},
                    {Schema.Placements.Timestamp}
                FROM CurrentPlacements
                WHERE rn = 1;
            ";

            return await connection.QueryAsync<Placement>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        EntityIds = entityIds.Select(entity => entity.Id).ToArray(),
                        BoardId = boardId.Id
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<Placement>> GetCurrentAsync(
            IEnumerable<EntityId> entityIds,
            BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = $@"
                WITH CurrentPlacements AS
                (
                    SELECT
                        p.{Schema.Placements.EntityId},
                        p.{Schema.Placements.BoardId},
                        p.{Schema.Placements.ColumnId},
                        p.{Schema.Placements.SortKey},
                        p.{Schema.Placements.Timestamp},
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY p.{Schema.Placements.EntityId}
                            ORDER BY p.{Schema.Placements.Timestamp} DESC
                        ) AS rn
                    FROM {Schema.Placements.Table} AS p
                    WHERE p.{Schema.Placements.EntityId} IN @EntityIds
                      AND p.{Schema.Placements.BoardId} = @BoardId
                )
                SELECT
                    {Schema.Placements.EntityId},
                    {Schema.Placements.BoardId},
                    {Schema.Placements.ColumnId},
                    {Schema.Placements.SortKey},
                    {Schema.Placements.Timestamp}
                FROM CurrentPlacements
                WHERE rn = 1;
            ";

            return await connection.QueryAsync<Placement>(
                sql, new
                {
                    EntityIds = entityIds.Select(entity => entity.Id).ToArray(),
                    BoardId = boardId.Id
                });
        }

        public async Task<IEnumerable<Placement>> GetCurrentByColumnAsync(
            ColumnId columnId,
            BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = $@"
                SELECT
                    cP.{Schema.Placements.EntityId},
                    cP.{Schema.Placements.BoardId},
                    cP.{Schema.Placements.ColumnId},
                    cP.{Schema.Placements.SortKey},
                    cP.{Schema.Placements.Timestamp}
                FROM
                (
                    SELECT
                        p.{Schema.Placements.EntityId},
                        p.{Schema.Placements.BoardId},
                        p.{Schema.Placements.ColumnId},
                        p.{Schema.Placements.SortKey},
                        p.{Schema.Placements.Timestamp},
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY p.{Schema.Placements.EntityId}
                            ORDER BY p.{Schema.Placements.Timestamp} DESC
                        ) AS rn
                    FROM {Schema.Placements.Table} AS p
                    WHERE p.{Schema.Placements.BoardId} = @BoardId
                ) AS cP
                WHERE cP.rn = 1
                  AND cP.{Schema.Placements.ColumnId} = @ColumnId
                ORDER BY cP.{Schema.Placements.SortKey} COLLATE Latin1_General_100_BIN2;
            ";

            return await connection.QueryAsync<Placement>(
               sql, new
               {
                   ColumnId = columnId.Id,
                   BoardId = boardId.Id
               });
        }

        //used to get it's boardID from column for the longest of time.
        //since we now have BoardID in placement, we can just use that to get all placements for a board.
        //if something crashes this change might be the bad guy,
        //but it should be fine since we are not using the columnID for anything else.
        public async Task<IEnumerable<Placement>> GetCurrentByBoardAsync(BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = $@"
                WITH CurrentPlacements AS
                (
                    SELECT
                        p.{Schema.Placements.EntityId},
                        p.{Schema.Placements.BoardId},
                        p.{Schema.Placements.ColumnId},
                        p.{Schema.Placements.SortKey},
                        p.{Schema.Placements.Timestamp},
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY p.{Schema.Placements.EntityId}
                            ORDER BY p.{Schema.Placements.Timestamp} DESC
                        ) AS rn
                    FROM {Schema.Placements.Table} AS p
                    WHERE p.{Schema.Placements.BoardId} = @Id
                )
                SELECT
                    EntityId,
                    BoardId,
                    ColumnId,
                    SortKey,
                    Timestamp
                FROM CurrentPlacements
                WHERE rn = 1
                ORDER BY
                    ColumnId,
                    SortKey COLLATE Latin1_General_100_BIN2;
            ";

            return await connection.QueryAsync<Placement>(
                sql,
                new
                {
                    boardId.Id
                });
        }

        /// <summary>
        /// Retrieves the previous and next sort keys for inserting or moving an entity within a column asynchronously.
        /// </summary>
        /// <remarks>Only the latest placement per entity within the same board as the column is
        /// considered; sort-key comparisons use binary collation to determine ordering.</remarks>
        /// <param name="columnId">Identifier of the column whose current placements are evaluated.</param>
        /// <param name="lookup">Specifies lookup mode: end of column, after a specified entity, or before a specified entity.</param>
        /// <param name="afterEntityId">Optional identifier of the entity after which to locate the range; used when lookup selects the 'after'
        /// mode.</param>
        /// <param name="beforeEntityId">Optional identifier of the entity before which to locate the range; 
        /// used when lookup selects the 'before' mode.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a SortKeyRange with the previous
        /// and next sort keys for the resolved position.</returns>
        public async Task<SortKeyRange> GetSortKeyRangeAsync(
            IDbTransaction transaction,
            ColumnId columnId,
            SortKeyLookup lookup,
            EntityId? afterEntityId = null,
            EntityId? beforeEntityId = null,
            CancellationToken cancellationToken = default)
        {
            var connection = transaction.Connection
                ?? throw new InvalidOperationException(MissingTransactionConnectionMessage);

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
                new CommandDefinition(
                    sql,
                    new
                    {
                        ColumnId = columnId.Id,
                        Lookup = (int)lookup,
                        AfterEntityId = afterEntityId?.Id,
                        BeforeEntityId = beforeEntityId?.Id
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }
    }
}