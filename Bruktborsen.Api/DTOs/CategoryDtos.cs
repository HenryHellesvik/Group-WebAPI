using System.ComponentModel.DataAnnotations;

namespace Bruktborsen.Api.DTOs;

// Output
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Input (POST / PUT)
public class CategoryRequestDto
{
    [Required(ErrorMessage = "Navn er kreves")]
    [MaxLength(50, ErrorMessage = "Navn kan maks være 50 tegn")]
    public string Name { get; set; } = string.Empty;
}