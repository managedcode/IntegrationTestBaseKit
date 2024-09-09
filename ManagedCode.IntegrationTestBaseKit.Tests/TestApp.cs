using ManagedCode.IntegrationTestBaseKit.XUnit;
using TestBlazorApp;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;

namespace ManagedCode.IntegrationTestBaseKit.Tests;

[CollectionDefinition(nameof(TestApp))]
public class TestApp : BaseXUnitTestApp<Program>, ICollectionFixture<TestApp>
{
    protected override async Task ConfigureTestContainers()
    {
        AddContainer(new AzuriteBuilder().Build());
        AddContainer("postgree", new PostgreSqlBuilder().Build());
    }

    protected override void ConfigureConfiguration()
    {
        SetConfigurationValue("AzureBlob", GetContainer<AzuriteContainer>()
            .GetConnectionString());
        SetConfigurationValue("ConnectionStrings:PostgreSql", GetContainer<PostgreSqlContainer>("postgree")
            .GetConnectionString());
    }
}