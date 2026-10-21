using Dapper;

using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.Repositories.Boards
{
    /// <summary>
    /// Dapper-based repository for persisting and querying boards and their root entities.
    /// </summary>
    /// <remarks>Boards are stored in the Boards table and the entities rooted at a board in the BoardRoots table. Each
    /// method opens its own connection; the repository does not manage transactions. Deletes cascade manually: removing
    /// a board also removes the column edges that reference its columns.</remarks>
    /// <param name="connectionFactory">Factory that provides connections to the kanban database.</param>
    public class BoardRepository(DbConnectionFactory connectionFactory) : IBoardRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        /// <summary>
        /// Inserts a new board row with the id and title carried by the given aggregate.
        /// </summary>
        /// <remarks>Only the board's id and title are persisted; roots must be added separately with
        /// <see cref="AddRootAsync"/>. The caller is responsible for generating the board id beforehand.</remarks>
        /// <param name="board">The board aggregate to persist; its <c>Id</c> and <c>Title</c> are written to the
        /// database.</param>
        /// <returns>A task whose result is the identifier of the created board.</returns>
        public async Task<BoardId> CreateAsync(Board board)
        {
            using var connection = _connectionFactory.CreateConnection();

            await connection.ExecuteScalarAsync<Guid>(
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

            return board.Id;
        }

        /// <summary>
        /// Loads a board together with its root entities, rehydrating the full aggregate.
        /// </summary>
        /// <remarks>The board row is read first; when found, the entity ids from BoardRoots are added to the aggregate
        /// via <see cref="Board.AddRoot"/>. A missing board short-circuits the root lookup.</remarks>
        /// <param name="id">The identifier of the board to load.</param>
        /// <returns>A task whose result is the restored <see cref="Board"/>, or <see langword="null"/> when no board
        /// exists with the given id.</returns>
        public async Task<Board?> GetByIdAsync(BoardId id)
        {
            using var connection = _connectionFactory.CreateConnection();

            var boardData = await connection.QuerySingleOrDefaultAsync<(Guid Id, string Title)>(
                $@"
                    SELECT {Schema.Boards.Id}, 
                           {Schema.Boards.Title}
                      FROM {Schema.Boards.Table} 
                     WHERE {Schema.Boards.Id} = @Id",
                new
                {
                    id.Id
                });

            if (boardData == default)
            {
                return null;
            }

            var board = Board.Restore(new BoardId(boardData.Id), boardData.Title);

            var roots = await connection.QueryAsync<Guid>(
                $@"
                    SELECT {Schema.BoardRoots.EntityId}
                      FROM {Schema.BoardRoots.Table} 
                     WHERE {Schema.BoardRoots.BoardId} = @BoardId",
                new
                {
                    BoardId = id.Id
                });

            foreach (var root in roots)
            {
                board.AddRoot(new EntityId(root));
            }

            return board;
        }

        /// <summary>
        /// Updates the title of an existing board.
        /// </summary>
        /// <remarks>Validation of the new title (non-empty, at most 255 characters) happens in the
        /// <see cref="Board"/> aggregate before this method is called; this method only writes the value.</remarks>
        /// <param name="id">The identifier of the board to rename.</param>
        /// <param name="title">The new title to store.</param>
        /// <returns>A task whose result is <see langword="true"/> when a row was updated, or <see langword="false"/>
        /// when no board exists with the given id.</returns>
        public async Task<bool> RenameAsync(
            BoardId id,
            string title)
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
        /// <summary>
        /// Adds an entity as a root of the given board by inserting a BoardRoots row.
        /// </summary>
        /// <remarks>The repository performs no existence or duplicate checks; callers are expected to verify that the
        /// board exists and that the entity is not already a root before inserting.</remarks>
        /// <param name="id">The identifier of the board that gains the root.</param>
        /// <param name="entityId">The identifier of the entity to add as a root.</param>
        /// <returns>A task whose result is <see langword="true"/> when the root row was inserted; otherwise
        /// <see langword="false"/>.</returns>
        public async Task<bool> AddRootAsync(
            BoardId id,
            EntityId entityId)
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

        /// <summary>
        /// Removes an entity from the set of roots of the given board.
        /// </summary>
        /// <remarks>Only the BoardRoots association is deleted; the entity itself and any placements it has on the
        /// board are left untouched.</remarks>
        /// <param name="id">The identifier of the board that loses the root.</param>
        /// <param name="entityId">The identifier of the entity to remove as a root.</param>
        /// <returns>A task whose result is <see langword="true"/> when a row was deleted; <see langword="false"/> when
        /// the entity was not a root of the board.</returns>
        public async Task<bool> RemoveRootAsync(
            BoardId id,
            EntityId entityId)
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

        /// <summary>
        /// Deletes a board and the column edges that reference any of its columns.
        /// </summary>
        /// <remarks>Column edges are deleted explicitly first because their foreign keys have no cascade and would
        /// otherwise block the delete. Deleting the board row then cascades at the database level to the board's
        /// columns and roots, and through the columns to their placements. The result reflects the total rows affected
        /// by the explicit statements, so it is <see langword="true"/> when either the edges or the board row were
        /// removed.</remarks>
        /// <param name="id">The identifier of the board to delete.</param>
        /// <returns>A task whose result is <see langword="true"/> when any rows were affected; <see langword="false"/>
        /// when the board did not exist.</returns>
        public async Task<bool> DeleteAsync(BoardId id)
        {
            using var connection = _connectionFactory.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    DELETE FROM {Schema.ColumnEdges.Table}
                         WHERE {Schema.ColumnEdges.FromColumnId} IN (
                                   SELECT {Schema.Columns.Id}
                                     FROM {Schema.Columns.Table}
                                    WHERE {Schema.Columns.BoardId} = @Id)
                            OR {Schema.ColumnEdges.ToColumnId} IN (
                                   SELECT {Schema.Columns.Id}
                                     FROM {Schema.Columns.Table}
                                    WHERE {Schema.Columns.BoardId} = @Id);

                    DELETE FROM {Schema.Boards.Table} 
                          WHERE {Schema.Boards.Id} = @Id;",
                new
                {
                    id.Id
                }
            );

            return rowsAffected > 0;
        }

        /// <summary>
        /// Checks whether a board with the given identifier exists.
        /// </summary>
        /// <param name="id">The identifier to look up.</param>
        /// <returns>A task whose result is <see langword="true"/> when the board exists; otherwise
        /// <see langword="false"/>.</returns>
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

        /// <summary>
        /// Checks whether the given entity is registered as a root of the given board.
        /// </summary>
        /// <param name="id">The identifier of the board to check.</param>
        /// <param name="entityId">The identifier of the entity to look for among the board's roots.</param>
        /// <returns>A task whose result is <see langword="true"/> when the root exists; otherwise
        /// <see langword="false"/>.</returns>
        public async Task<bool> RootExistsAsync(BoardId id, EntityId entityId)
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
                    BoardId = id.Id,
                    EntityId = entityId.Id
                }
            );

            return count > 0;
        }
    }
}