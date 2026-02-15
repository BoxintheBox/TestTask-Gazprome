namespace API.Controllers;

using Application.DTOs;
using Application.Services;
using API.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService, IHubContext<ProductHub> hubContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await productService.GetAllProductsAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductByIdAsync(id, cancellationToken);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto createDto, CancellationToken cancellationToken)
    {
        var product = await productService.CreateProductAsync(createDto, cancellationToken);
        await hubContext.Clients.All.SendAsync("ProductCreated", product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductDto updateDto, CancellationToken cancellationToken)
    {
        if (id != updateDto.Id) return BadRequest();

        var product = await productService.UpdateProductAsync(updateDto, cancellationToken);
        if (product == null) return NotFound();

        await hubContext.Clients.All.SendAsync("ProductUpdated", product);
        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await productService.DeleteProductAsync(id, cancellationToken);
        if (!result) return NotFound();

        await hubContext.Clients.All.SendAsync("ProductDeleted", id);
        return NoContent();
    }
}
