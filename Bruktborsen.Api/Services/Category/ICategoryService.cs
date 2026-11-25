using Bruktborsen.Api.DTOs;
using Bruktborsen.Api.DTOs.Category;

namespace Bruktborsen.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CategoryRequestDto dto);
    Task<CategoryDto> UpdateAsync(int id, CategoryRequestDto dto);
    Task DeleteAsync(int id);
}