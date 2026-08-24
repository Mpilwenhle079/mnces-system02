using System.ComponentModel.DataAnnotations;
using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.DTOs;

public record TopItemDto(string Name, int QuantitySold, decimal Revenue);

public record DashboardSummaryResponse(
    int TodayOrderCount,
    decimal TodayRevenue,
    int PendingCount,
    int PreparingCount,
    int ReadyCount,
    int CompletedTodayCount,
    int CardPaymentsToday,
    int CashPaymentsToday,
    int OpenCallCount,
    List<TopItemDto> TopItemsToday
);

public class StaffLoginRequest
{
    [Required, MinLength(4), MaxLength(10)]
    public string PinCode { get; set; } = string.Empty;
}

public record StaffLoginResponse(
    string Token,
    string Name,
    StaffRole Role
);
