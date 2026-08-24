using Microsoft.AspNetCore.Mvc;
using MnceShisanyama.Api.DTOs;
using MnceShisanyama.Api.Services;

namespace MnceShisanyama.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly StaffAuthService _authService;

    public AuthController(StaffAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Staff sign-in for the Kitchen / Admin dashboards.</summary>
    [HttpPost("staff-login")]
    public async Task<ActionResult<StaffLoginResponse>> StaffLogin([FromBody] StaffLoginRequest request)
    {
        var session = await _authService.LoginAsync(request.PinCode);
        if (session is null)
            return Unauthorized(new { message = "Incorrect PIN." });

        return Ok(new StaffLoginResponse(session.Token, session.Name, session.Role));
    }
}
