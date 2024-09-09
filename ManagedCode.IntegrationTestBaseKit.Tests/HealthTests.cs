using FluentAssertions;
using TestBlazorApp;
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
    public void ConnectionStringTest()
    {
        StaticContainer.AzureBlobConnectionString
            .Should()
            .Be(testApplication.GetContainer<AzuriteContainer>()
                .GetConnectionString());

        StaticContainer.PostgreSqlConnectionString
            .Should()
            .Be(testApplication.GetContainer<PostgreSqlContainer>("postgree")
                .GetConnectionString());
    }
}