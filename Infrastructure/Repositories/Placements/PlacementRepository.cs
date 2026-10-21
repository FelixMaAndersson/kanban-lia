using Dapper;

using System.Data;

using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;

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
    /// <summary>
    /// Provides data access for placements using Dapper against SQL Server.
    /// </summary>
    /// <remarks>Placements are append-only move records; the current placement of an entity is the one with the
    /// newest timestamp. Mutating operations run inside a caller-supplied transaction and take the per-column sort-key
    /// application lock to serialize concurrent sort-key calculations. Reads that do not take part in a transaction
    /// open their own connection. When two placements of the same entity share a timestamp, which one counts as
    /// current is not deterministic; see the comment above this class.</remarks>
    /// <param name="connectionFactory">Factory that creates the database connections used for operations that do not
    /// take part in a caller-supplied transaction.</param>
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

        /// <summary>
        /// Inserts the given placements as new rows in the Placements table within the given transaction.
        /// </summary>
        /// <remarks>Every placement is appended as a separate history row; existing placements of the same entities
        /// are left untouched. The rows become visible only when the transaction commits.</remarks>
        /// <param name="placements">The placements to insert.</param>
        /// <param name="transaction">The active database transaction the inserts commit with.</param>
        /// <returns>A task that completes when the rows have been written.</returns>
        /// <exception cref="InvalidOperationException">The transaction has no connection.</exception>
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

        // see comments above about concurrent moves and tie-breakers
        /// <summary>
        /// Retrieves the current placement of each of the given entities on a board within an existing transaction.
        /// </summary>
        /// <remarks>For each entity, the placement with the newest timestamp on the given board is returned; entities
        /// with no placements on the board are omitted from the result.</remarks>
        /// <param name="entityIds">Identifiers of the entities whose current placements are retrieved.</param>
        /// <param name="boardId">Identifier of the board to filter placements by.</param>
        /// <param name="transaction">The active database transaction to run the query in.</param>
        /// <param name="cancellationToken">A token used to cancel the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current placement
        /// of each found entity.</returns>
        /// <exception cref="InvalidOperationException">The transaction has no connection.</exception>
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

        // see comments above about concurrent moves and tie-breakers
        /// <summary>
        /// Retrieves the current placement of each of the given entities on a board using a new connection.
        /// </summary>
        /// <remarks>For each entity, the placement with the newest timestamp on the given board is returned; entities
        /// with no placements on the board are omitted from the result.</remarks>
        /// <param name="entityIds">Identifiers of the entities whose current placements are retrieved.</param>
        /// <param name="boardId">Identifier of the board to filter placements by.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the current placement
        /// of each found entity.</returns>
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

        // see comments above about concurrent moves and tie-breakers
        /// <summary>
        /// Retrieves the entities currently placed in a column, ordered by sort key.
        /// </summary>
        /// <remarks>Only each entity's newest placement on the board is considered, and only entities whose current
        /// placement is in the given column are returned. Sort keys are compared with binary collation, so the order
        /// is the lexicographic order of the keys.</remarks>
        /// <param name="columnId">Identifier of the column whose current placements are retrieved.</param>
        /// <param name="boardId">Identifier of the board that contains the column.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the column's current
        /// placements ordered by sort key.</returns>
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
        // ALSO: see comments above about concurrent moves and tie-breakers
        /// <summary>
        /// Retrieves the current placements for a whole board, ordered by column and sort key.
        /// </summary>
        /// <remarks>Only each entity's newest placement is considered. The result is ordered by column identifier and
        /// then by sort key with binary collation. Filtering is done on the board identifier stored on the placement
        /// itself rather than by looking up the board through the column.</remarks>
        /// <param name="boardId">Identifier of the board whose current placements are retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the board's current
        /// placements ordered by column and sort key.</returns>
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

        // see comments above about concurrent moves and tie-breakers
        /// <summary>
        /// Retrieves the previous and next sort keys for inserting or moving an entity within a column asynchronously.
        /// </summary>
        /// <remarks>Only the latest placement per entity within the same board as the column is
        /// considered; sort-key comparisons use binary collation to determine ordering.</remarks>
        /// <param name="transaction">The active database transaction used for the query.</param>
        /// <param name="columnId">Identifier of the column whose current placements are evaluated.</param>
        /// <param name="lookup">Specifies lookup mode: end of column, after a specified entity, or before a specified entity.</param>
        /// <param name="afterEntityId">Optional identifier of the entity after which to locate the range; used when lookup selects the 'after'
        /// mode.</param>
        /// <param name="beforeEntityId">Optional identifier of the entity before which to locate the range; 
        /// used when lookup selects the 'before' mode.</param>
        /// <param name="cancellationToken">A token used to cancel the query.</param>
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