using Microsoft.AspNetCore.SignalR;
using MnceShisanyama.Api.DTOs;
using MnceShisanyama.Api.Hubs;

namespace MnceShisanyama.Api.Services;

/// <summary>
/// Thin wrapper so controllers don't need to depend on SignalR types directly -
/// keeps OrdersController focused on HTTP concerns and this class focused on
/// "who needs to know about this event".
/// </summary>
public class OrderNotifier
{
    private readonly IHubContext<OrderHub> _hub;

    public OrderNotifier(IHubContext<OrderHub> hub)
    {
        _hub = hub;
    }

    public Task NotifyNewOrderAsync(OrderResponse order) =>
        _hub.Clients.Group("staff").SendAsync("NewOrder", order);

    public Task NotifyStatusChangedAsync(OrderResponse order)
    {
        var staffTask = _hub.Clients.Group("staff").SendAsync("OrderStatusChanged", order);
        var customerTask = _hub.Clients.Group($"order-{order.Id}").SendAsync("OrderStatusChanged", order);
        return Task.WhenAll(staffTask, customerTask);
    }
}
