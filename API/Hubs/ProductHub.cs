namespace API.Hubs;

using Microsoft.AspNetCore.SignalR;

public class ProductHub : Hub
{
    public async Task NotifyProductCreated(object product)
    {
        await Clients.All.SendAsync("ProductCreated", product);
    }

    public async Task NotifyProductUpdated(object product)
    {
        await Clients.All.SendAsync("ProductUpdated", product);
    }

    public async Task NotifyProductDeleted(Guid productId)
    {
        await Clients.All.SendAsync("ProductDeleted", productId);
    }
}
