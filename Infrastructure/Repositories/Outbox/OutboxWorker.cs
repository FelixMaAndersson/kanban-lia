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
    public sealed class OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IIntegrationEventPublisher integrationEventPublisher)
        : BackgroundService
    {
        /// <summary>
        /// Runs the polling loop for the lifetime of the host.
        /// </summary>
        /// <remarks>Each iteration creates its own service scope, publishes every currently unprocessed message and
        /// then waits one second before polling again. Per-message errors are recorded via MarkAsFailedAsync and
        /// loop-level errors are written to the console; neither rethrows. The loop exits when
        /// <paramref name="stoppingToken"/> is canceled, which happens on host shutdown.</remarks>
        /// <param name="stoppingToken">Token canceled when the host is shutting down.</param>
        /// <returns>A task that completes when the loop has been canceled.</returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown by the final delay when <paramref name="stoppingToken"/> is canceled during shutdown.
        /// </exception>
        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            Console.WriteLine("[OutboxWorker] ExecuteAsync started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var outboxRepository =
                        scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                    var messages =
                        await outboxRepository.GetUnprocessedAsync(stoppingToken);

                    Console.WriteLine(
                        $"[OutboxWorker] Found {messages.Count()} unprocessed message(s).");

                    foreach (var message in messages)
                    {
                        try
                        {
                            Console.WriteLine(
                                $"[OutboxWorker] Publishing {message.Id} ({message.EventType}).");
                            await integrationEventPublisher.PublishAsync(
                                message.Id,
                                message.EventType,
                                message.Content,
                                stoppingToken);
                            Console.WriteLine(
                                $"[OutboxWorker] Publish completed for {message.Id}.");

                            await outboxRepository.MarkAsProcessedAsync(
                                message.Id,
                                stoppingToken);

                            Console.WriteLine(
                                $"[OutboxWorker] Marked {message.Id} as processed.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                $"[OutboxWorker] Failed to process {message.Id}: {ex}");

                            await outboxRepository.MarkAsFailedAsync(
                                message.Id,
                                ex.Message,
                                stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[OutboxWorker] Failed to process messages: {ex}");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);
            }
        }
    }
}