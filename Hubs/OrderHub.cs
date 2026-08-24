using Microsoft.AspNetCore.SignalR;

namespace MnceShisanyama.Api.Hubs;

/// <summary>
/// Real-time channel used to push order events to connected dashboards (kitchen, admin)
/// and to individual customers tracking their own order, without them having to poll.
///
/// Groups used:
///  - "staff"            -> every kitchen/admin dashboard, receives NewOrder + OrderStatusChanged
///  - "order-{orderId}"  -> the single customer tracking that order number
/// </summary>
public class OrderHub : Hub
{
    public async Task JoinStaffGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
    }

    public async Task JoinOrderGroup(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }
}
