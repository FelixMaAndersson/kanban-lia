using Dapper;

using kanban_lia.Domain;
using kanban_lia.Infrastructure.Database;

namespace kanban_lia.Infrastructure.Repositories
{
    public class ColumnRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        public ColumnRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreateColumnAsync(Column column)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    INSERT INTO Columns (Id, BoardId, Title, Position) 
                    VALUES (@Id, @BoardId, @Title, @Position)",
                column
            );
        }

        // Behöver vi lägga till en metod för att hämta alla kolumner, samt en metod för att hämta kolumner baserat på BoardId?
        public async Task<IEnumerable<Column>> GetAllColumnsAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var columns = await connection.QueryAsync<Column>(
                @"
                    SELECT * FROM Columns"
            );
            return columns;
        }

        public async Task<IEnumerable<Column>> GetColumnsByBoardIdAsync(Guid boardId)
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

        public async Task<Column?> GetColumnByIdAsync(Guid id)
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

        public async Task UpdateColumnAsync(Column column)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    UPDATE Columns 
                    SET Title = @Title, 
                    Position = @Position 
                    WHERE Id = @Id",
                column
            );
        }

        public async Task DeleteColumnAsync(Guid id)
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
