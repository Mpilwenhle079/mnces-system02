using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MnceShisanyama.Api.Data;
using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.Services;

/// <summary>
/// A deliberately simple session-token store for staff dashboards.
///
/// How it works: a staff member enters their PIN on the Kitchen or Admin login screen.
/// If it matches an active StaffUser, we mint a random opaque token, keep it in memory
/// mapped to that staff member, and the dashboard sends it back on the "X-Staff-Token"
/// header on every request. StaffAuthFilter (see Filters/) validates that header.
///
/// This is a starter-project auth model, intentionally dependency-free so the whole
/// solution runs with zero external services. For production, replace this with
/// ASP.NET Core Identity + JWT (or cookie auth) and persist sessions server-side or
/// behind a proper identity provider.
/// </summary>
public class StaffAuthService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<string, StaffSession> _sessions = new();

    public StaffAuthService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<StaffSession?> LoginAsync(string pinCode)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pinHash = HashPin(pinCode);
        var staff = await db.StaffUsers
            .FirstOrDefaultAsync(s => s.PinHash == pinHash && s.IsActive);

        if (staff is null) return null;

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var session = new StaffSession(token, staff.Id, staff.Name, staff.Role);
        _sessions[token] = session;
        return session;
    }

    public bool TryGetSession(string token, out StaffSession? session) =>
        _sessions.TryGetValue(token, out session);

    public static string HashPin(string pin)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pin));
        return Convert.ToHexString(bytes);
    }
}

public record StaffSession(string Token, int StaffId, string Name, StaffRole Role);
