namespace kanban_lia.Services.Boards.Exceptions
{
    public class InvalidBoardException : Exception
    {
        public InvalidBoardException(string message)
            : base(message)
        {
        }
    }
}
