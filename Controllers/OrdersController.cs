using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnceShisanyama.Api.Data;
using MnceShisanyama.Api.DTOs;
using MnceShisanyama.Api.Filters;
using MnceShisanyama.Api.Models;
using MnceShisanyama.Api.Services;

namespace MnceShisanyama.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly OrderNotifier _notifier;

    public OrdersController(AppDbContext db, OrderNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    /// <summary>
    /// Customer builds their cart and submits it here. This does NOT put the order
    /// in front of the kitchen yet - it's created as AwaitingPayment and the
    /// frontend immediately opens the payment screen. The order only becomes
    /// visible to staff (and counts toward loyalty/reporting) once
    /// POST /api/payments/charge succeeds for it.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (request.Channel == OrderChannel.Delivery && string.IsNullOrWhiteSpace(request.DeliveryAddress))
            return BadRequest(new { message = "Delivery address is required for delivery orders." });

        var menuItemIds = request.Items.Select(i => i.MenuItemId).Distinct().ToList();
        var menuItems = await _db.MenuItems
            .Where(m => menuItemIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        foreach (var line in request.Items)
        {
            if (!menuItems.TryGetValue(line.MenuItemId, out var menuItem) || !menuItem.IsAvailable)
                return BadRequest(new { message = $"Menu item {line.MenuItemId} is not available." });
        }

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == request.Phone);
        if (customer is null)
        {
            customer = new Customer { Name = request.CustomerName, Phone = request.Phone };
            _db.Customers.Add(customer);
        }
        else
        {
            customer.Name = request.CustomerName;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
            customer.Email = request.Email;

        if (request.Channel == OrderChannel.Delivery)
            customer.DeliveryAddress = request.DeliveryAddress;

        var order = new Order
        {
            Customer = customer,
            Channel = request.Channel,
            Status = OrderStatus.AwaitingPayment,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal subtotal = 0m;
        foreach (var line in request.Items)
        {
            var menuItem = menuItems[line.MenuItemId];
            var lineTotal = menuItem.Price * line.Quantity;
            subtotal += lineTotal;

            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                ItemNameSnapshot = menuItem.ServingInfo is null
                    ? menuItem.Name
                    : $"{menuItem.Name} ({menuItem.ServingInfo})",
                UnitPrice = menuItem.Price,
                Quantity = line.Quantity,
                LineTotal = lineTotal,
                SpecialInstructions = line.SpecialInstructions
            });
        }

        var discount = LoyaltyService.CalculateDiscount(customer, subtotal);

        order.Subtotal = subtotal;
        order.DiscountAmount = discount;
        order.UsedLoyaltyReward = discount > 0;
        order.TotalAmount = subtotal - discount;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        order.OrderNumber = $"MT-{order.CreatedAt:yyMMdd}-{order.Id:D4}";
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ToResponse(order, customer));
    }

    /// <summary>
    /// Customer-facing order tracking (also used to hydrate the live tracker before SignalR connects).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();
        return Ok(ToResponse(order, order.Customer!));
    }

    /// <summary>
    /// The explicit "Submit Order to Kitchen" step. Called after a successful payment
    /// (order is in PaymentConfirmed). This is the moment the order actually lands in
    /// the staff/kitchen database - Pending orders show up on the Kitchen Board and
    /// Admin dashboard and get broadcast live over SignalR. Public endpoint (no staff
    /// auth) since it's the customer confirming their own just-paid order.
    /// </summary>
    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<OrderResponse>> SubmitToKitchen(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        if (order.Status == OrderStatus.AwaitingPayment)
            return BadRequest(new { message = "This order hasn't been paid for yet." });

        if (order.Status != OrderStatus.PaymentConfirmed)
            return BadRequest(new { message = "This order has already been submitted." });

        order.Status = OrderStatus.Pending;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var response = ToResponse(order, order.Customer!);
        await _notifier.NotifyNewOrderAsync(response);

        return Ok(response);
    }

    /// <summary>
    /// Staff view of the order board / history. Only ever returns orders the customer
    /// has actually submitted (AwaitingPayment carts and paid-but-not-yet-submitted
    /// orders never show up here).
    /// </summary>
    [HttpGet]
    [StaffAuth]
    public async Task<ActionResult<List<OrderResponse>>> GetOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int take = 100)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.AwaitingPayment && o.Status != OrderStatus.PaymentConfirmed)
            .AsQueryable();

        if (status is not null) query = query.Where(o => o.Status == status);
        if (from is not null) query = query.Where(o => o.CreatedAt >= from);
        if (to is not null) query = query.Where(o => o.CreatedAt <= to);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync();

        return Ok(orders.Select(o => ToResponse(o, o.Customer!)).ToList());
    }

    /// <summary>Kitchen/admin staff move a submitted order through Preparing -> Ready -> Completed (or Cancelled).</summary>
    [HttpPut("{id:int}/status")]
    [StaffAuth]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();
        if (order.Status is OrderStatus.AwaitingPayment or OrderStatus.PaymentConfirmed)
            return BadRequest(new { message = "This order hasn't been submitted to the kitchen yet." });

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var response = ToResponse(order, order.Customer!);
        await _notifier.NotifyStatusChangedAsync(response);

        return Ok(response);
    }

    internal static OrderResponse ToResponse(Order order, Customer customer) => new(
        order.Id,
        order.OrderNumber,
        customer.Name,
        customer.Phone,
        order.Channel == OrderChannel.Delivery ? customer.DeliveryAddress : null,
        order.Channel,
        order.Status,
        order.Notes,
        order.Subtotal,
        order.DiscountAmount,
        order.UsedLoyaltyReward,
        order.TotalAmount,
        order.CreatedAt,
        order.UpdatedAt,
        order.Items.Select(i => new OrderItemResponse(
            i.Id, i.MenuItemId, i.ItemNameSnapshot, i.UnitPrice, i.Quantity, i.LineTotal, i.SpecialInstructions
        )).ToList()
    );
}
