using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MnceShisanyama.Api.Models;

/// <summary>
/// A single sellable item on the menu (a plate, a platter, etc).
/// </summary>
public class MenuItem
{
    public int Id { get; set; }

    public int CategoryId { get; set; }
    public MenuCategory? Category { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    /// <summary>For platters, e.g. "Serves 2" / "Serves 3". Null for regular plates.</summary>
    [MaxLength(40)]
    public string? ServingInfo { get; set; }

    /// <summary>Relative path under wwwroot/img, or an external URL. Optional.</summary>
    [MaxLength(300)]
    public string? ImageUrl { get; set; }

    /// <summary>Toggle off to "86" an item without deleting it (e.g. sold out today).</summary>
    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
