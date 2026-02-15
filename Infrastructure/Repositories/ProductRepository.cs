namespace Infrastructure.Repositories;

using Domain.Common;
using Domain.Entities;
using Domain.QueryFilters;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ProductRepository(AppDbContext context, ILogger<ProductRepository> logger) : GenericRepository<Product>(context, logger), IProductRepository
{
    private static readonly Func<AppDbContext, Guid, Task<Product?>> GetByIdWithIncludesCompiled =
        EF.CompileAsyncQuery((AppDbContext context, Guid id) =>
            context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefault(p => p.Id == id));

    private static readonly Func<AppDbContext, string, IAsyncEnumerable<Product>> SearchByNameCompiled =
        EF.CompileAsyncQuery((AppDbContext context, string searchTerm) =>
            context.Products
                .Where(p => EF.Functions.Like(p.Name, searchTerm))
                .OrderBy(p => p.Name)
                .Take(50));

    public async Task<Product?> GetByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByIdWithIncludesCompiled(_context, id);
    }

    public async Task<IEnumerable<Product>> GetAllWithIncludesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .IgnoreQueryFilters([IQueryFilterProvider.SoftDeleteFilter])
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ILookup<Guid, Product>> GetProductsLookupByCategoryAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .ToListAsync(cancellationToken);
        _logger.LogDebug("Created lookup by category for {Count} products", products.Count);
        return products.ToLookup(p => p.CategoryId);
    }

    public async Task<ILookup<Guid, Product>> GetProductsLookupBySupplierAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .ToListAsync(cancellationToken);
        return products.ToLookup(p => p.SupplierId);
    }

    public async Task<ILookup<string, Product>> GetProductsLookupByCountryAsync(CancellationToken cancellationToken = default)
    {
        var products = await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .ToListAsync(cancellationToken);
        return products.ToLookup(p => p.Supplier.Address.Country);
    }

    public async Task<IEnumerable<CategoryGrouping>> GroupByCategoryAsync(CancellationToken cancellationToken = default)
    {
        var groupings = await _dbSet
            .AsNoTracking()
            .Include(p => p.Category)
            .Select(p => new
            {
                p.CategoryId,
                CategoryName = p.Category!.Name,
                p.Price,
                p.Stock
            })
            .ToListAsync(cancellationToken);

        return groupings
            .GroupBy(p => new { p.CategoryId, p.CategoryName })
            .Select(g => new CategoryGrouping
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                ProductCount = g.Count(),
                TotalValue = g.Sum(p => p.Price.Amount * p.Stock),
                MinPrice = g.Min(p => p.Price.Amount),
                MaxPrice = g.Max(p => p.Price.Amount),
                AvgPrice = g.Average(p => p.Price.Amount)
            })
            .OrderByDescending(g => g.TotalValue)
            .ToList();
    }

    public async Task<IEnumerable<SupplierGrouping>> GroupBySupplierAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Supplier)
            .GroupBy(p => new { p.SupplierId, p.Supplier!.Name, Country = p.Supplier.Address.Country })
            .Select(g => new SupplierGrouping
            {
                SupplierId = g.Key.SupplierId,
                SupplierName = g.Key.Name,
                Country = g.Key.Country,
                ProductCount = g.Count(),
                TotalStock = g.Sum(p => p.Stock)
            })
            .OrderBy(g => g.Country)
            .ThenByDescending(g => g.ProductCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetStockByCountryAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Supplier)
            .GroupBy(p => p.Supplier!.Address.Country)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Sum(p => p.Stock),
                cancellationToken);
    }

    public async Task<int> BulkUpdatePricesAsync(Money multiplier, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting bulk price update with multiplier {Amount} {Currency}", multiplier.Amount, multiplier.Currency);
        var products = await _dbSet.ToListAsync(cancellationToken);
        foreach (var p in products)
        {
            p.Price = new Money { Amount = p.Price.Amount * multiplier.Amount, Currency = p.Price.Currency };
            p.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Bulk updated {Count} product prices", products.Count);
        return products.Count;
    }

    public async Task<int> UpdatePriceToValueAsync(Guid productId, Money value, CancellationToken cancellationToken = default)
    {
        var product = await _dbSet.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product == null) return 0;
        product.Price = value;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return 1;
    }

    public async Task<int> BulkDeleteOutOfStockAsync(CancellationToken cancellationToken = default)
    {
        var deleted = await _dbSet
            .Where(p => p.Stock == 0)
            .ExecuteDeleteAsync(cancellationToken);
        _logger.LogInformation("Bulk deleted {Count} out-of-stock products", deleted);
        return deleted;
    }

    public async Task<IEnumerable<Product>> GetProductsWithMinMaxPriceAsync(decimal min, decimal max, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => p.Price.Amount >= min && p.Price.Amount <= max)
            .OrderBy(p => p.Price.Amount)
            .ToListAsync(cancellationToken);
    }

    public async Task BulkInsertAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        var productsList = products.ToList();
        _logger.LogInformation("Starting bulk insert of {Count} products", productsList.Count);
        await _dbSet.AddRangeAsync(productsList, cancellationToken);
    }

    public async Task<PagedResult<Product>> GetProductsByCategoryPagedAsync(
        Guid categoryId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => p.CategoryId == categoryId);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Product>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = pageNumber > 1,
            HasNextPage = pageNumber < totalPages
        };
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<Product>();

        var pattern = $"%{searchTerm}%";
        var list = new List<Product>();
        await foreach (var p in SearchByNameCompiled(_context, pattern))
            list.Add(p);
        return list;
    }

    public async Task<ProductStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var data = await _dbSet
            .AsNoTracking()
            .Select(p => new { p.Price, p.Stock })
            .ToListAsync(cancellationToken);

        if (data.Count == 0)
            return new ProductStatistics();

        return new ProductStatistics
        {
            TotalProducts = data.Count,
            TotalStock = data.Sum(p => p.Stock),
            TotalValue = data.Sum(p => p.Price.Amount * p.Stock),
            AveragePrice = data.Average(p => p.Price.Amount),
            MinPrice = data.Min(p => p.Price.Amount),
            MaxPrice = data.Max(p => p.Price.Amount)
        };
    }
}
