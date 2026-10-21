using Dapper;

using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.Repositories.Columns
{
    /// <summary>
    /// Dapper repository for reading and writing column edges, which model connectivity between columns (including
    /// across boards).
    /// </summary>
    /// <remarks>Edges are treated as undirected for existence checks and deletes: a stored edge matches the requested
    /// column pair in either direction. Each method opens its own connection through
    /// <see cref="DbConnectionFactory"/>.</remarks>
    public class ColumnEdgeRepository(DbConnectionFactory connectionFactory) : IColumnEdgeRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        /// <summary>
        /// Inserts the given column edge into the database.
        /// </summary>
        /// <param name="columnEdge">The edge to insert; its FromColumnId and ToColumnId are stored as given.</param>
        /// <returns>A task that completes when the row has been inserted.</returns>
        public async Task CreateAsync(ColumnEdge columnEdge)
        {
            using var connection = _connectionFactory.CreateConnection();

            await connection.ExecuteAsync(
                $@"
            INSERT INTO {Schema.ColumnEdges.Table}
                       ({Schema.ColumnEdges.FromColumnId},
                        {Schema.ColumnEdges.ToColumnId})
            VALUES     (@FromColumnId, @ToColumnId)",
                new
                {
                    FromColumnId = columnEdge.FromColumnId.Id,
                    ToColumnId = columnEdge.ToColumnId.Id
                }
            );
        }

        /// <summary>
        /// Determines whether an edge connecting the two given columns already exists, in either direction.
        /// </summary>
        /// <param name="columnEdge">The candidate edge whose column pair is searched for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if a
        /// matching edge exists in either direction; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> ExistsAsync(ColumnEdge columnEdge)
        {
            using var connection = _connectionFactory.CreateConnection();

            var count = await connection.ExecuteScalarAsync<int>(
                $@"
                SELECT COUNT(*)
                FROM {Schema.ColumnEdges.Table}
                WHERE ({Schema.ColumnEdges.FromColumnId} = @FromColumnId
                   AND {Schema.ColumnEdges.ToColumnId} = @ToColumnId)
                   OR ({Schema.ColumnEdges.FromColumnId} = @ToColumnId
                   AND {Schema.ColumnEdges.ToColumnId} = @FromColumnId)",
                new
                {
                    FromColumnId = columnEdge.FromColumnId.Id,
                    ToColumnId = columnEdge.ToColumnId.Id
                });

            return count > 0;
        }
        /// <summary>
        /// Gets all edges that touch any column belonging to the given board.
        /// </summary>
        /// <param name="boardId">Identifier of the board whose columns are used to match either edge endpoint.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the matching edges.
        /// </returns>
        public async Task<IEnumerable<ColumnEdge>> GetByBoardIdAsync(BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();

            var edges = await connection.QueryAsync<ColumnEdge>(
                $@"
            SELECT  ce.{Schema.ColumnEdges.FromColumnId},
                    ce.{Schema.ColumnEdges.ToColumnId}
            FROM    {Schema.ColumnEdges.Table} AS ce
            JOIN    {Schema.Columns.Table} AS c1
                    ON ce.{Schema.ColumnEdges.FromColumnId} = c1.{Schema.Columns.Id}
            JOIN    {Schema.Columns.Table} AS c2
                    ON ce.{Schema.ColumnEdges.ToColumnId} = c2.{Schema.Columns.Id}
            WHERE   c1.{Schema.Columns.BoardId} = @Id
               OR   c2.{Schema.Columns.BoardId} = @Id",
                new
                {
                    boardId.Id
                }
            );

            return edges;
        }

        /// <summary>
        /// Gets all edges that originate from the given column.
        /// </summary>
        /// <param name="fromColumnId">Identifier of the originating column.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the edges whose
        /// FromColumnId matches.</returns>
        public async Task<IEnumerable<ColumnEdge>> GetByFromColumnIdAsync(ColumnId fromColumnId)
        {
            using var connection = _connectionFactory.CreateConnection();

            var edges = await connection.QueryAsync<ColumnEdge>(
                $@"
            SELECT  {Schema.ColumnEdges.FromColumnId},
                    {Schema.ColumnEdges.ToColumnId}
            FROM    {Schema.ColumnEdges.Table}
            WHERE   {Schema.ColumnEdges.FromColumnId} = @FromColumnId",
                new
                {
                    FromColumnId = fromColumnId.Id
                }
            );

            return edges;
        }

        /// <summary>
        /// Gets all edges that terminate at the given column.
        /// </summary>
        /// <param name="toColumnId">Identifier of the destination column.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the edges whose
        /// ToColumnId matches.</returns>
        public async Task<IEnumerable<ColumnEdge>> GetByToColumnIdAsync(ColumnId toColumnId)
        {
            using var connection = _connectionFactory.CreateConnection();

            var edges = await connection.QueryAsync<ColumnEdge>(
                $@"
            SELECT  {Schema.ColumnEdges.FromColumnId},
                    {Schema.ColumnEdges.ToColumnId}
            FROM    {Schema.ColumnEdges.Table}
            WHERE   {Schema.ColumnEdges.ToColumnId} = @ToColumnId",
                new
                {
                    ToColumnId = toColumnId.Id
                }
            );

            return edges;
        }

        /// <summary>
        /// Deletes the edge connecting the two given columns, in either direction.
        /// </summary>
        /// <param name="columnEdge">The edge to remove; a stored row matching either direction is deleted.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if a
        /// row was deleted; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> DeleteAsync(ColumnEdge columnEdge)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(
                $@"
                DELETE FROM {Schema.ColumnEdges.Table}
                WHERE {Schema.ColumnEdges.FromColumnId} = @FromColumnId AND {Schema.ColumnEdges.ToColumnId} = @ToColumnId
                   OR {Schema.ColumnEdges.FromColumnId} = @ToColumnId AND {Schema.ColumnEdges.ToColumnId} = @FromColumnId",
                new
                {
                    FromColumnId = columnEdge.FromColumnId.Id,
                    ToColumnId = columnEdge.ToColumnId.Id
                }
            );

            return rowsAffected > 0;
        }
    }
}
