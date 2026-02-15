namespace Infrastructure.Repositories;

using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CategoryRepository(AppDbContext context, ILogger<CategoryRepository> logger)
    : GenericRepository<Category>(context, logger), ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByCodeAsync(string code)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Code == code.ToUpper());
    }

    public async Task<Dictionary<Guid, Category>> GetCategoriesDictionaryAsync()
    {
        return await _dbSet
            .ToDictionaryAsync(c => c.Id);
    }
}
