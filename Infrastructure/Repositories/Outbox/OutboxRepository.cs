using Dapper;

using System.Data;

using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;

namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    /// <summary>
    /// Dapper-based repository for the transactional outbox that buffers integration events for later publishing.
    /// </summary>
    /// <remarks>Messages are written with <see cref="AddAsync"/> inside the same transaction as the business change
    /// that produced them, guaranteeing that a committed change always has a corresponding message. A background
    /// publisher then reads unprocessed messages with <see cref="GetUnprocessedAsync"/> and marks them processed or
    /// failed. Failed messages are retried until they reach the maximum attempt count, after which they are treated as
    /// processed to stop further retries.</remarks>
    /// <param name="connectionFactory">Factory that provides connections to the kanban database.</param>
    public class OutboxRepository(DbConnectionFactory connectionFactory) : IOutboxRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        private const int MaxAttempts = 5;

        /// <summary>
        /// Writes an outbox message using the caller's transaction so it commits or rolls back with the business
        /// change.
        /// </summary>
        /// <remarks>The message is inserted with its processed timestamp, error, and retry count left unset; those
        /// columns are maintained later by <see cref="MarkAsProcessedAsync"/> and <see cref="MarkAsFailedAsync"/>.
        /// The transaction's connection is used, so this method does not open a new one.</remarks>
        /// <param name="message">The message to persist; its id, event type, content, and occurrence time are
        /// written.</param>
        /// <param name="transaction">The transaction that owns both the business change and the outbox insert.</param>
        /// <param name="cancellationToken">A token used to cancel the insert.</param>
        /// <returns>A task that completes when the message has been written within the transaction.</returns>
        public async Task AddAsync(
        OutboxMessage message,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
        {
            const string sql = $"""
            INSERT INTO {Schema.OutBoxMessages.Table}
            (
                {Schema.OutBoxMessages.Id},
                {Schema.OutBoxMessages.EventType},
                {Schema.OutBoxMessages.Content},
                {Schema.OutBoxMessages.OccurredOn}
            )
            VALUES
            (
                @Id,
                @EventType,
                @Content,
                @OccurredOn
            );
            """;

            await transaction.Connection!.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    message,
                    transaction,
                    cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Retrieves all outbox messages that have not yet been processed, in the order they occurred.
        /// </summary>
        /// <remarks>A message counts as unprocessed while its processed timestamp is null; this includes messages
        /// that have failed but still have retry attempts left. Ordering by occurrence time lets publishers deliver
        /// events in the sequence they were produced.</remarks>
        /// <param name="cancellationToken">A token used to cancel the query.</param>
        /// <returns>A task whose result is the unprocessed messages ordered by <c>OccurredOn</c>, including their
        /// error and retry information.</returns>
        public async Task<IEnumerable<OutboxMessage>> GetUnprocessedAsync(
            CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = $"""
            SELECT
                {Schema.OutBoxMessages.Id},
                {Schema.OutBoxMessages.EventType},
                {Schema.OutBoxMessages.Content},
                {Schema.OutBoxMessages.OccurredOn},
                {Schema.OutBoxMessages.ProcessedOn},
                {Schema.OutBoxMessages.Error},
                {Schema.OutBoxMessages.RetryCount}
            FROM {Schema.OutBoxMessages.Table}
            WHERE {Schema.OutBoxMessages.ProcessedOn} IS NULL
            ORDER BY {Schema.OutBoxMessages.OccurredOn};
            """;

            var rows = await connection.QueryAsync<OutboxMessageRow>(
                new CommandDefinition(
                    sql,
                    cancellationToken: cancellationToken));

            return rows.Select(row => new OutboxMessage(
                row.Id,
                row.EventType,
                row.Content,
                row.OccurredOn,
                row.ProcessedOn,
                row.Error,
                row.RetryCount));
        }

        /// <summary>
        /// Marks an outbox message as processed by stamping it with the current UTC time.
        /// </summary>
        /// <remarks>Processed messages are excluded from subsequent <see cref="GetUnprocessedAsync"/> reads. The
        /// update is unconditional: it applies even if the message was already processed or does not exist.</remarks>
        /// <param name="id">The identifier of the message to mark as processed.</param>
        /// <param name="cancellationToken">A token used to cancel the update.</param>
        /// <returns>A task that completes when the update has been executed.</returns>
        public async Task MarkAsProcessedAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = $"""
            UPDATE {Schema.OutBoxMessages.Table}
            SET {Schema.OutBoxMessages.ProcessedOn} = @ProcessedOn
            WHERE {Schema.OutBoxMessages.Id} = @Id;
            """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = id,
                        ProcessedOn = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Records a publishing failure for an outbox message, incrementing its retry count and giving up once the
        /// maximum number of attempts is reached.
        /// </summary>
        /// <remarks>Only unprocessed messages are affected. Each failure stores the error text and increments the
        /// retry count; when the count reaches <c>MaxAttempts</c> (5) the message is stamped with the current UTC time
        /// so it is no longer picked up, otherwise it stays unprocessed and remains eligible for retry.</remarks>
        /// <param name="id">The identifier of the message that failed to publish.</param>
        /// <param name="error">The error text to store on the message.</param>
        /// <param name="cancellationToken">A token used to cancel the update.</param>
        /// <returns>A task that completes when the update has been executed.</returns>
        public async Task MarkAsFailedAsync(
            Guid id,
            string error,
            CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql =
                $"""
                UPDATE {Schema.OutBoxMessages.Table}
                SET {Schema.OutBoxMessages.Error} = @Error,
                    {Schema.OutBoxMessages.RetryCount} = {Schema.OutBoxMessages.RetryCount} + 1,
                    {Schema.OutBoxMessages.ProcessedOn} = CASE
                        WHEN {Schema.OutBoxMessages.RetryCount} + 1 >= @MaxAttempts THEN @ProcessedOn
                        ELSE NULL
                     END
                 WHERE {Schema.OutBoxMessages.Id} = @Id
                  AND {Schema.OutBoxMessages.ProcessedOn} IS NULL;
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = id,
                        Error = error,
                        MaxAttempts,
                        ProcessedOn = DateTime.UtcNow
                    },
                    cancellationToken: cancellationToken));
        }
        // Dapper row shape for OutBoxMessages, mapped back into OutboxMessage records by GetUnprocessedAsync.
#pragma warning disable S1144
        private sealed class OutboxMessageRow
        {
            public Guid Id { get; set; }
            public string EventType { get; set; } = null!;
            public string Content { get; set; } = null!;
            public DateTime OccurredOn { get; set; }
            public DateTime? ProcessedOn { get; set; }
            public string? Error { get; set; }
            public int RetryCount { get; set; }
        }
#pragma warning restore S1144
    }
}
