# IntegrationTestBaseKit

## Overview
IntegrationTestBaseKit is a library designed to facilitate the creation and management of Docker containers for integration testing purposes. It provides a set of tools to start, stop, and check the readiness of Docker containers in a thread-safe manner.

## Features
- Start and stop Docker containers asynchronously.
- Check container readiness using customizable wait strategies.
- Event-driven notifications for container lifecycle events (starting, started, stopping, stopped).

## Installation
To install the library, use the following command:

```sh
dotnet add package ManagedCode.IntegrationTestBaseKit
```

## Usage
Creating a Test Application
Define a TestApp class that inherits from `BaseTestApp<TestBlazorApp.Program>`, implements `ICollectionFixture<TestApp>` and `IAsyncLifetime`.


```csharp
using DotNet.Testcontainers.Containers;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;
using Xunit;

namespace ManagedCode.IntegrationTestBaseKit.Tests
{
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

        public async Task InitializeAsync()
        {
            // Initialization logic if needed
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
```