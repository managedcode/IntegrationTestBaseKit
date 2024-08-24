using FluentAssertions;
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
}