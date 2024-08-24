using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ManagedCode.IntegrationTestBaseKit;

internal class PlaywrightWrapper
{
    public IBrowser Browser { get; set; } = default!;
    private IPlaywright PlaywrightInstance { get; set; } = default!;

    public Task InitializeAsync()
    {
        return InitializeAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task InitializeAsync(BrowserTypeLaunchOptions options)
    {
        await PlaywrightInstaller.Install();
        PlaywrightInstance = await Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(options);
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        PlaywrightInstance.Dispose();
    }

    private static class PlaywrightInstaller
    {
        private static Task<bool> InstallInternal(string command)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            _ = Task.Factory.StartNew(() =>
            {
                var logger = LoggerFactory.Create(builder => builder.AddConsole())
                    .CreateLogger(nameof(PlaywrightInstaller));
                logger.LogInformation("Installing Playwright...");
                var exitCode = Program.Main(new[] { command });
                taskCompletionSource.SetResult(exitCode == 0);
                logger.LogInformation($"Playwright installed; Status: {exitCode == 0}");
            }, TaskCreationOptions.LongRunning);

            return taskCompletionSource.Task;
        }

        public static Task<bool> Install(string browser)
        {
            var command = $"install {browser}";
            return InstallInternal(command);
        }

        public static Task<bool> Install()
        {
            return InstallInternal("install");
        }
    }
}