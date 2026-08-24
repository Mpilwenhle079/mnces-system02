using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.Services;

public static class LoyaltyService
{
    public static decimal CalculateDiscount(Customer customer, decimal subtotal)
    {
        if (customer.LoyaltyOrderCount > 0 && customer.LoyaltyOrderCount % 7 == 0)
            return Math.Round(subtotal * 0.5m, 2);

        return 0m;
    }

    public static void RegisterPaidOrder(Customer customer, bool usedLoyaltyReward)
    {
        if (usedLoyaltyReward)
        {
            return;
        }

        customer.LoyaltyOrderCount++;
        customer.LoyaltyPoints += 10;
    }
}
