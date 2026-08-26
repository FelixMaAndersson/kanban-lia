using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public class BoardRepository(DbConnectionFactory connectionFactory)
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task CreateAsync(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    INSERT INTO Boards (Id, Title, Roots) 
                    VALUES (@Id, @Title, @Roots)",
                board
            );
        }

        public async Task<IEnumerable<Board>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var boards = await connection.QueryAsync<Board>(
                @"
                    SELECT * FROM Boards"
            );
            return boards;
        }

        public async Task<Board?> GetByIdAsync(BoardId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var board = await connection.QuerySingleOrDefaultAsync<Board>(
                @"
                    SELECT * FROM Boards 
                    WHERE Id = @Id",
                new { Id = id }
            );
            return board;
        }

        public async Task RenameAsync(BoardId id, string title)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    UPDATE Boards 
                    SET Title = @Title
                    WHERE Id = @Id",
                new { Id = id, Title = title }
            );
        }

        public async Task DeleteAsync(BoardId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    DELETE FROM Boards 
                    WHERE Id = @Id",
                new { Id = id }
            );
        }
    }
}
