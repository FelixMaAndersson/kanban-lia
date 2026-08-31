using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
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
                $@"
                    INSERT INTO {Schema.Boards.Table} 
                               ({Schema.Boards.Id},
                                {Schema.Boards.Title})
                    VALUES (@Id, @Title)",
                new
                {
                    board.Id.Id,
                    board.Title
                }
            );

            return new BoardId(id);
        }

        public async Task<Board?> GetByIdAsync(BoardId id)
        {
            using var connection = _connectionFactory.CreateConnection();

            var board = await connection.QuerySingleOrDefaultAsync<Board>(
                $@"
                    SELECT {Schema.Boards.Id}, 
                           {Schema.Boards.Title}
                      FROM {Schema.Boards.Table} 
                     WHERE {Schema.Boards.Id} = @Id",
                new
                {
                    id.Id
                }
            );

            return board;
        }

        public async Task<bool> RenameAsync(BoardId id, string title)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(
                 $@"
                    UPDATE {Schema.Boards.Table} 
                       SET {Schema.Boards.Title} = @Title
                     WHERE {Schema.Boards.Id}    = @Id",
                 new
                 {
                     id.Id,
                     Title = title
                 }
            );

            return rowsAffected > 0;
        }
        public async Task<bool> AddRootAsync(BoardId id, EntityId entityId)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    INSERT INTO {Schema.BoardRoots.Table}
                               ({Schema.BoardRoots.BoardId}, 
                                {Schema.BoardRoots.EntityId}) 
                    VALUES (@BoardId, @EntityId)",
                new
                {
                    BoardId = id.Id,
                    EntityId = entityId.Id
                }
            );

            return rowsAffected > 0;
        }

        public async Task<bool> RemoveRootAsync(BoardId id, EntityId entityId)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    DELETE FROM {Schema.BoardRoots.Table}
                          WHERE {Schema.BoardRoots.BoardId}  = @BoardId
                            AND {Schema.BoardRoots.EntityId} = @EntityId",
                new
                {
                    BoardId = id.Id,
                    EntityId = entityId.Id
                }
            );

            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(BoardId id)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    DELETE FROM {Schema.Boards.Table} 
                          WHERE {Schema.Boards.Id} = @Id",
                new
                {
                    id.Id
                }
            );

            return rowsAffected > 0;
        }

        public async Task<bool> BoardExistsAsync(BoardId id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                $@"
                    SELECT COUNT(1) 
                     FROM {Schema.Boards.Table}
                    WHERE {Schema.Boards.Id} = @Id",
                new
                {
                    id.Id
                }
            );

            return count > 0;
        }

        public async Task<bool> RootExistsAsync(BoardId boardId, EntityId entityId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                $@"
                    SELECT COUNT(1) 
                     FROM {Schema.BoardRoots.Table} 
                    WHERE {Schema.BoardRoots.BoardId}  = @BoardId 
                      AND {Schema.BoardRoots.EntityId} = @EntityId",
                new
                {
                    BoardId = boardId.Id,
                    EntityId = entityId.Id
                }
            );

            return count > 0;
        }
    }
}