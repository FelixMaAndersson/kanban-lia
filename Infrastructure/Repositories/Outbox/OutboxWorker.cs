using kanban_lia.Services.IntegrationEvents;

namespace kanban_lia.Infrastructure.Repositories.Outbox
{
    public sealed class OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IIntegrationEventPublisher integrationEventPublisher)
        : BackgroundService
    {
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