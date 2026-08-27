namespace kanban_lia.Models.Domain.Placements.Exceptions
{
    public class InvalidPlacementPositionException(string position) : Exception($"Invalid placement position: '{position}'.");
       
}
