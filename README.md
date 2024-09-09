# IntegrationTestBaseKit

## Overview

IntegrationTestBaseKit is a library designed to facilitate the creation and management of Docker containers for
integration testing purposes. It provides a set of tools to start, stop, and check the readiness of Docker containers in
a thread-safe manner.

## Features

- Start and stop Docker containers asynchronously.
- Check container readiness using customizable wait strategies.
- Event-driven notifications for container lifecycle events (starting, started, stopping, stopped).

## Installation

To install the library, use the following command:

```sh
dotnet add package ManagedCode.IntegrationTestBaseKit
```

for xUnit integration use the following command:

```sh
dotnet add package ManagedCode.ManagedCode.IntegrationTestBaseKit.XUnit
```

## Usage

Creating a Test Application
Define a TestApp class that inherits from `BaseXUnitTestApp<TestBlazorApp.Program>`, add `ICollectionFixture<TestApp>`

```csharp
using DotNet.Testcontainers.Containers;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;
using Xunit;

namespace ManagedCode.IntegrationTestBaseKit.Tests
{
    [CollectionDefinition(nameof(TestApp))]
    public class TestApp : BaseXUnitTestApp<TestBlazorApp.Program>, ICollectionFixture<TestApp>
    {
        protected override async Task ConfigureTestContainers()
        {
            AddContainer(new AzuriteBuilder().Build());
            AddContainer("postgree", new PostgreSqlBuilder().Build());
        }
    }
}
```

## Writing Tests

Use the TestApp class in your tests to manage Docker containers.

```csharp
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
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
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
    public async Task HealthTest()
    {
        var client = testApplication.CreateSignalRClient("/healthHub");
        await client.StartAsync();

        client.State
            .Should()
            .Be(HubConnectionState.Connected);

        var result = await client.InvokeAsync<string>("Health");
        result.Should()
            .Be("Healthy");
    }
}
```