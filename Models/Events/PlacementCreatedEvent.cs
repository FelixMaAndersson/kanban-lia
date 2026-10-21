using kanban_lia.Services.IntegrationEvents.Models;

namespace kanban_lia.Models.Events
{
    /// <summary>
    /// Record <c>PlacementCreatedEvent</c> is the SignalR event broadcast to board subscribers after placements are
    /// created.
    /// </summary>
    /// <remarks>Sent once per affected board group with the entities that were successfully placed, the column
    /// transitions involved, and the actor that caused the placement.</remarks>
    /// <param name="EntityIds">Identifiers of the entities whose placements were created successfully.</param>
    /// <param name="Changes">The column transitions performed, one per source-to-target move (or a single change with
    /// no source when an entity was placed for the first time).</param>
    /// <param name="Actor">The actor that caused the placements to be created.</param>
    public record PlacementCreatedEvent(
        Guid[] EntityIds,
        IEnumerable<PlacementChange> Changes,
        Actor Actor
    );
}