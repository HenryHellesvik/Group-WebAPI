using System.Security.Claims;
using Bruktborsen.Api.DTOs.Offers;
using Bruktborsen.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Authorize]
public class OffersController(IOfferService offerService) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("api/listings/{listingId:int}/offers")]
    public async Task<ActionResult<List<OfferDto>>> GetForListing(int listingId) =>
        Ok(await offerService.GetForListingAsync(listingId, UserId));

    [HttpPost("api/listings/{listingId:int}/offers")]
    public async Task<ActionResult<OfferDto>> Create(int listingId, CreateOfferDto dto)
    {
        var offer = await offerService.CreateAsync(listingId, UserId, dto);
        return StatusCode(StatusCodes.Status201Created, offer);
    }

    [HttpGet("api/offers/my")]
    public async Task<ActionResult<List<OfferDto>>> GetMine() =>
        Ok(await offerService.GetMineAsync(UserId));

    [HttpPost("api/offers/{id:int}/accept")]
    public async Task<ActionResult<OfferDto>> Accept(int id) =>
        Ok(await offerService.AcceptAsync(id, UserId));

    [HttpPost("api/offers/{id:int}/reject")]
    public async Task<ActionResult<OfferDto>> Reject(int id) =>
        Ok(await offerService.RejectAsync(id, UserId));

    [HttpPost("api/offers/{id:int}/withdraw")]
    public async Task<ActionResult<OfferDto>> Withdraw(int id) =>
        Ok(await offerService.WithdrawAsync(id, UserId));
}
