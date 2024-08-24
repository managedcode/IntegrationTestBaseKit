using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Testcontainers.Azurite;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace ManagedCode.IntegrationTestBaseKit.Tests;

[Collection(nameof(TestApp))]
public class ContainerTests(ITestOutputHelper log, TestApp testApplication)
{
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