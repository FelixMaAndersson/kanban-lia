using System.Data;

namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    /// <summary>
    /// Repository for the transactional outbox that stages integration events for asynchronous delivery.
    /// </summary>
    /// <remarks>Messages are written inside the same transaction as the domain changes they describe, then picked up
    /// and marked by a background dispatcher. A message is considered done when it has a processed timestamp; failed
    /// publishes record the error and increment a retry counter until a maximum attempt count is reached.</remarks>
    public interface IOutboxRepository
    {
        /// <summary>
        /// Stages an outbox message for sending by inserting it into the outbox table as part of the caller's
        /// transaction.
        /// </summary>
        /// <remarks>The insert is not visible to the dispatcher until the caller's transaction commits, and it is
        /// rolled back with it if the transaction fails, keeping events and state changes atomic.</remarks>
        /// <param name="message">The message to stage, supplying id, event type, content, and occurrence time.</param>
        /// <param name="transaction">The active transaction the insert participates in.</param>
        /// <param name="cancellationToken">A token used to cancel the insert.</param>
        /// <returns>A task that completes when the message has been inserted.</returns>
        Task AddAsync(
            OutboxMessage message,
            IDbTransaction transaction,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves every outbox message that has not yet been processed, in the order they occurred.
        /// </summary>
        /// <param name="cancellationToken">A token used to cancel the query.</param>
        /// <returns>A task whose result is the unprocessed messages ordered by occurrence time ascending; empty when
        /// nothing is pending.</returns>
        Task <IEnumerable<OutboxMessage>> GetUnprocessedAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Marks a message as successfully processed by stamping it with the current UTC time, so later polls skip
        /// it.
        /// </summary>
        /// <param name="id">Identifier of the message to mark as processed.</param>
        /// <param name="cancellationToken">A token used to cancel the update.</param>
        /// <returns>A task that completes when the message has been updated.</returns>
        Task MarkAsProcessedAsync(
            Guid id,
            CancellationToken cancellationToken);

        /// <summary>
        /// Records a delivery failure on an unprocessed message by storing the error and incrementing its retry
        /// count.
        /// </summary>
        /// <remarks>Once the retry count reaches the implementation's maximum attempt count, the message is also
        /// stamped as processed so it is not retried again; otherwise it stays unprocessed for a later attempt.</remarks>
        /// <param name="id">Identifier of the failed message.</param>
        /// <param name="error">The error text describing why delivery failed.</param>
        /// <param name="cancellationToken">A token used to cancel the update.</param>
        /// <returns>A task that completes when the failure has been recorded.</returns>
        Task MarkAsFailedAsync(
            Guid id,
            string error,
            CancellationToken cancellationToken);
    }
}
