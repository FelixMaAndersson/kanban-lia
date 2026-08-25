using Dapper;

using kanban_lia.Domain;
using kanban_lia.Infrastructure.Database;

namespace kanban_lia.Infrastructure.Repositories
{
    public class BoardRepository
    {
        private readonly DbConnectionFactory _connectionFactory;

        public BoardRepository(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task CreateAsync(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    INSERT INTO Boards (Id, Title, TrackedRoots) 
                    VALUES (@Id, @Title, @TrackedRoots)",
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

        public async Task<Board?> GetByIdAsync(Guid id)
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

        public async Task UpdateAsync(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                @"
                    UPDATE Boards 
                    SET Title = @Title, 
                    TrackedRoots = @TrackedRoots 
                    WHERE Id = @Id",
                board
            );
        }

        public async Task DeleteAsync(Guid id)
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
