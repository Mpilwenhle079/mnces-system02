using System.ComponentModel.DataAnnotations;
using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.DTOs;

public class ChargeCardRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public PaymentMethod Method { get; set; } = PaymentMethod.Card;

    [MaxLength(40)]
    public string? CardNumber { get; set; }

    [MaxLength(120)]
    public string? CardHolderName { get; set; }

    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }

    [MaxLength(4)]
    public string? Cvv { get; set; }
}

public record PaymentResponse(
    bool Success,
    string Message,
    string? GatewayReference,
    CardBrand Brand,
    string? CardLastFour,
    OrderResponse Order
);
