using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public class ColumnRepository(DbConnectionFactory connectionFactory) : IColumnRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task<Column> CreateAsync(Column column)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    INSERT INTO Columns (Id, BoardId, Title, Position) 
                    VALUES (@Id, @BoardId, @Title, @Position)",
                column
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
                @"
                    SELECT * FROM Columns 
                    WHERE BoardId = @BoardId
                    ORDER BY Position",
                new { BoardId = boardId }
            );
            return columns;
        }

        public async Task<Column?> GetByIdAsync(ColumnId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var column = await connection.QuerySingleOrDefaultAsync<Column>(
                @"
                    SELECT * FROM Columns 
                    WHERE Id = @Id
                    ORDER BY Position",
                new { Id = id }
            );
            return column;
        }

        public async Task RenameAsync(ColumnId id, string title)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    UPDATE Columns 
                    SET Title = @Title
                    WHERE Id = @Id",
                new { Id = id, Title = title }
            );
        }

        //Kanske ha en move method också??

        public async Task DeleteAsync(ColumnId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    DELETE FROM Columns 
                    WHERE Id = @Id",
                new { Id = id }
            );
        }
    }
}
