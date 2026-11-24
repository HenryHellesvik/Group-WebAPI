using Bruktborsen.Api.Models;

namespace Bruktborsen.Api.Repository;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task<bool> HasListingsAsync(int id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Category category);
}