

namespace kanban_lia.Services.IntegrationEvents
{
    /// <summary>
    /// Publishes serialized integration events to external consumers.
    /// </summary>
    /// <remarks>Implementations take a fully serialized event (content plus event-type discriminator) together with
    /// the message identifier that consumers use for deduplication.</remarks>
    public interface IIntegrationEventPublisher
    {
        /// <summary>
        /// Publishes a serialized integration event asynchronously.
        /// </summary>
        /// <param name="messageId">Unique identifier of the message, used by consumers to detect duplicates.</param>
        /// <param name="eventType">Discriminator naming the type of the integration event.</param>
        /// <param name="content">The serialized event payload.</param>
        /// <param name="cancellationToken">A token used to cancel the publish operation.</param>
        /// <returns>A task that completes when the event has been published.</returns>
        Task PublishAsync(
            Guid messageId,
            string eventType,
            string content,
            CancellationToken cancellationToken);

        //Task PublishPlacementCreatedAsync(
        //    Guid entityId,
        //    Guid columnId,
        //    Guid? causationEventId,
        //    Actor actor,
        //    CancellationToken cancellationToken);

        //Task PublishColumnHasNoEdgeAsync(
        //    Guid columnId,
        //    Guid? causationEventId,
        //    Actor actor,
        //    CancellationToken cancellationToken);
    }
}
