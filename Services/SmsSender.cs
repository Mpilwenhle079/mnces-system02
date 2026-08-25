namespace MnceShisanyama.Api.Services;

public interface ISmsSender
{
    Task SendPickupCodeAsync(string phone, string orderNumber, string code);
}

public sealed class DemoSmsSender : ISmsSender
{
    private readonly ILogger<DemoSmsSender> _logger;

    public DemoSmsSender(ILogger<DemoSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendPickupCodeAsync(string phone, string orderNumber, string code)
    {
        _logger.LogInformation("[Demo SMS] Pickup code for {OrderNumber} to {Phone}: {Code}", orderNumber, phone, code);
        return Task.CompletedTask;
    }
}