using kanban_lia.Models.Domain.Exceptions;

namespace kanban_lia.Models.Domain.Columns
{
    /// <summary>
    /// Immutable domain entity representing a directed edge from one column to another, describing the allowed
    /// direction of travel between two columns, possibly on different boards.
    /// </summary>
    /// <remarks>Edges are only created through <see cref="Create"/>, which rejects a column pointing at itself; the
    /// private constructor exists for that purpose alone. An edge may connect columns on different boards, which is
    /// what allows a placement move to be resolved across boards.</remarks>
    public class ColumnEdge
    {
        public ColumnId FromColumnId { get; }
        public ColumnId ToColumnId { get; }
        private ColumnEdge(
            Guid fromColumnId,
            Guid toColumnId)
        {
            FromColumnId = new ColumnId(fromColumnId);
            ToColumnId = new ColumnId(toColumnId);
        }

        /// <summary>
        /// Creates an edge that allows movement from one column to another.
        /// </summary>
        /// <param name="fromColumnId">Column the edge starts at.</param>
        /// <param name="toColumnId">Column the edge points to; must differ from <paramref name="fromColumnId"/>.</param>
        /// <returns>The new immutable edge.</returns>
        /// <exception cref="InvalidDomainException">
        /// <paramref name="fromColumnId"/> and <paramref name="toColumnId"/> are the same column, which would create a
        /// self-loop.
        /// </exception>
        public static ColumnEdge Create(
            ColumnId fromColumnId,
            ColumnId toColumnId)
        {
            if (fromColumnId.Id == toColumnId.Id)
            {
                throw new InvalidDomainException("Cannot create an edge from a column to itself");
            }
            return new ColumnEdge(fromColumnId.Id, toColumnId.Id);
        }
    }
}

