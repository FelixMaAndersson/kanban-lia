using AutoMapper;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;
using kanban_lia.Services.Columns.Exceptions;

namespace kanban_lia.Services.Columns
{
    public class ColumnService(IColumnRepository repository, IMapper mapper) : IColumnService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IColumnRepository _repository = repository;

        public async Task CreateAsync(CreateColumnDto dto)
        {
            var newColumn = Column.Create(dto.Id, dto.Title, dto.Position, dto.BoardId);

            await _repository.CreateAsync(newColumn);
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

        public async Task<bool> RenameAsync(RenameColumnDto dto)
        {
            var columnId = new ColumnId(dto.Id);

            var renamed = await _repository.RenameAsync(columnId, dto.NewTitle);

            if (!renamed)
            {
                throw new ColumnNotFoundException(columnId);
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