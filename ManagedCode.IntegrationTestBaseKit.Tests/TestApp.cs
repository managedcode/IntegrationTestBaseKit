using TestBlazorApp;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;

namespace ManagedCode.IntegrationTestBaseKit.Tests;

[CollectionDefinition(nameof(TestApp))]
public class TestApp : BaseTestApp<Program>, ICollectionFixture<TestApp>, IAsyncLifetime
{
    public async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }

    protected override async Task ConfigureTestContainers()
    {
        AddContainer(new AzuriteBuilder().Build());
        AddContainer("postgree", new PostgreSqlBuilder().Build());
    }
}