using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit.Abstractions;

namespace ManagedCode.IntegrationTestBaseKit.Tests;

[Collection(nameof(TestApp))]
public class SignalRTests(ITestOutputHelper log, TestApp testApplication)
{
    [Fact]
    public async Task HealthTest()
    {
        var client = testApplication.CreateSignalRClient("/");
        await client.StartAsync();

        client.State
            .Should()
            .Be(HubConnectionState.Connected);

        var result = await client.InvokeAsync<string>("Health");
        result.Should()
            .Be("Healthy");
    }
}