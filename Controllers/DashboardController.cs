using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnceShisanyama.Api.Data;
using MnceShisanyama.Api.DTOs;
using MnceShisanyama.Api.Filters;
using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[StaffAuth]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary()
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var todaysOrders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < todayEnd && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.AwaitingPayment && o.Status != OrderStatus.PaymentConfirmed)
            .ToListAsync();

        var topItems = todaysOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ItemNameSnapshot)
            .Select(g => new TopItemDto(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.LineTotal)))
            .OrderByDescending(t => t.QuantitySold)
            .Take(5)
            .ToList();

        var openCalls = await _db.SupportCalls.CountAsync(c => c.Status == CallStatus.Open);

        var summary = new DashboardSummaryResponse(
            TodayOrderCount: todaysOrders.Count,
            TodayRevenue: todaysOrders.Sum(o => o.TotalAmount),
            PendingCount: todaysOrders.Count(o => o.Status == OrderStatus.Pending),
            PreparingCount: todaysOrders.Count(o => o.Status == OrderStatus.Preparing),
            ReadyCount: todaysOrders.Count(o => o.Status == OrderStatus.Ready),
            CompletedTodayCount: todaysOrders.Count(o => o.Status == OrderStatus.Completed),
            CardPaymentsToday: todaysOrders.Count(o => o.Payment?.Method == PaymentMethod.Card),
            CashPaymentsToday: todaysOrders.Count(o => o.Payment?.Method == PaymentMethod.CashOnCollection),
            OpenCallCount: openCalls,
            TopItemsToday: topItems
        );

        return Ok(summary);
    }
}
