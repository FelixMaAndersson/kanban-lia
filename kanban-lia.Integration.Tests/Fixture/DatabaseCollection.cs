namespace kanban_lia.Integration.Tests.Fixture
{
    [CollectionDefinition(Name)]
    public sealed class DatabaseCollection
        : ICollectionFixture<DatabaseFixture>
    {
        public const string Name = "Kanban database";
    }
}
