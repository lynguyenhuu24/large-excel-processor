using Microsoft.AspNetCore.SignalR;

namespace LargeExcelProcessor.Api.Hubs;

public class UploadHub : Hub
{
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
    }

    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
    }

    public async Task SubscribeToRequests()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "requests");
    }

    public async Task UnsubscribeFromRequests()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "requests");
    }
}
