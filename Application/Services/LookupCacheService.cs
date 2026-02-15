namespace Application.Services;

using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;
using Microsoft.Extensions.Caching.Memory;

public class LookupCacheService(IUnitOfWork unitOfWork, IMemoryCache cache) : ILookupCacheService
{
    private const int CacheExpirationMinutes = 30;

    public async Task<ILookup<Guid, ProductDto>> GetProductsByCategoryLookupAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "products_by_category_lookup";

        if (!cache.TryGetValue(cacheKey, out ILookup<Guid, ProductDto>? lookup))
        {
            var productsLookup = await unitOfWork.Products.GetProductsLookupByCategoryAsync(cancellationToken);

            var productDtos = productsLookup
                .SelectMany(g => g)
                .Select(MapProductToDto)
                .ToList();

            lookup = productDtos.ToLookup(p => p.Category!.Id);

            cache.Set(cacheKey, lookup, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        return lookup!;
    }

    public async Task<ILookup<Guid, ProductDto>> GetProductsBySupplierLookupAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "products_by_supplier_lookup";

        if (!cache.TryGetValue(cacheKey, out ILookup<Guid, ProductDto>? lookup))
        {
            var productsLookup = await unitOfWork.Products.GetProductsLookupBySupplierAsync(cancellationToken);

            var productDtos = productsLookup
                .SelectMany(g => g)
                .Select(MapProductToDto)
                .ToList();

            lookup = productDtos.ToLookup(p => p.Supplier!.Id);

            cache.Set(cacheKey, lookup, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        return lookup!;
    }

    public async Task<ILookup<string, ProductDto>> GetProductsByCountryLookupAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "products_by_country_lookup";

        if (!cache.TryGetValue(cacheKey, out ILookup<string, ProductDto>? lookup))
        {
            var productsLookup = await unitOfWork.Products.GetProductsLookupByCountryAsync(cancellationToken);

            var productDtos = productsLookup
                .SelectMany(g => g)
                .Select(MapProductToDto)
                .ToList();

            lookup = productDtos.ToLookup(p => p.Supplier!.Country);

            cache.Set(cacheKey, lookup, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        return lookup!;
    }

    public async Task<Dictionary<Guid, CategoryDto>> GetCategoriesCacheAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "categories_dictionary";

        if (!cache.TryGetValue(cacheKey, out Dictionary<Guid, CategoryDto>? dict))
        {
            var categoriesDict = await unitOfWork.Categories.GetCategoriesDictionaryAsync();

            dict = categoriesDict.ToDictionary(
                kvp => kvp.Key,
                kvp => new CategoryDto
                {
                    Id = kvp.Value.Id,
                    Name = kvp.Value.Name,
                    Code = kvp.Value.Code,
                    IsActive = kvp.Value.IsActive
                }
            );

            cache.Set(cacheKey, dict, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        return dict!;
    }

    public async Task<Dictionary<Guid, SupplierDto>> GetSuppliersCacheAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "suppliers_dictionary";

        if (!cache.TryGetValue(cacheKey, out Dictionary<Guid, SupplierDto>? dict))
        {
            var suppliersDict = await unitOfWork.Suppliers.GetSuppliersDictionaryAsync();

            dict = suppliersDict.ToDictionary(
                kvp => kvp.Key,
                kvp => new SupplierDto
                {
                    Id = kvp.Value.Id,
                    Name = kvp.Value.Name,
                    ContactEmail = kvp.Value.ContactEmail,
                    ContactPhone = kvp.Value.ContactPhone,
                    Country = kvp.Value.Address.Country,
                    City = kvp.Value.Address.City,
                    Street = kvp.Value.Address.Street,
                    PostalCode = kvp.Value.Address.PostalCode,
                    IsActive = kvp.Value.IsActive
                }
            );

            cache.Set(cacheKey, dict, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }

        return dict!;
    }

    public void InvalidateCache()
    {
        cache.Remove("products_by_category_lookup");
        cache.Remove("products_by_supplier_lookup");
        cache.Remove("products_by_country_lookup");
        cache.Remove("categories_dictionary");
        cache.Remove("suppliers_dictionary");
    }

    private static ProductDto MapProductToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            Stock = product.Stock,
            SKU = product.SKU,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Category = new CategoryDto
            {
                Id = product.Category.Id,
                Name = product.Category.Name,
                Code = product.Category.Code,
                IsActive = product.Category.IsActive
            },
            Supplier = new SupplierDto
            {
                Id = product.Supplier.Id,
                Name = product.Supplier.Name,
                ContactEmail = product.Supplier.ContactEmail,
                ContactPhone = product.Supplier.ContactPhone,
                Country = product.Supplier.Address.Country,
                City = product.Supplier.Address.City,
                Street = product.Supplier.Address.Street,
                PostalCode = product.Supplier.Address.PostalCode,
                IsActive = product.Supplier.IsActive
            }
        };
    }
}
