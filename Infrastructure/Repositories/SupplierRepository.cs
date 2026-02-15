namespace Infrastructure.Repositories;

using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class SupplierRepository(AppDbContext context, ILogger<SupplierRepository> logger)
    : GenericRepository<Supplier>(context, logger), ISupplierRepository
{
    public async Task<IEnumerable<Supplier>> GetActiveSuppliersAsync()
    {
        return await _dbSet
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<ILookup<string, Supplier>> GetSuppliersLookupByCountryAsync()
    {
        var suppliers = await _dbSet.ToListAsync();
        return suppliers.ToLookup(s => s.Address.Country);
    }

    public async Task<Dictionary<Guid, Supplier>> GetSuppliersDictionaryAsync()
    {
        return await _dbSet
            .ToDictionaryAsync(s => s.Id);
    }
}
