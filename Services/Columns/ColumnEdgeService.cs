using AutoMapper;

using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;

namespace kanban_lia.Services.Columns
{
    /// <summary>
    /// Application service for creating, querying, and deleting the edges that connect columns.
    /// </summary>
    /// <remarks>Column edges express which columns a placement may move between. Edge direction is
    /// normalised on creation so a pair of columns is stored exactly once regardless of the order the two ids
    /// are supplied in.</remarks>
    /// <param name="repository">Repository used to persist and query column edges.</param>
    /// <param name="mapper">Mapper used to convert <see cref="ColumnEdge"/> aggregates into
    /// <see cref="ColumnEdgeDto"/> results.</param>
    public class ColumnEdgeService(
        IColumnEdgeRepository repository,
        IMapper mapper) : IColumnEdgeService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IColumnEdgeRepository _repository = repository;

        /// <summary>
        /// Creates an edge between two columns.
        /// </summary>
        /// <remarks>The two column ids are ordered before the edge is built, so the edge from the
        /// lower-ordered id to the higher-ordered id is stored no matter which direction the caller supplied.
        /// Self-edges are rejected by <see cref="ColumnEdge.Create"/>.</remarks>
        /// <param name="dto">The edge request identifying the two columns to connect.</param>
        /// <returns>A task that completes when the edge has been stored.</returns>
        /// <exception cref="ColumnEdgeAlreadyExistsException">An edge between the two columns already
        /// exists.</exception>
        public async Task CreateAsync(CreateColumnEdgeDto dto)
        {
            var fromColumnId = dto.FromColumnId;
            var toColumnId = dto.ToColumnId;

            if (fromColumnId.Id.CompareTo(toColumnId.Id) > 0)
            {
                (fromColumnId, toColumnId) = (toColumnId, fromColumnId);
            }

            var newEdge = ColumnEdge.Create(fromColumnId, toColumnId);

            if (await _repository.ExistsAsync(newEdge))
            {
                throw new ColumnEdgeAlreadyExistsException(newEdge.FromColumnId, newEdge.ToColumnId);
            }

            await _repository.CreateAsync(newEdge);
        }
        /// <summary>
        /// Retrieves every edge that connects two columns of the given board.
        /// </summary>
        /// <param name="boardId">The board whose edges are fetched.</param>
        /// <returns>A task whose result is the board's column edges mapped to DTOs.</returns>
        public async Task<IEnumerable<ColumnEdgeDto>> GetByBoardIdAsync(BoardId boardId)
        {
            var columnEdges = await _repository.GetByBoardIdAsync(boardId);
            return columnEdges.Select(_mapper.Map<ColumnEdgeDto>);
        }
        /// <summary>
        /// Retrieves the edges that start at the given column.
        /// </summary>
        /// <param name="fromColumnId">The column whose outgoing edges are fetched.</param>
        /// <returns>A task whose result is the outgoing edges mapped to DTOs; empty when the column has no
        /// outgoing edges.</returns>
        public async Task<IEnumerable<ColumnEdgeDto>> GetByFromColumnIdAsync(ColumnId fromColumnId)
        {
            var columnEdges = await _repository.GetByFromColumnIdAsync(fromColumnId);
            return columnEdges.Select(_mapper.Map<ColumnEdgeDto>);
        }
        /// <summary>
        /// Retrieves the edges that end at the given column.
        /// </summary>
        /// <param name="toColumnId">The column whose incoming edges are fetched.</param>
        /// <returns>A task whose result is the incoming edges mapped to DTOs; empty when the column has no
        /// incoming edges.</returns>
        public async Task<IEnumerable<ColumnEdgeDto>> GetByToColumnIdAsync(ColumnId toColumnId)
        {
            var columnEdges = await _repository.GetByToColumnIdAsync(toColumnId);
            return columnEdges.Select(_mapper.Map<ColumnEdgeDto>);
        }
        /// <summary>
        /// Deletes the edge between the two columns identified by the given edge.
        /// </summary>
        /// <param name="edge">The edge to delete; both column ids must match the stored edge for it to be
        /// removed.</param>
        /// <returns>A task whose result is true when an edge was deleted, false when no matching edge
        /// exists.</returns>
        public async Task<bool> DeleteAsync(ColumnEdge edge)
        {
            return await _repository.DeleteAsync(edge);
        }
    }
}
