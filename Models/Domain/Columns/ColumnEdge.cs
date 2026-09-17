using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Columns
{
    public class ColumnEdge
    {
        public ColumnId FromColumnId { get; }
        public ColumnId ToColumnId { get; }
        private ColumnEdge(Guid fromColumnId, Guid toColumnId)
        {
            FromColumnId = new ColumnId(fromColumnId);
            ToColumnId = new ColumnId(toColumnId);
        }

        public static ColumnEdge Create(ColumnId fromColumnId, ColumnId toColumnId)
        {
            if (fromColumnId.Id == toColumnId.Id)
            {
                throw new InvalidDomainException("Cannot create an edge from a column to itself");
            }
            return new ColumnEdge(fromColumnId.Id, toColumnId.Id);
        }
    }
}

