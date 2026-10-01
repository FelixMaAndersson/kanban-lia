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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var outboxRepository =
                        scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                    var messages =
                        await outboxRepository.GetUnprocessedAsync(stoppingToken);

                    foreach (var message in messages)
                    {
                        try
                        {
                            await integrationEventPublisher.PublishAsync(
                                message.EventType,
                                message.Content,
                                stoppingToken);

                            await outboxRepository.MarkAsProcessedAsync(
                                message.Id,
                                stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            await outboxRepository.MarkAsFailedAsync(
                                message.Id,
                                ex.Message,
                                stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ninja raid shadow legends. " + ex);
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);
            }
        }
    }
}