using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnceShisanyama.Api.Data;
using MnceShisanyama.Api.DTOs;
using MnceShisanyama.Api.Models;
using MnceShisanyama.Api.Services;

namespace MnceShisanyama.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly OrderNotifier _notifier;

    public PaymentsController(AppDbContext db, IPaymentGateway gateway, OrderNotifier notifier)
    {
        _db = db;
        _gateway = gateway;
        _notifier = notifier;
    }

    [HttpPost("charge")]
    public async Task<ActionResult<PaymentResponse>> Charge([FromBody] ChargeCardRequest request)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order is null) return NotFound(new { message = "Order not found." });
        if (order.Status != OrderStatus.AwaitingPayment)
            return BadRequest(new { message = "This order has already been paid for." });

        CardBrand brand = CardBrand.Unknown;
        string? last4 = null;
        ChargeResult result;

        if (request.Method == PaymentMethod.Card)
        {
            if (string.IsNullOrWhiteSpace(request.CardNumber) ||
                string.IsNullOrWhiteSpace(request.CardHolderName) ||
                request.ExpiryMonth is null || request.ExpiryYear is null ||
                string.IsNullOrWhiteSpace(request.Cvv))
            {
                return BadRequest(new { message = "Card details are incomplete." });
            }

            brand = CardValidator.DetectBrand(request.CardNumber);
            var digitsOnly = new string(request.CardNumber.Where(char.IsDigit).ToArray());
            last4 = digitsOnly.Length >= 4 ? digitsOnly[^4..] : digitsOnly;
            result = await _gateway.ChargeAsync(new ChargeRequest(
                order.TotalAmount,
                request.CardHolderName,
                request.CardNumber,
                request.ExpiryMonth.Value,
                request.ExpiryYear.Value,
                request.Cvv));
        }
        else
        {
            result = new ChargeResult(true, "CASH-ON-COLLECTION", null);
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            Method = request.Method,
            Amount = order.TotalAmount,
            Status = result.Success ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            CardBrand = brand,
            CardLastFour = last4,
            GatewayReference = result.GatewayReference ?? string.Empty,
            FailureReason = result.FailureReason,
            CreatedAt = DateTime.UtcNow
        };
        _db.Payments.Add(payment);
        order.Payment = payment;

        if (!result.Success)
        {
            await _db.SaveChangesAsync();
            return Ok(new PaymentResponse(
                false,
                result.FailureReason ?? "Payment failed. Please try again.",
                null,
                brand,
                last4,
                OrdersController.ToResponse(order, order.Customer!)));
        }

        order.Status = OrderStatus.Pending;
        order.UpdatedAt = DateTime.UtcNow;
        LoyaltyService.RegisterPaidOrder(order.Customer!, order.UsedLoyaltyReward);
        await _db.SaveChangesAsync();

        var response = OrdersController.ToResponse(order, order.Customer!);
        await _notifier.NotifyNewOrderAsync(response);

        return Ok(new PaymentResponse(
            true,
            "Payment successful. Your order has been sent to the kitchen.",
            result.GatewayReference,
            brand,
            last4,
            response));
    }
}
