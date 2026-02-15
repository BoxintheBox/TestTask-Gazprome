namespace Domain.Entities;

using Domain.ValueObjects;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Money Price { get; set; } = new();
    public int Stock { get; set; }
    public string SKU { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }
    public Guid SupplierId { get; set; }

    public Category Category { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}
