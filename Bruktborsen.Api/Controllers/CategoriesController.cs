using Bruktborsen.Api.DTOs;
using Bruktborsen.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bruktborsen.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    // GET /api/categories
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryDto>>> GetAll() =>
        Ok(await service.GetAllAsync());

    // GET /api/categories/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(int id) =>
        Ok(await service.GetByIdAsync(id));

    // POST /api/categories
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequestDto dto)
    {
        var category = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    // PUT /api/categories/5
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequestDto dto) =>
        Ok(await service.UpdateAsync(id, dto));

    // DELETE /api/categories/5
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}