using System.ComponentModel.DataAnnotations;
using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.DTOs;

public class CreateOrderItemRequest
{
    [Required]
    public int MenuItemId { get; set; }

    [Range(1, 50)]
    public int Quantity { get; set; } = 1;

    [MaxLength(200)]
    public string? SpecialInstructions { get; set; }
}

public class CreateOrderRequest
{
    [Required, MaxLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    public OrderChannel Channel { get; set; } = OrderChannel.Collection;

    [MaxLength(300)]
    public string? DeliveryAddress { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required, MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;

    [MaxLength(4)]
    public string? CardLastFour { get; set; }
}

public record OrderItemResponse(
    int Id,
    int MenuItemId,
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? SpecialInstructions
);

public record OrderResponse(
    int Id,
    string OrderNumber,
    string CustomerName,
    string Phone,
    string? DeliveryAddress,
    OrderChannel Channel,
    OrderStatus Status,
    string? Notes,
    decimal Subtotal,
    decimal DiscountAmount,
    bool UsedLoyaltyReward,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<OrderItemResponse> Items
);

public class UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus Status { get; set; }
}
