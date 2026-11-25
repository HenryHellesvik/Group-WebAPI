using Bruktborsen.Api.DTOs;
using Bruktborsen.Api.DTOs.Category;
using Bruktborsen.Api.Exceptions;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repository;

namespace Bruktborsen.Api.Services;

public class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync() =>
        (await repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<CategoryDto> GetByIdAsync(int id) =>
        ToDto(await GetCategoryOrThrowAsync(id));

    public async Task<CategoryDto> CreateAsync(CategoryRequestDto dto)
    {
        var name = dto.Name.Trim();

        if (await repository.NameExistsAsync(name))
            throw new ConflictException($"Kategorien '{name}' finnes allerede.");

        var category = new Category { Name = name };
        await repository.AddAsync(category);

        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryRequestDto dto)
    {
        var category = await GetCategoryOrThrowAsync(id);
        var name = dto.Name.Trim();

        if (await repository.NameExistsAsync(name, excludeId: id))
            throw new ConflictException($"Kategorien '{name}' finnes allerede.");

        category.Name = name;
        await repository.UpdateAsync(category);

        return ToDto(category);
    }

    public async Task DeleteAsync(int id)
    {
        var category = await GetCategoryOrThrowAsync(id);

        // Forretningsregel: kategori med annonser kan ikke slettes
        if (await repository.HasListingsAsync(id))
            throw new ConflictException("Kategorien har annonser og kan ikke slettes.");

        await repository.DeleteAsync(category);
    }

    private async Task<Category> GetCategoryOrThrowAsync(int id) =>
        await repository.GetByIdAsync(id)
        ?? throw new NotFoundException("Kategorien finnes ikke.");

    private static CategoryDto ToDto(Category c) => new() { Id = c.Id, Name = c.Name };
}