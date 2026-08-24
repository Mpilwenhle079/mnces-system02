using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnceShisanyama.Api.Data;
using MnceShisanyama.Api.DTOs;
using MnceShisanyama.Api.Filters;
using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.Controllers;

[ApiController]
[Route("api/support-calls")]
public class SupportCallsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SupportCallsController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<SupportCallResponse>> Create(CreateSupportCallRequest request)
    {
        var call = new SupportCall
        {
            CustomerName = request.CustomerName,
            Phone = request.Phone,
            Type = request.Type,
            Description = request.Description,
            OrderId = request.OrderId
        };
        _db.SupportCalls.Add(call);
        await _db.SaveChangesAsync();
        return Created($"/api/support-calls/{call.Id}", ToResponse(call));
    }

    [HttpGet]
    [StaffAuth(StaffRole.Admin)]
    public async Task<ActionResult<List<SupportCallResponse>>> GetAll()
    {
        var calls = await _db.SupportCalls.OrderByDescending(c => c.CreatedAt).Take(100).ToListAsync();
        return Ok(calls.Select(ToResponse).ToList());
    }

    [HttpPatch("{id:int}/status")]
    [StaffAuth(StaffRole.Admin)]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] CallStatus status)
    {
        var call = await _db.SupportCalls.FindAsync(id);
        if (call is null) return NotFound();
        call.Status = status;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static SupportCallResponse ToResponse(SupportCall call) => new(call.Id, call.CustomerName, call.Phone, call.Type, call.Description, call.Status.ToString(), call.OrderId, call.CreatedAt);
}