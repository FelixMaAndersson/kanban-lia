using Dapper;
using kanban_lia.Infrastructure.Database;
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
            const string sql = """
            INSERT INTO OutboxMessages
            (
                Id,
                EventType,
                Content,
                OccurredOn
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

            const string sql = """
            SELECT
                Id,
                EventType,
                Payload,
                OccurredOn,
                ProcessedOn,
                Error
            FROM OutboxMessages
            WHERE ProcessedOn IS NULL
            ORDER BY OccurredOn;
            """;

            var messages = await connection.QueryAsync<OutboxMessage>(
                new CommandDefinition(
                    sql,
                    cancellationToken: cancellationToken));

            return messages.ToList();
        }

        public async Task MarkAsProcessedAsync(
            Guid id, 
            CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            UPDATE OutboxMessages
            SET ProcessedOn = @ProcessedOn
            WHERE Id = @Id;
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

            const string sql = """
            UPDATE OutboxMessages
            SET Error = @Error
            WHERE Id = @Id;
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
