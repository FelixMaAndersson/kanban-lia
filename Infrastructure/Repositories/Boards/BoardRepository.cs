using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Boards
{
    public class BoardRepository(DbConnectionFactory connectionFactory) : IBoardRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task<BoardId> CreateAsync(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();

            var id = await connection.ExecuteScalarAsync<Guid>(
                @"
                    INSERT INTO Boards (Id, Title)
                    VALUES (@Id, @Title)",
                new
                {
                    Id = board.Id.Value,
                    board.Title
                }
            );

            return new BoardId(id);
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

        public async Task<bool> RenameAsync(BoardId id, string title)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                 @"
                    UPDATE Boards 
                    SET Title = @Title
                    WHERE Id = @Id",
                 new { Id = id, Title = title }
             );

            return rowsAffected > 0;
        }
        public async Task<bool> AddRootAsync(BoardId id, EntityId entityId)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(
                @"
                    INSERT INTO BoardRoots (BoardId, EntityId) 
                    VALUES (@BoardId, @EntityId)",
                new { BoardId = id, EntityId = entityId }
            );


            return rowsAffected > 0;
        }

        public async Task<bool> RemoveRootAsync(BoardId boardId, EntityId entityId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = @"
                DELETE FROM BoardRoots
                WHERE BoardId = @BoardId
                AND EntityId = @EntityId;
            ";

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                BoardId = boardId,
                EntityId = entityId
            });

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(BoardId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                @"
                    DELETE FROM Boards 
                    WHERE Id = @Id",
                new { Id = id }
            );
            return rowsAffected > 0;
        }
    }
}