using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MnceShisanyama.Api.Models;

/// <summary>
/// A single line within an order. Name/price are snapshotted at order time so that
/// later menu price changes never rewrite historical order totals.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    [Required, MaxLength(120)]
    public string ItemNameSnapshot { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }

    [MaxLength(200)]
    public string? SpecialInstructions { get; set; }
}
