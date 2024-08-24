namespace TestBlazorApp.Hub;

public class HealthHub : Microsoft.AspNetCore.SignalR.Hub
{
    public Task<string> Health()
    {
        return Task.FromResult("Healthy");
    }
}