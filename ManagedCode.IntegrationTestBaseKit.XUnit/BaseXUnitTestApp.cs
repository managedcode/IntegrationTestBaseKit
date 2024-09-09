using Xunit;

namespace ManagedCode.IntegrationTestBaseKit.XUnit;

public abstract class BaseXUnitTestApp<TEntryPoint> : BaseTestApp<TEntryPoint>, IAsyncLifetime where TEntryPoint : class
{
    async Task IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
        CreateHttpClient();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }
}