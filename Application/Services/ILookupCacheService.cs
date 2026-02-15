namespace Application.Services;

using Application.DTOs;

public interface ILookupCacheService
{
    Task<ILookup<Guid, ProductDto>> GetProductsByCategoryLookupAsync(CancellationToken cancellationToken = default);
    Task<ILookup<Guid, ProductDto>> GetProductsBySupplierLookupAsync(CancellationToken cancellationToken = default);
    Task<ILookup<string, ProductDto>> GetProductsByCountryLookupAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, CategoryDto>> GetCategoriesCacheAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, SupplierDto>> GetSuppliersCacheAsync(CancellationToken cancellationToken = default);

    void InvalidateCache();
}
