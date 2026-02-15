namespace Domain.Repositories;

using Domain.Entities;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
    Task<Category?> GetByCodeAsync(string code);
    Task<Dictionary<Guid, Category>> GetCategoriesDictionaryAsync();
}
