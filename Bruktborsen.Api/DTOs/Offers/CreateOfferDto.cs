using System.ComponentModel.DataAnnotations;

namespace Bruktborsen.Api.DTOs.Offers;

public class CreateOfferDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Budet må være større enn 0.")]
    public int Amount { get; set; }
}
