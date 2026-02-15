namespace Application.Services;

using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;
using System.Collections.Frozen;

public class ModernCSharpService(IUnitOfWork unitOfWork)
{
    private int _cacheVersion;

    public int CacheVersion
    {
        get => _cacheVersion;
        set
        {
            if (value < 0) throw new ArgumentException("Version cannot be negative");
            _cacheVersion = value;
        }
    }

    public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync(CancellationToken cancellationToken = default)
    {
        var electronics = await unitOfWork.Products.GetProductsLookupByCategoryAsync(cancellationToken);
        var elecId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var furnId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        IEnumerable<Product> featured = [
            ..(electronics[elecId].Take(3)),
            ..(electronics[furnId].Take(2))
        ];
        return featured.Select(MapToDto);
    }

    public async Task<Dictionary<string, int>> GetStockForCountriesAsync(CancellationToken cancellationToken, params string[] countries)
    {
        var allStock = await unitOfWork.Products.GetStockByCountryAsync(cancellationToken);
        var result = new Dictionary<string, int>();
        foreach (var country in countries)
        {
            if (allStock.TryGetValue(country, out var stock))
                result[country] = stock;
        }
        return result;
    }

    public async Task<FrozenDictionary<Guid, CategoryDto>> GetCategoriesFrozenAsync(CancellationToken cancellationToken = default)
    {
        var categories = await unitOfWork.Categories.GetAllAsync(cancellationToken);
        return categories.ToFrozenDictionary(
            c => c.Id,
            c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                IsActive = c.IsActive
            });
    }

    public static string GetProductPriceCategory(ProductDto product) => product.Price switch
    {
        < 1000 => "Budget",
        >= 1000 and < 10000 => "Mid-range",
        >= 10000 and < 100000 => "Premium",
        _ => "Luxury"
    };

    private static ProductDto MapToDto(Product product)
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
            Category = product.Category != null ? new CategoryDto
            {
                Id = product.Category.Id,
                Name = product.Category.Name,
                Code = product.Category.Code,
                IsActive = product.Category.IsActive
            } : null,
            Supplier = product.Supplier != null ? new SupplierDto
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
            } : null
        };
    }
}
