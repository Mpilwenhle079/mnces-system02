namespace MnceShisanyama.Api.Services;

public interface IPaymentGateway
{
    Task<ChargeResult> ChargeAsync(ChargeRequest request);
}

public record ChargeRequest(
    decimal Amount,
    string CardHolderName,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Cvv
);

public class ChargeResult
{
    public bool Success { get; }
    public string? GatewayReference { get; }
    public string? FailureReason { get; }

    public ChargeResult(bool success, string? gatewayReference, string? failureReason)
    {
        Success = success;
        GatewayReference = gatewayReference;
        FailureReason = failureReason;
    }
}

public class DemoPaymentGateway : IPaymentGateway
{
    public Task<ChargeResult> ChargeAsync(ChargeRequest request)
    {
        var digits = new string(request.CardNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(request.CardHolderName) || digits.Length < 12)
        {
            return Task.FromResult(new ChargeResult(false, null, "Card details are invalid."));
        }

        if (request.ExpiryMonth is < 1 or > 12 || request.ExpiryYear < DateTime.UtcNow.Year % 100)
        {
            return Task.FromResult(new ChargeResult(false, null, "The card expiry date is invalid."));
        }

        if (string.IsNullOrWhiteSpace(request.Cvv) || request.Cvv.Length < 3)
        {
            return Task.FromResult(new ChargeResult(false, null, "Card security code is invalid."));
        }

        var reference = $"DEMO-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        return Task.FromResult(new ChargeResult(true, reference, null));
    }
}
