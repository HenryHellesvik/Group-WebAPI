using System.Security.Claims;
using Bruktborsen.Api.DTOs.Favorites;
using Bruktborsen.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController(IFavoriteService service) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /api/favorites
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FavoriteDto>>> GetMine() =>
        Ok(await service.GetMineAsync(UserId));

    // POST /api/favorites/5
    [HttpPost("{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FavoriteDto>> Add(int listingId)
    {
        var favorite = await service.AddAsync(listingId, UserId);
        return StatusCode(StatusCodes.Status201Created, favorite);
    }

    // DELETE /api/favorites/5
    [HttpDelete("{listingId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int listingId)
    {
        await service.RemoveAsync(listingId, UserId);
        return NoContent();
    }
}