using System.Data;

namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    public interface IOutboxRepository
    {
        Task AddAsync(
            OutboxMessage message,
            IDbTransaction transaction,
            CancellationToken cancellationToken);

        Task <IEnumerable<OutboxMessage>> GetUnprocessedAsync(
            CancellationToken cancellationToken);

        Task MarkAsProcessedAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task MarkAsFailedAsync(
            Guid id,
            string error,
            CancellationToken cancellationToken);
    }
}
