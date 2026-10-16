using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.Repositories.Columns
{
    public class ColumnRepository(DbConnectionFactory connectionFactory) : IColumnRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

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

        public async Task<bool> DeleteAsync(ColumnId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    DELETE FROM {Schema.Columns.Table} 
                          WHERE {Schema.Columns.Id} = @Id",
                new
                {
                    id.Id
                }
            );

            return rowsAffected > 0;
        }

        #pragma warning disable S1144 // Setters are required for Dapper to map the results to the object
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
