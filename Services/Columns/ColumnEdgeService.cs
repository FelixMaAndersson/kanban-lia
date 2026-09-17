using AutoMapper;
using kanban_lia.Infrastructure.Repositories.Columns;
using kanban_lia.Models.Domain.Columns;
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
        public async Task<IEnumerable<ColumnEdge>> GetByFromColumnIdAsync(ColumnId fromColumnId)
        {
            return await _repository.GetByFromColumnIdAsync(fromColumnId);
        }
        public async Task<IEnumerable<ColumnEdge>> GetByToColumnIdAsync(ColumnId toColumnId)
        {
            return await _repository.GetByToColumnIdAsync(toColumnId);
        }
        public async Task<bool> DeleteAsync(ColumnEdge edge)
        {
            return await _repository.DeleteAsync(edge);
        }
    }
}
