using System.Security.Claims;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController(IListingService service) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsAdmin => User.IsInRole(DbSeeder.AdminRole);

    // GET /api/listings?search=&categoryId=&condition=&minPrice=&maxPrice=&sort=
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ListingDto>>> Search([FromQuery] ListingQueryDto query) =>
        Ok(await service.SearchAsync(query));

    // GET /api/listings/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListingDetailsDto>> GetById(int id) =>
        Ok(await service.GetByIdAsync(id));

    // GET /api/listings/my
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ListingDto>>> GetMine() =>
        Ok(await service.GetMineAsync(UserId));

    // POST /api/listings
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ListingDetailsDto>> Create(ListingRequestDto dto)
    {
        var listing = await service.CreateAsync(dto, UserId);
        return CreatedAtAction(nameof(GetById), new { id = listing.Id }, listing);
    }

    // PUT /api/listings/5
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ListingDetailsDto>> Update(int id, ListingRequestDto dto) =>
        Ok(await service.UpdateAsync(id, dto, UserId, IsAdmin));

    // DELETE /api/listings/5  (setter status til Removed)
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(int id)
    {
        await service.RemoveAsync(id, UserId, IsAdmin);
        return NoContent();
    }
}