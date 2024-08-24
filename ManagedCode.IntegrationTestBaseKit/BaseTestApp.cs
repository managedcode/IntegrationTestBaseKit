using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace ManagedCode.IntegrationTestBaseKit;

public abstract class BaseTestApp<TEntryPoint> : WebApplicationFactory<TEntryPoint> 
    where TEntryPoint : class
{
    private IHost? _host;

    private PlaywrightWrapper Fixture { get; } = new();

    protected Dictionary<string, DockerContainer> Containers { get; } = new();

    public IBrowser Browser => Fixture.Browser;

    public Uri ServerUri
    {
        get
        {
            EnsureServer();
            return ClientOptions.BaseAddress;
        }
    }

    public string ServerAddress
    {
        get
        {
            EnsureServer();
            return ClientOptions.BaseAddress.ToString();
        }
    }

    private void EnsureServer()
    {
        if (_host is null)
        {
            // This forces WebApplicationFactory to bootstrap the server  
            using var _ = CreateDefaultClient();
        }
    }

    public T GetContainer<T>(string name) where T : DockerContainer
    {
        return (T)Containers[name];
    }

    public T GetContainer<T>() where T : DockerContainer
    {
        return (T)Containers[typeof(T).Name];
    }

    public virtual async Task InitializeAsync()
    {
        await ConfigureTestContainers();
        await Fixture.InitializeAsync();
        foreach (var container in Containers)
            await container.Value.StartAsync();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var testHost = builder.Build();
        builder.ConfigureWebHost(hostBuilder => hostBuilder.UseKestrel());
        _host = builder.Build();
        _host.Start();

        var server = _host.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        ClientOptions.BaseAddress = addressFeature!.Addresses
            .Select(s => new Uri(s))
            .Last();

        testHost.Start();
        return testHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    public override async ValueTask DisposeAsync()
    {
        await Fixture.DisposeAsync();
        foreach (var container in Containers)
        {
            await container.Value.StopAsync();
            await container.Value.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    public HttpClient CreateHttpClient()
    {
        return Server.CreateClient();
    }

    public HubConnection CreateSignalRClient(string hubUrl, Action<HubConnectionBuilder>? configure = null,
        Action<HttpConnectionOptions>? configureConnection = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = ServerUri
        });
        var builder = new HubConnectionBuilder();
        configure?.Invoke(builder);
        return builder.WithUrl(new Uri(client.BaseAddress!, hubUrl), options =>
            {
                configureConnection?.Invoke(options);
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            })
            .Build();
    }

    public async Task<IPage> OpenNewPage(string url)
    {
        var fullUrl = new Uri(ServerUri, url).ToString();
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(fullUrl);
        return page;
    }

    protected void AddContainer(string name, DockerContainer container)
    {
        Containers.Add(name, container);
    }

    protected void AddContainer(DockerContainer container)
    {
        Containers.Add(container.GetType()
            .Name, container);
    }

    protected abstract Task ConfigureTestContainers();
}