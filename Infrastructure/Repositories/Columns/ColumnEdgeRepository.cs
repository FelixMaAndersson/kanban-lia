using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.Repositories.Columns
{
    public class ColumnEdgeRepository(DbConnectionFactory connectionFactory) : IColumnEdgeRepository
    {

        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task CreateAsync(ColumnEdge columnEdge)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                $@"
                    INSERT INTO {Schema.ColumnEdges.Table} 
                               ({Schema.ColumnEdges.FromColumnId}, 
                                {Schema.ColumnEdges.ToColumnId}) 
                    VALUES (@FromColumnId, @ToColumnId)",
                new
                {
                    FromColumnId = columnEdge.FromColumnId.Id,
                    ToColumnId = columnEdge.ToColumnId.Id
                }
            );

        }
        public async Task<IEnumerable<ColumnEdge>> GetByBoardIdAsync(BoardId boardId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var edges = await connection.QueryAsync<ColumnEdge>(
                $@"
                    SELECT ce.* FROM {Schema.ColumnEdges.Table} ce
                    JOIN {Schema.Columns.Table} c1 ON ce.{Schema.ColumnEdges.FromColumnId} = c1.{Schema.Columns.Id}
                    JOIN {Schema.Columns.Table} c2 ON ce.{Schema.ColumnEdges.ToColumnId} = c2.{Schema.Columns.Id}
                    WHERE c1.{Schema.Columns.BoardId} = @BoardId OR c2.{Schema.Columns.BoardId} = @BoardId",
                new { BoardId = boardId.Id }
            );
            return edges;
        }

        public async Task<IEnumerable<ColumnEdge>> GetByFromColumnIdAsync(ColumnId fromColumnId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var edges = await connection.QueryAsync<ColumnEdge>(
                $@"
                    SELECT * FROM {Schema.ColumnEdges.Table} 
                    WHERE {Schema.ColumnEdges.FromColumnId} = @FromColumnId",
                new { FromColumnId = fromColumnId.Id }
            );
            return edges;
        }

        public async Task<IEnumerable<ColumnEdge>> GetByToColumnIdAsync(ColumnId toColumnId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var edges = await connection.QueryAsync<ColumnEdge>(
                $@"
                    SELECT * FROM {Schema.ColumnEdges.Table} 
                    WHERE {Schema.ColumnEdges.ToColumnId} = @ToColumnId",
                new { ToColumnId = toColumnId.Id }
            );
            return edges;
        }

        public async Task<bool> DeleteAsync(ColumnEdge columnEdge)
        {
            using var connection = _connectionFactory.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                $@"
                    DELETE FROM {Schema.ColumnEdges.Table} 
                    WHERE {Schema.ColumnEdges.FromColumnId} = @FromColumnId 
                    AND {Schema.ColumnEdges.ToColumnId} = @ToColumnId",
                new
                {
                    FromColumnId = columnEdge.FromColumnId.Id,
                    ToColumnId = columnEdge.ToColumnId.Id
                }
            );
            return rowsAffected > 0;
        }
    }
}
