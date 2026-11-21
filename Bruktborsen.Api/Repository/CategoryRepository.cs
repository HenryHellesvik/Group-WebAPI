using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Api.Repository;

public class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync() =>
        context.Categories.OrderBy(c => c.Name).ToListAsync();

    public Task<Category?> GetByIdAsync(int id) =>
        context.Categories.FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null) =>
        context.Categories.AnyAsync(c =>
            c.Name.ToLower() == name.ToLower() &&
            (excludeId == null || c.Id != excludeId));

    public Task<bool> HasListingsAsync(int id) =>
        context.Listings.AnyAsync(l => l.CategoryId == id);

    public async Task AddAsync(Category category)
    {
        context.Categories.Add(category);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Category category)
    {
        context.Categories.Remove(category);
        await context.SaveChangesAsync();
    }
}