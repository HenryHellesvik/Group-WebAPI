using Bruktborsen.Api.DTOs;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repository;

namespace Bruktborsen.Api.Services;

public class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await repository.GetAllAsync();
        return categories.Select(ToDto).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await repository.GetByIdAsync(id);
        return category == null ? null : ToDto(category);
    }

    public async Task<ServiceResult<CategoryDto>> CreateAsync(CategoryRequestDto dto)
    {
        var name = dto.Name.Trim();

        if (await repository.NameExistsAsync(name))
            return ServiceResult<CategoryDto>.Fail(ServiceError.Conflict, $"Kategorien '{name}' finnes allerede.");

        var category = new Category { Name = name };
        await repository.AddAsync(category);

        return ServiceResult<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<ServiceResult<CategoryDto>> UpdateAsync(int id, CategoryRequestDto dto)
    {
        var category = await repository.GetByIdAsync(id);
        if (category == null)
            return ServiceResult<CategoryDto>.Fail(ServiceError.NotFound, "Kategorien finnes ikke.");

        var name = dto.Name.Trim();
        if (await repository.NameExistsAsync(name, excludeId: id))
            return ServiceResult<CategoryDto>.Fail(ServiceError.Conflict, $"Kategorien '{name}' finnes allerede.");

        category.Name = name;
        await repository.UpdateAsync(category);

        return ServiceResult<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var category = await repository.GetByIdAsync(id);
        if (category == null)
            return ServiceResult.Fail(ServiceError.NotFound, "Kategorien finnes ikke.");

        // Forretningsregel: kategori med annonser kan ikke slettes
        if (await repository.HasListingsAsync(id))
            return ServiceResult.Fail(ServiceError.Conflict, "Kategorien har annonser og kan ikke slettes.");

        await repository.DeleteAsync(category);
        return ServiceResult.Ok();
    }

    private static CategoryDto ToDto(Category c) => new() { Id = c.Id, Name = c.Name };
}