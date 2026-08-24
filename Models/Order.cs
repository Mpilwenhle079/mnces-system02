using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MnceShisanyama.Api.Models;

/// <summary>
/// A customer order made up of one or more order lines (order items).
/// </summary>
public class Order
{
    public int Id { get; set; }

    /// <summary>Human friendly reference shown to the customer, e.g. "MT-260822-0007".</summary>
    [MaxLength(30)]
    public string OrderNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public OrderChannel Channel { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    public Payment? Payment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this order used a loyalty discount that should not be applied again.
    /// </summary>
    public bool UsedLoyaltyReward { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
