namespace Domain.Repositories;

using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetAllWithIncludesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdWithIncludesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ILookup<Guid, Product>> GetProductsLookupByCategoryAsync(CancellationToken cancellationToken = default);
    Task<ILookup<Guid, Product>> GetProductsLookupBySupplierAsync(CancellationToken cancellationToken = default);
    Task<ILookup<string, Product>> GetProductsLookupByCountryAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<CategoryGrouping>> GroupByCategoryAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SupplierGrouping>> GroupBySupplierAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetStockByCountryAsync(CancellationToken cancellationToken = default);

    Task<int> BulkUpdatePricesAsync(Money multiplier, CancellationToken cancellationToken = default);
    Task<int> UpdatePriceToValueAsync(Guid productId, Money value, CancellationToken cancellationToken = default);
    Task<int> BulkDeleteOutOfStockAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetProductsWithMinMaxPriceAsync(decimal min, decimal max, CancellationToken cancellationToken = default);
    Task BulkInsertAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);

    Task<PagedResult<Product>> GetProductsByCategoryPagedAsync(
        Guid categoryId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<ProductStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public class CategoryGrouping
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal AvgPrice { get; set; }
}

public class SupplierGrouping
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
}

public class ProductStatistics
{
    public int TotalProducts { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
}
