using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.Services;

public static class CardValidator
{
    public static CardBrand DetectBrand(string cardNumber)
    {
        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) return CardBrand.Unknown;

        if (digits.StartsWith("4")) return CardBrand.Visa;
        if (digits.Length >= 2 && (digits.StartsWith("51") || digits.StartsWith("52") || digits.StartsWith("53") ||
            digits.StartsWith("54") || digits.StartsWith("55"))) return CardBrand.Mastercard;
        if (digits.StartsWith("34") || digits.StartsWith("37")) return CardBrand.Amex;
        if (digits.StartsWith("6011") || digits.StartsWith("65")) return CardBrand.Discover;
        if (digits.StartsWith("300") || digits.StartsWith("305") || digits.StartsWith("36") || digits.StartsWith("38")) return CardBrand.Diners;

        return CardBrand.Unknown;
    }
}
