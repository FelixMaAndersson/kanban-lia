using AutoMapper;

using kanban_lia.Infrastructure.Repositories.Boards;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Boards.Exceptions;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Services.Columns
{
    /// <summary>
    /// Application service for creating, querying, renaming, reconfiguring, and deleting board columns.
    /// </summary>
    /// <remarks>Coordinates the column, board, and column-edge repositories and maps domain columns to DTOs.
    /// Operations that cannot find their target throw a not-found exception rather than returning null, so callers
    /// can distinguish "missing" from "succeeded".</remarks>
    /// <param name="repository">Repository used to read and write columns.</param>
    /// <param name="boardRepository">Repository used to verify that a board exists before listing its columns.</param>
    /// <param name="columnEdgeRepository">Repository used to look up edges connected to newly created columns.</param>
    /// <param name="mapper">AutoMapper instance used to project columns to DTOs.</param>
    public class ColumnService(
        IColumnRepository repository,
        IBoardRepository boardRepository,
        IColumnEdgeRepository columnEdgeRepository,
        IMapper mapper) : IColumnService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IColumnRepository _repository = repository;
        private readonly IBoardRepository _boardRepository = boardRepository;
        private readonly IColumnEdgeRepository _columnEdgeRepository = columnEdgeRepository;

        /// <summary>
        /// Creates a new column on a board from the given specification and returns its identifier.
        /// </summary>
        /// <remarks>The column is validated through <see cref="Column.Create"/> (non-empty title of at most 255
        /// characters and a non-negative position) and persisted immediately. The <paramref name="correlationId"/>
        /// and <paramref name="actor"/> parameters are accepted for signature compatibility but are not currently
        /// used.</remarks>
        /// <param name="dto">The column specification: optional pre-set id, board id, title, position, and the
        /// request-writable flag.</param>
        /// <param name="correlationId">Correlation identifier of the request; currently unused.</param>
        /// <param name="actor">Actor performing the creation; currently unused.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>A task whose result is the identifier of the newly created column.</returns>
        /// <exception cref="kanban_lia.Models.Domain.Exceptions.InvalidDomainException">The title is empty, longer
        /// than 255 characters, or the position is negative.</exception>
        public async Task<ColumnId> CreateAsync(CreateColumnDto dto, Guid correlationId, Actor actor, CancellationToken cancellationToken)
        {
            var newColumn = Column.Create(dto.Id, dto.BoardId, dto.Title, dto.Position, dto.RequestWritable);

            await _repository.CreateAsync(newColumn);

            return newColumn.Id;
        }

        /// <summary>
        /// Returns the columns of a board, mapped to DTOs.
        /// </summary>
        /// <param name="boardId">Identifier of the board whose columns are requested.</param>
        /// <returns>A task whose result is the board's columns as <see cref="ColumnDto"/> instances.</returns>
        /// <exception cref="BoardNotFoundException">No board with the given identifier exists.</exception>
        public async Task<IEnumerable<ColumnDto>> GetByBoardIdAsync(BoardId boardId)
        {
            var boardExists = await _boardRepository.BoardExistsAsync(boardId);

            if (!boardExists)
            {
                throw new BoardNotFoundException(boardId);
            }

            var columns = await _repository.GetByBoardIdAsync(boardId);

            return columns.Select(_mapper.Map<ColumnDto>);
        }

        /// <summary>
        /// Returns a single column by its identifier.
        /// </summary>
        /// <param name="id">Identifier of the column to retrieve.</param>
        /// <returns>A task whose result is the column mapped to a <see cref="ColumnDto"/>.</returns>
        /// <exception cref="ColumnNotFoundException">No column with the given identifier exists.</exception>
        public async Task<ColumnDto?> GetByIdAsync(ColumnId id)
        {
            var column = await _repository.GetByIdAsync(id);

            if (column is null)
            {
                throw new ColumnNotFoundException(id);
            }

            return _mapper.Map<ColumnDto>(column);
        }

        /// <summary>
        /// Sets whether requests may write to the column.
        /// </summary>
        /// <param name="id">Identifier of the column to update.</param>
        /// <param name="requestWritable">The new value of the request-writable flag.</param>
        /// <returns>A task whose result is <see langword="true"/> once the flag has been updated.</returns>
        /// <exception cref="ColumnNotFoundException">No column with the given identifier was updated.</exception>
        public async Task<bool> SetRequestWritableAsync(
            ColumnId id,
            bool requestWritable)
        {
            var changed = await _repository.SetRequestWritableAsync(id, requestWritable);
            if (!changed)
            {
                throw new ColumnNotFoundException(id);
            }
            return changed;
        }

        /// <summary>
        /// Renames a column after validating the new title against the domain rules.
        /// </summary>
        /// <param name="dto">The rename instruction carrying the column id and the new title.</param>
        /// <returns>A task whose result is <see langword="true"/> once the column has been renamed.</returns>
        /// <exception cref="ColumnNotFoundException">No column with the given identifier exists, or the update
        /// affected no rows.</exception>
        /// <exception cref="kanban_lia.Models.Domain.Exceptions.InvalidDomainException">The new title is empty or
        /// longer than 255 characters.</exception>
        public async Task<bool> RenameAsync(RenameColumnDto dto)
        {
            var column = await _repository.GetByIdAsync(dto.Id);

            if (column is null)
            {
                throw new ColumnNotFoundException(dto.Id);
            }

            column.Rename(dto.NewTitle);

            var renamed = await _repository.RenameAsync(dto.Id, dto.NewTitle);

            if (!renamed)
            {
                throw new ColumnNotFoundException(dto.Id);
            }

            return renamed;
        }

        /// <summary>
        /// Deletes a column by its identifier.
        /// </summary>
        /// <param name="id">Identifier of the column to delete.</param>
        /// <returns>A task whose result is <see langword="true"/> once the column has been deleted.</returns>
        /// <exception cref="ColumnNotFoundException">No column with the given identifier was deleted.</exception>
        public async Task<bool> DeleteAsync(ColumnId id)
        {
            var deleted = await _repository.DeleteAsync(id);

            if (!deleted)
            {
                throw new ColumnNotFoundException(id);
            }

            return deleted;
        }
    }
}