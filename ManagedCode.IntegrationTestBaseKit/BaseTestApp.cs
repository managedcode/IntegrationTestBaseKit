using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace ManagedCode.IntegrationTestBaseKit;

public abstract class BaseTestApp<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    private IHost? _host;

    private readonly ConfigurationBuilder ConfigurationBuilder = new();

    protected virtual bool UsePlaywright { get; } = true;
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

        if (UsePlaywright)
            await Fixture.InitializeAsync();

        foreach (var container in Containers)
            await container.Value.StartAsync();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        ConfigureConfiguration();
        var configuration = ConfigurationBuilder.Build();
        builder.ConfigureWebHost(hostBuilder =>
        {
            foreach (var setting in configuration.AsEnumerable(true))
                hostBuilder.UseSetting(setting.Key, setting.Value);
        });

        // Create the host for TestServer now before we  
        // modify the builder to use Kestrel instead.    
        var testHost = builder.Build();

        // Modify the host builder to use Kestrel instead  
        // of TestServer so we can listen on a real address.    
        builder.ConfigureWebHost(hostBuilder => hostBuilder.UseKestrel());

        // Create and start the Kestrel server before the test server,  
        // otherwise due to the way the deferred host builder works    
        // for minimal hosting, the server will not get "initialized    
        // enough" for the address it is listening on to be available.    
        // See https://github.com/dotnet/aspnetcore/issues/33846.    
        _host = builder.Build(); //base.CreateHost(builder);
        _host.Start();

        // Extract the selected dynamic port out of the Kestrel server  
        // and assign it onto the client options for convenience so it    
        // "just works" as otherwise it'll be the default http://localhost    
        // URL, which won't route to the Kestrel-hosted HTTP server.     
        var server = _host.Services.GetRequiredService<IServer>();
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        ClientOptions.BaseAddress = addressFeature!.Addresses
            .Select(s => new Uri(s))
            .Last();

        testHost.Start();
        return testHost;
    }

    public override async ValueTask DisposeAsync()
    {
        _host?.Dispose();
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
        if (!UsePlaywright)
            throw new InvalidOperationException("Playwright is not enabled");

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
    protected abstract void ConfigureConfiguration();

    protected void SetConfigurationValue(string key, string value)
    {
        ConfigurationBuilder.AddInMemoryCollection(new Dictionary<string, string> { { key, value } }!);
    }
}