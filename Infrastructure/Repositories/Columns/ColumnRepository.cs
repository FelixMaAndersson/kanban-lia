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
                              {Schema.Columns.Position}) 
                    VALUES (@Id, @BoardId, @Title, @Position)",
                new
                {
                    Id = column.Id.Value,
                    BoardId = column.BoardId.Value,
                    column.Title,
                    column.Position
                }
            );

            return column;
        }

        // Behöver vi lägga till en metod för att hämta alla kolumner, samt en metod för att hämta kolumner baserat på BoardId?
        //public async Task<IEnumerable<Column>> GetAllAsync()
        //{
        //    using var connection = _connectionFactory.CreateConnection();
        //    var columns = await connection.QueryAsync<Column>(
        //        @"
        //            SELECT * FROM Columns"
        //    );
        //    return columns;
        //}

        public async Task<IEnumerable<Column>> GetByBoardIdAsync(BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var columns = await connection.QueryAsync<Column>(
                $@"
                    SELECT * FROM {Schema.Columns.Table} 
                            WHERE {Schema.Columns.BoardId} = @BoardId
                         ORDER BY {Schema.Columns.Position}",
                new
                {
                    BoardId = boardId
                }
            );

            return columns;
        }

        public async Task<Column?> GetByIdAsync(ColumnId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var column = await connection.QuerySingleOrDefaultAsync<Column>(
                $@"
                    SELECT * FROM {Schema.Columns.Table}
                            WHERE {Schema.Columns.Id} = @Id
                         ORDER BY {Schema.Columns.Position}",
                new
                {
                    Id = id
                }
            );

            return column;
        }

        public async Task<bool> RenameAsync(ColumnId id, string title)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    UPDATE {Schema.Columns.Table} 
                       SET {Schema.Columns.Title} = @Title
                     WHERE {Schema.Columns.Id}     = @Id",
                new
                {
                    Id = id,
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
                    Id = id
                }
            );

            return rowsAffected > 0;
        }
    }
}
