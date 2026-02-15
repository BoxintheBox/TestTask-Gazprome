namespace Application.Services;

using Application.DTOs;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;

public class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await unitOfWork.Products.GetAllWithIncludesAsync(cancellationToken);
        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Products.GetByIdWithIncludesAsync(id, cancellationToken);
        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createDto, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Description = createDto.Description,
            Price = new Money { Amount = createDto.Price, Currency = createDto.Currency ?? "RUB" },
            Stock = createDto.Stock,
            SKU = createDto.SKU,
            CategoryId = createDto.CategoryId,
            SupplierId = createDto.SupplierId,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await unitOfWork.Products.GetByIdWithIncludesAsync(product.Id, cancellationToken);
        return MapToDto(created!);
    }

    public async Task<ProductDto?> UpdateProductAsync(UpdateProductDto updateDto, CancellationToken cancellationToken = default)
    {
        var existing = await unitOfWork.Products.GetByIdWithIncludesAsync(updateDto.Id, cancellationToken);
        if (existing == null) return null;

        existing.Name = updateDto.Name;
        existing.Description = updateDto.Description;
        existing.Price = new Money { Amount = updateDto.Price, Currency = updateDto.Currency ?? "RUB" };
        existing.Stock = updateDto.Stock;
        existing.SKU = updateDto.SKU;
        existing.CategoryId = updateDto.CategoryId;
        existing.SupplierId = updateDto.SupplierId;
        existing.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.Products.UpdateAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await unitOfWork.Products.GetByIdWithIncludesAsync(updateDto.Id, cancellationToken);
        return updated == null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product == null) return false;

        await unitOfWork.Products.SoftDeleteAsync(id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

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
