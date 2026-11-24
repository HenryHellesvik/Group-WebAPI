using Bruktborsen.Api.DTOs.Auth;
using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

// /register og /login kommer fra MapIdentityApi. Denne controlleren legger bare til /me.
[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    // GET /api/auth/me
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new UserInfoDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList()
        });
    }
}