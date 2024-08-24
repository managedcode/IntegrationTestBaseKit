using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace ManagedCode.IntegrationTestBaseKit.Tests;

[Collection(nameof(TestApp))]
public class HealthTests(ITestOutputHelper log, TestApp testApplication)
{
    [Fact]
    public async Task HealthTest()
    {
        var client = testApplication.CreateHttpClient();
        var responce = await client.GetAsync("/health");
        responce.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task BrowserHealthTest()
    {
        var page = await testApplication.OpenNewPage("/health");
        var content = await page.ContentAsync();
        content.Contains("Healthy")
            .Should()
            .BeTrue();
    }
    
    [Fact]
    public void TestcontainersTest()
    {
        var azurite = testApplication.GetContainer<AzuriteContainer>();
        var postgree = testApplication.GetContainer<PostgreSqlContainer>("postgree");
        
        azurite.GetConnectionString()
            .Should()
            .NotBeNullOrWhiteSpace();
        
        azurite.State 
            .Should()
            .Be(TestcontainersStates.Running);
        
        
        postgree.GetConnectionString()
            .Should()
            .NotBeNullOrWhiteSpace();
        postgree.State 
            .Should()
            .Be(TestcontainersStates.Running);
    }
}

[CollectionDefinition(nameof(TestApp))]
public class TestApp : BaseTestApp<TestBlazorApp.Program>, ICollectionFixture<TestApp>, IAsyncLifetime
{
    protected override async Task ConfigureTestContainers()
    {
        AddContainer(new AzuriteBuilder().Build());
        AddContainer("postgree", new PostgreSqlBuilder().Build());
    }

    public async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}