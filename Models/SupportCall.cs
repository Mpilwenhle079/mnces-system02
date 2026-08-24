using System.ComponentModel.DataAnnotations;

namespace MnceShisanyama.Api.Models;

public class SupportCall
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Type { get; set; } = "Order issue";

    [Required, MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public CallStatus Status { get; set; } = CallStatus.Open;

    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}