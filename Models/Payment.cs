using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MnceShisanyama.Api.Models;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Card;

    [MaxLength(4)]
    public string? CardLastFour { get; set; }

    public CardBrand? CardBrand { get; set; }

    [Required, MaxLength(80)]
    public string GatewayReference { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? FailureReason { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Succeeded;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}