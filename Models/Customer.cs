using System.ComponentModel.DataAnnotations;

namespace MnceShisanyama.Api.Models;

/// <summary>
/// Minimal customer record captured at checkout. No login required for customers -
/// they are identified by phone number, which keeps ordering fast and frictionless.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? DeliveryAddress { get; set; }

    public int LoyaltyOrderCount { get; set; }

    public int LoyaltyPoints { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
