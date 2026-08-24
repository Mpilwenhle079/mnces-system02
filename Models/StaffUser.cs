using System.ComponentModel.DataAnnotations;

namespace MnceShisanyama.Api.Models;

/// <summary>
/// A staff account. Staff sign in to the Kitchen / Admin dashboards with a short PIN
/// rather than a username/password, since these are shared shop-floor devices.
///
/// SECURITY NOTE: PinHash is a SHA-256 hash for this starter system. Before going to
/// production, swap this for ASP.NET Core Identity (or another proper auth provider)
/// with salted password hashing, account lockout, and HTTPS enforced end to end.
/// </summary>
public class StaffUser
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string PinHash { get; set; } = string.Empty;

    public StaffRole Role { get; set; } = StaffRole.Kitchen;

    public bool IsActive { get; set; } = true;
}
