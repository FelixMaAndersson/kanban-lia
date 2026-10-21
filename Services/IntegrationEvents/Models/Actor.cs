namespace kanban_lia.Services.IntegrationEvents.Models
{
    /// <summary>
    /// Record <c>Actor</c> identifies the person or automation that caused an integration event.
    /// </summary>
    /// <remarks>Built from the Actor-Id and Actor-Type request headers when the caller supplies them; otherwise the
    /// endpoints fall back to the id "-1" and an empty type, so consumers must tolerate those placeholder values.
    /// The actor travels with every published event and is broadcast with placement changes so clients can attribute
    /// changes to their origin. Value-based record equality applies.</remarks>
    /// <param name="Id">Identifier of the actor; a user or automation id, or "-1" when the request carried none.</param>
    /// <param name="Type">Kind of actor, such as "User" or "Automation"; empty when the request carried none.</param>
    public record Actor(
        string Id,
        string Type);
}
