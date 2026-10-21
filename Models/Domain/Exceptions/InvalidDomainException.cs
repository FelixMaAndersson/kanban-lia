namespace kanban_lia.Models.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a domain invariant is violated, e.g. an empty or too-long column title.
    /// </summary>
    /// <remarks>Raised by domain entities while validating invariants; the API's exception middleware maps it to an
    /// HTTP 400 response with the message as the error detail.</remarks>
    /// <param name="message">A description of the invariant that was violated.</param>
    public class InvalidDomainException(string message)
        : Exception(message);
}