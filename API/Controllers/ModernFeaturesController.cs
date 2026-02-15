namespace API.Controllers;

using Application.DTOs;
using Application.Services;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ModernFeaturesController(IUnitOfWork unitOfWork, ModernCSharpService modernService) : ControllerBase
{
    [HttpPost("bulk-update-prices")]
    public async Task<IActionResult> BulkUpdatePrices([FromQuery] decimal multiplier = 1.1m, [FromQuery] string currency = "RUB", CancellationToken cancellationToken = default)
    {
        var updated = await unitOfWork.Products.BulkUpdatePricesAsync(new Money { Amount = multiplier, Currency = currency }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Updated {updated} products", multiplier, currency });
    }

    [HttpPost("update-product-price/{id}")]
    public async Task<IActionResult> UpdateProductPrice(Guid id, [FromBody] Money newPrice, CancellationToken cancellationToken = default)
    {
        var updated = await unitOfWork.Products.UpdatePriceToValueAsync(id, newPrice, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Updated {updated} product(s)", newPrice });
    }

    [HttpDelete("bulk-delete-out-of-stock")]
    public async Task<IActionResult> BulkDeleteOutOfStock(CancellationToken cancellationToken = default)
    {
        var deleted = await unitOfWork.Products.BulkDeleteOutOfStockAsync(cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Deleted {deleted} out-of-stock products" });
    }

    [HttpGet("products-including-deleted")]
    public async Task<IActionResult> GetProductsIncludingDeleted(CancellationToken cancellationToken = default)
    {
        var products = await unitOfWork.Products.GetAllIncludingDeletedAsync(cancellationToken);
        return Ok(new { message = "Retrieved products including soft-deleted", count = products.Count(), products });
    }

    [HttpGet("featured-products")]
    public async Task<IActionResult> GetFeaturedProducts(CancellationToken cancellationToken = default)
    {
        var featured = await modernService.GetFeaturedProductsAsync(cancellationToken);
        return Ok(featured);
    }

    [HttpGet("stock-by-countries")]
    public async Task<IActionResult> GetStockByCountries([FromQuery] string[] countries, CancellationToken cancellationToken = default)
    {
        var stock = await modernService.GetStockForCountriesAsync(cancellationToken, countries);
        return Ok(stock);
    }

    [HttpGet("categories-frozen")]
    public async Task<IActionResult> GetCategoriesFrozen(CancellationToken cancellationToken = default)
    {
        var frozen = await modernService.GetCategoriesFrozenAsync(cancellationToken);
        return Ok(new { message = "FrozenDictionary - optimized for lookups, immutable", count = frozen.Count, categories = frozen.Values });
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string q, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Search term required");
        var results = await unitOfWork.Products.SearchByNameAsync(q, cancellationToken);
        return Ok(new { message = "Using compiled query for performance", searchTerm = q, count = results.Count(), results });
    }

    [HttpGet("products-with-min-max-price")]
    public async Task<IActionResult> GetProductsWithMinMaxPrice([FromQuery] decimal min = 0, [FromQuery] decimal max = 1_000_000, CancellationToken cancellationToken = default)
    {
        var products = await unitOfWork.Products.GetProductsWithMinMaxPriceAsync(min, max, cancellationToken);
        return Ok(new { message = "Products in price range", count = products.Count(), products });
    }

    [HttpGet("price-category/{price:decimal}")]
    public IActionResult GetPriceCategory([FromRoute] decimal price)
    {
        var category = price switch
        {
            < 1000 => "Budget",
            >= 1000 and < 10000 => "Mid-range",
            >= 10000 and < 100000 => "Premium",
            _ => "Luxury"
        };
        return Ok(new { price, category });
    }
}
