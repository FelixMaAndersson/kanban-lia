using Dapper;
using kanban_lia.Infrastructure.Database;
using kanban_lia.Infrastructure.Schemas;
using System.Data;

namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    public class OutboxRepository(DbConnectionFactory connectionFactory) : IOutboxRepository
    {
        private readonly DbConnectionFactory _connectionFactory = connectionFactory;

        public async Task AddAsync(
        OutboxMessage message,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
        {
            const string sql = $"""
            INSERT INTO OutboxMessages
            (
                {Schema.OutBox.Id},
                {Schema.OutBox.EventType},
                {Schema.OutBox.Content},
                {Schema.OutBox.OccurredOn}
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
                {Schema.OutBox.Id},
                {Schema.OutBox.EventType},
                {Schema.OutBox.Content},
                {Schema.OutBox.OccurredOn},
                {Schema.OutBox.ProcessedOn},
                {Schema.OutBox.Error}
            FROM {Schema.OutBox.OutBoxMessages}
            WHERE {Schema.OutBox.ProcessedOn} IS NULL
            ORDER BY {Schema.OutBox.OccurredOn};
            """;

            var messages = await connection.QueryAsync<OutboxMessage>(
                new CommandDefinition(
                    sql,
                    cancellationToken: cancellationToken));

            return [.. messages];
        }

        public async Task MarkAsProcessedAsync(
            Guid id, 
            CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = $"""
            UPDATE {Schema.OutBox.OutBoxMessages}
            SET {Schema.OutBox.ProcessedOn} = @ProcessedOn
            WHERE {Schema.OutBox.Id} = @Id;
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
            UPDATE {Schema.OutBox.OutBoxMessages}
            SET {Schema.OutBox.Error} = @Error
            WHERE {Schema.OutBox.Id} = @Id;
            """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = id,
                        Error = error
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
