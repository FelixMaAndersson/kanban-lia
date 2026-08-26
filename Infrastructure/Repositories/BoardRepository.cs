using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Models.Domain;

namespace kanban_lia.Infrastructure.Repositories
{
    public class BoardRepository(DbConnectionFactory connectionFactory) : IBoardRepository
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
        public Task AddRoot(BoardId id, EntityId entityId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.ExecuteAsync(
                @"
                    UPDATE Boards 
                    SET Roots = JSON_ARRAY_APPEND(Roots, '$', @EntityId)
                    WHERE Id = @Id",
                new { Id = id, EntityId = entityId }
            ); // AI genererad (mock)
        }

        public Task RemoveRoot(BoardId id, EntityId entityId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return connection.ExecuteAsync(
                @"
                    UPDATE Boards 
                    SET Roots = JSON_REMOVE(Roots, JSON_SEARCH(Roots, @EntityId))
                    WHERE Id = @Id",
                new { Id = id, EntityId = entityId }
            ); // AI genererad (mock)
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
