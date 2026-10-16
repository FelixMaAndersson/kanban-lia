using AutoMapper;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;
using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Services.Columns
{
    public class ColumnService(
        IColumnRepository repository,
        IColumnEdgeRepository columnEdgeRepository,
        IMapper mapper) : IColumnService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IColumnRepository _repository = repository;
        private readonly IColumnEdgeRepository _columnEdgeRepository = columnEdgeRepository;

        public async Task CreateAsync(CreateColumnDto dto, Guid correlationId, Actor actor, CancellationToken cancellationToken)
        {
            var newColumn = Column.Create(dto.Id, dto.BoardId, dto.Title, dto.Position, dto.RequestWritable);

            await _repository.CreateAsync(newColumn);

            await _columnEdgeRepository.GetByFromColumnIdAsync(newColumn.Id);

            await _columnEdgeRepository.GetByToColumnIdAsync(newColumn.Id);

            //if (!fromEdges.Any() && !toEdges.Any())
            //{
            //    await _integrationEventPublisher.PublishColumnHasNoEdgeAsync(newColumn.Id.Id, causationEventId, actor, cancellationToken);
            //}
        }

        public async Task<IEnumerable<ColumnDto>> GetByBoardIdAsync(BoardId boardId)
        {
            var columns = await _repository.GetByBoardIdAsync(boardId);

            return columns.Select(_mapper.Map<ColumnDto>);
        }

        public async Task<ColumnDto?> GetByIdAsync(ColumnId id)
        {
            var column = await _repository.GetByIdAsync(id);

            if (column is null)
            {
                throw new ColumnNotFoundException(id);
            }

            return _mapper.Map<ColumnDto>(column);
        }

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