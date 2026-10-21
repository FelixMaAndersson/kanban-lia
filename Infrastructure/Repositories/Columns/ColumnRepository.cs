using Dapper;

using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.Repositories.Columns
{
    /// <summary>
    /// Provides data access for board columns using Dapper against SQL Server.
    /// </summary>
    /// <remarks>Each method opens its own connection from the factory. Deleting a column also removes the column
    /// edges that reference it, in the same batch of statements.</remarks>
    /// <param name="connectionFactory">Factory that creates the database connections used by this repository.</param>
    public class ColumnRepository(DbConnectionFactory connectionFactory) : IColumnRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        /// <summary>
        /// Inserts a new column row and returns the same column instance.
        /// </summary>
        /// <param name="column">The column to persist; its identifier, board, title, position, and write-access flag
        /// are written to the Columns table.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the persisted column.
        /// </returns>
        public async Task<Column> CreateAsync(Column column)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                $@"
                    INSERT INTO {Schema.Columns.Table} 
                               ({Schema.Columns.Id}, 
                                {Schema.Columns.BoardId}, 
                                {Schema.Columns.Title}, 
                                {Schema.Columns.Position}, 
                                {Schema.Columns.RequestWritable}) 
                    VALUES (@Id, @BoardId, @Title, @Position, @RequestWritable)",
                new
                {
                    column.Id.Id,
                    BoardId = column.BoardId.Id,
                    column.Title,
                    column.Position,
                    column.RequestWritable
                }
            );

            return column;
        }

        /// <summary>
        /// Retrieves all columns of a board ordered by their position.
        /// </summary>
        /// <param name="boardId">Identifier of the board whose columns are retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the board's columns
        /// in ascending position order.</returns>
        public async Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rows = await connection.QueryAsync<ColumnRow>(
                $@"
                    SELECT  {Schema.Columns.Id},
                            {Schema.Columns.BoardId},
                            {Schema.Columns.Title},
                            {Schema.Columns.Position},
                            {Schema.Columns.RequestWritable}
                    FROM    {Schema.Columns.Table}
                    WHERE   {Schema.Columns.BoardId} = @Id
                    ORDER BY {Schema.Columns.Position}",
                new
                {
                    boardId.Id
                });

            return rows.Select(row => Column.Restore(
                new ColumnId(row.Id),
                new BoardId(row.BoardId),
                row.Title,
                row.Position,
                row.RequestWritable));
        }

        /// <summary>
        /// Retrieves a single column by its identifier.
        /// </summary>
        /// <param name="id">Identifier of the column to find.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the column, or
        /// <see langword="null"/> if no column with the given identifier exists.</returns>
        public async Task<Column?> GetByIdAsync(ColumnId id)
        {
            using var connection = _connectionFactory.CreateConnection();

            var row = await connection.QuerySingleOrDefaultAsync<ColumnRow>(
                $@"
                    SELECT  {Schema.Columns.Id},
                            {Schema.Columns.BoardId},
                            {Schema.Columns.Title},
                            {Schema.Columns.Position},
                            {Schema.Columns.RequestWritable}
                    FROM    {Schema.Columns.Table}
                    WHERE   {Schema.Columns.Id} = @Id",
                new
                {
                    id.Id
                }
            );

            if (row is null)
            {
                return null;
            }

            return Column.Restore(
                new ColumnId(row.Id),
                new BoardId(row.BoardId),
                row.Title,
                row.Position,
                row.RequestWritable);
        }

        /// <summary>
        /// Reads the write-access flag of a column without loading the whole column.
        /// </summary>
        /// <param name="id">Identifier of the column to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the column's
        /// request-writable flag, or <see langword="null"/> if the column does not exist.</returns>
        public async Task<bool?> CanBeWrittenAsync(ColumnId id)
        {
            using var connection = _connectionFactory.CreateConnection();

            var requestWritable = await connection.QuerySingleOrDefaultAsync<bool?>(
                $@"
                    SELECT {Schema.Columns.RequestWritable}
                    FROM   {Schema.Columns.Table}
                    WHERE  {Schema.Columns.Id} = @Id",
                new { id.Id }
            );

            return requestWritable;
        }

        /// <summary>
        /// Updates the write-access flag of a column.
        /// </summary>
        /// <param name="id">Identifier of the column to update.</param>
        /// <param name="requestWritable">The new value of the column's request-writable flag.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if a
        /// row was updated; <see langword="false"/> if no column with the given identifier exists.</returns>
        public async Task<bool> SetRequestWritableAsync(
            ColumnId id,
            bool requestWritable)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    UPDATE {Schema.Columns.Table} 
                       SET {Schema.Columns.RequestWritable} = @RequestWritable
                     WHERE {Schema.Columns.Id}     = @Id",
                new
                {
                    id.Id,
                    RequestWritable = requestWritable
                }
            );
            return rowsAffected > 0;
        }

        /// <summary>
        /// Updates the title of a column.
        /// </summary>
        /// <param name="id">Identifier of the column to rename.</param>
        /// <param name="title">The column's new title.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if a
        /// row was updated; <see langword="false"/> if no column with the given identifier exists.</returns>
        public async Task<bool> RenameAsync(
            ColumnId id,
            string title)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    UPDATE {Schema.Columns.Table} 
                       SET {Schema.Columns.Title} = @Title
                     WHERE {Schema.Columns.Id}     = @Id",
                new
                {
                    id.Id,
                    Title = title
                }
            );

            return rowsAffected > 0;
        }

        /// <summary>
        /// Deletes a column together with the edges that reference it.
        /// </summary>
        /// <remarks>Both the column-edge cleanup and the column deletion are issued as a single batch; the caller is
        /// not given a separate indication of how many edges were removed.</remarks>
        /// <param name="id">Identifier of the column to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if a
        /// column row was deleted; <see langword="false"/> if no column with the given identifier exists.</returns>
        public async Task<bool> DeleteAsync(ColumnId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    DELETE FROM {Schema.ColumnEdges.Table}
                          WHERE {Schema.ColumnEdges.FromColumnId} = @Id
                             OR {Schema.ColumnEdges.ToColumnId} = @Id;

                    DELETE FROM {Schema.Columns.Table} 
                          WHERE {Schema.Columns.Id} = @Id;",
                new
                {
                    id.Id
                }
            );

            return rowsAffected > 0;
        }

#pragma warning disable S1144 // Setters are required for Dapper to map the results to the object
        /// <summary>
        /// Row type used by Dapper to materialize Columns table results.
        /// </summary>
        private sealed class ColumnRow
        {
            public Guid Id { get; set; }
            public Guid BoardId { get; set; }
            public string Title { get; set; } = string.Empty;
            public int Position { get; set; }
            public bool RequestWritable { get; set; }
        }
#pragma warning restore S1144
    }
}
