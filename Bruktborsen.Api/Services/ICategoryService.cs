using Bruktborsen.Api.DTOs;

namespace Bruktborsen.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<ServiceResult<CategoryDto>> CreateAsync(CategoryRequestDto dto);
    Task<ServiceResult<CategoryDto>> UpdateAsync(int id, CategoryRequestDto dto);
    Task<ServiceResult> DeleteAsync(int id);
}