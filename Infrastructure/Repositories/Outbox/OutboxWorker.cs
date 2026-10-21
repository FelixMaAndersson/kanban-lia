using kanban_lia.Services.IntegrationEvents;

namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    /// <summary>
    /// Background service that drains the outbox table and publishes pending integration events.
    /// </summary>
    /// <remarks>Messages are written to the outbox inside the same transaction as the business change, and this
    /// worker turns them into published events after commit: every second it resolves an <see cref="IOutboxRepository"/>
    /// from a fresh DI scope, loads all messages whose ProcessedOn is null in the order they occurred, publishes each
    /// through <see cref="IIntegrationEventPublisher"/>, then marks it processed. A message that fails to publish is
    /// marked failed with the error text and its retry count is incremented; it stays eligible for the next pass
    /// until the retry limit is reached, at which point the repository marks it processed to stop further attempts.
    /// Failures of an individual message never stop the pass, and failures of the pass itself are logged and
    /// swallowed, so the loop keeps polling until the host is stopped.</remarks>
    /// <param name="scopeFactory">Factory used to create a fresh service scope on every polling pass.</param>
    /// <param name="integrationEventPublisher">Publisher used to deliver each unprocessed outbox message.</param>
    /// <param name="logger">Logger used for start-up, per-message progress and failure reporting.</param>
    public sealed class OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IIntegrationEventPublisher integrationEventPublisher,
        ILogger<OutboxWorker> logger)
        : BackgroundService
    {
        /// <summary>
        /// Runs the polling loop for the lifetime of the host.
        /// </summary>
        /// <remarks>Each iteration creates its own service scope, publishes every currently unprocessed message and
        /// then waits one second before polling again. Per-message errors are recorded via MarkAsFailedAsync and
        /// loop-level errors are logged; neither rethrows. The loop exits when
        /// <paramref name="stoppingToken"/> is canceled, which happens on host shutdown.</remarks>
        /// <param name="stoppingToken">Token canceled when the host is shutting down.</param>
        /// <returns>A task that completes when the loop has been canceled.</returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown by the final delay when <paramref name="stoppingToken"/> is canceled during shutdown.
        /// </exception>
        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            logger.LogInformation("[OutboxWorker] ExecuteAsync started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var outboxRepository =
                        scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                    var messages =
                        await outboxRepository.GetUnprocessedAsync(stoppingToken);

                    logger.LogInformation(
                        "[OutboxWorker] Found {MessageCount} unprocessed message(s).",
                        messages.Count());

                    foreach (var message in messages)
                    {
                        try
                        {
                            logger.LogInformation(
                                "[OutboxWorker] Publishing {MessageId} ({EventType}).",
                                message.Id,
                                message.EventType);
                            await integrationEventPublisher.PublishAsync(
                                message.Id,
                                message.EventType,
                                message.Content,
                                stoppingToken);
                            logger.LogInformation(
                                "[OutboxWorker] Publish completed for {MessageId}.",
                                message.Id);

                            await outboxRepository.MarkAsProcessedAsync(
                                message.Id,
                                stoppingToken);

                            logger.LogInformation(
                                "[OutboxWorker] Marked {MessageId} as processed.",
                                message.Id);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(
                                ex,
                                "[OutboxWorker] Failed to process {MessageId}.",
                                message.Id);

                            await outboxRepository.MarkAsFailedAsync(
                                message.Id,
                                ex.Message,
                                stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "[OutboxWorker] Failed to process messages.");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);
            }
        }
    }
}