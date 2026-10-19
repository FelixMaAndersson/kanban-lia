using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using System.Data;

namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    public class OutboxRepository(DbConnectionFactory connectionFactory) : IOutboxRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        private const int MaxAttempts = 5;

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

        public async Task MarkAsFailedAsync(Guid id,
            string error,
            CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = $"""
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
