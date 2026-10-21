using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace kanban_lia.Integration.Tests.Fixture
{
    public sealed class KanbanApiFactory(DatabaseFixture database)
    : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                database.ConnectionString);
        }
    }
}
