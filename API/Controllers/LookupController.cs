namespace API.Controllers;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LookupController(ILookupCacheService lookupCache) : ControllerBase
{
    [HttpGet("products-by-category")]
    public async Task<ActionResult> GetProductsByCategory([FromQuery] Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var lookup = await lookupCache.GetProductsByCategoryLookupAsync(cancellationToken);

        if (categoryId.HasValue)
        {
            var products = lookup[categoryId.Value].ToList();
            return Ok(new { categoryId, products, count = products.Count });
        }

        var grouped = lookup
            .Select(g => new
            {
                categoryId = g.Key,
                categoryName = g.First().Category?.Name,
                products = g.ToList(),
                count = g.Count()
            })
            .ToList();

        return Ok(grouped);
    }

    [HttpGet("products-by-supplier")]
    public async Task<ActionResult> GetProductsBySupplier([FromQuery] Guid? supplierId = null, CancellationToken cancellationToken = default)
    {
        var lookup = await lookupCache.GetProductsBySupplierLookupAsync(cancellationToken);

        if (supplierId.HasValue)
        {
            var products = lookup[supplierId.Value].ToList();
            return Ok(new { supplierId, products, count = products.Count });
        }

        var grouped = lookup
            .Select(g => new
            {
                supplierId = g.Key,
                supplierName = g.First().Supplier?.Name,
                products = g.ToList(),
                count = g.Count()
            })
            .ToList();

        return Ok(grouped);
    }

    [HttpGet("products-by-country")]
    public async Task<ActionResult> GetProductsByCountry([FromQuery] string? country = null, CancellationToken cancellationToken = default)
    {
        var lookup = await lookupCache.GetProductsByCountryLookupAsync(cancellationToken);

        if (!string.IsNullOrEmpty(country))
        {
            var products = lookup[country].ToList();
            return Ok(new { country, products, count = products.Count });
        }

        var grouped = lookup
            .Select(g => new
            {
                country = g.Key,
                products = g.ToList(),
                count = g.Count(),
                totalStock = g.Sum(p => p.Stock)
            })
            .OrderByDescending(x => x.count)
            .ToList();

        return Ok(grouped);
    }

    [HttpGet("categories-cache")]
    public async Task<ActionResult<Dictionary<Guid, CategoryDto>>> GetCategoriesCache(CancellationToken cancellationToken = default)
    {
        var cache = await lookupCache.GetCategoriesCacheAsync(cancellationToken);
        return Ok(cache);
    }

    [HttpGet("suppliers-cache")]
    public async Task<ActionResult<Dictionary<Guid, SupplierDto>>> GetSuppliersCache(CancellationToken cancellationToken = default)
    {
        var cache = await lookupCache.GetSuppliersCacheAsync(cancellationToken);
        return Ok(cache);
    }

    [HttpPost("invalidate-cache")]
    public ActionResult InvalidateCache()
    {
        lookupCache.InvalidateCache();
        return Ok(new { message = "Cache invalidated successfully" });
    }
}
