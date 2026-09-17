using AutoMapper;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Boards;
using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Columns.DTOs;
using kanban_lia.Services.Columns.DTOs;

namespace kanban_lia.Services.Columns
{
    public class ColumnEdgeService(IColumnEdgeRepository repository, IMapper mapper) : IColumnEdgeService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IColumnEdgeRepository _repository = repository;

        public async Task CreateAsync(CreateColumnEdgeDto dto)
        {
            var newEdge = ColumnEdge.Create(dto.FromColumnId, dto.ToColumnId);
            await _repository.CreateAsync(newEdge);
        }
        public async Task<IEnumerable<ColumnEdgeDto>> GetByBoardIdAsync(BoardId boardId)
        {
            var columnEdges = await _repository.GetByBoardIdAsync(boardId);
            return columnEdges.Select(_mapper.Map<ColumnEdgeDto>);
        }
        public async Task<IEnumerable<ColumnEdgeDto>> GetByFromColumnIdAsync(ColumnId fromColumnId)
        {
            var columnEdges = await _repository.GetByFromColumnIdAsync(fromColumnId);
            return columnEdges.Select(_mapper.Map<ColumnEdgeDto>);
        }
        public async Task<IEnumerable<ColumnEdgeDto>> GetByToColumnIdAsync(ColumnId toColumnId)
        {
            var columnEdges = await _repository.GetByToColumnIdAsync(toColumnId);
            return columnEdges.Select(_mapper.Map<ColumnEdgeDto>);
        }
        public async Task<bool> DeleteAsync(ColumnEdge edge)
        {
            return await _repository.DeleteAsync(edge);
        }
    }
}
