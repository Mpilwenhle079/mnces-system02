using System.ComponentModel.DataAnnotations;

namespace MnceShisanyama.Api.DTOs;

public class CreateSupportCallRequest
{
    [Required, MaxLength(120)] public string CustomerName { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Phone { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Type { get; set; } = "Order issue";
    [Required, MaxLength(1000)] public string Description { get; set; } = string.Empty;
    public int? OrderId { get; set; }
}

public record SupportCallResponse(int Id, string CustomerName, string Phone, string Type, string Description, string Status, int? OrderId, DateTime CreatedAt);