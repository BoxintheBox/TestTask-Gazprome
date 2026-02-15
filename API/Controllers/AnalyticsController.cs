namespace API.Controllers;

using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet("group-by-category")]
    public async Task<ActionResult> GroupByCategory(CancellationToken cancellationToken)
    {
        var grouping = await unitOfWork.Products.GroupByCategoryAsync(cancellationToken);
        return Ok(grouping);
    }

    [HttpGet("group-by-supplier")]
    public async Task<ActionResult> GroupBySupplier(CancellationToken cancellationToken)
    {
        var grouping = await unitOfWork.Products.GroupBySupplierAsync(cancellationToken);
        return Ok(grouping);
    }

    [HttpGet("stock-by-country")]
    public async Task<ActionResult> GetStockByCountry(CancellationToken cancellationToken)
    {
        var stockDict = await unitOfWork.Products.GetStockByCountryAsync(cancellationToken);
        return Ok(stockDict);
    }

    [HttpPost("bulk-update-prices")]
    public async Task<ActionResult> BulkUpdatePrices([FromQuery] decimal multiplier = 1.1m, [FromQuery] string currency = "RUB", CancellationToken cancellationToken = default)
    {
        var updated = await unitOfWork.Products.BulkUpdatePricesAsync(new Money { Amount = multiplier, Currency = currency }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(new { message = $"Updated {updated} products", multiplier, currency });
    }
}
