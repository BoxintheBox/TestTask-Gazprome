namespace Infrastructure.Data;

using Domain.Entities;
using Domain.QueryFilters;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().HasQueryFilter(IQueryFilterProvider.SoftDeleteFilter, p => !p.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(IQueryFilterProvider.SoftDeleteFilter, c => !c.IsDeleted);
        modelBuilder.Entity<Supplier>().HasQueryFilter(IQueryFilterProvider.SoftDeleteFilter, s => !s.IsDeleted);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.ComplexProperty(e => e.Price, b =>
            {
                b.Property(p => p.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)");
                b.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.SupplierId);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Supplier)
                  .WithMany(s => s.Products)
                  .HasForeignKey(p => p.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.ComplexProperty(e => e.Address, b =>
            {
                b.Property(a => a.Country).HasColumnName("Country").IsRequired().HasMaxLength(100);
                b.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
                b.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
                b.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
            });
            entity.HasIndex(e => e.IsActive);
        });
    }

    public async Task SeedIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        if (await Categories.AnyAsync(cancellationToken))
            return;

        var electronicsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var furnitureId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var booksId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var supplier1Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var supplier2Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var supplier3Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var now = DateTime.UtcNow;

        var categories = new[]
        {
            new Category { Id = electronicsId, Name = "Электроника", Code = "ELEC", IsActive = true, CreatedAt = now },
            new Category { Id = furnitureId, Name = "Мебель", Code = "FURN", IsActive = true, CreatedAt = now },
            new Category { Id = booksId, Name = "Книги", Code = "BOOK", IsActive = true, CreatedAt = now }
        };
        await Categories.AddRangeAsync(categories, cancellationToken);

        var suppliers = new[]
        {
            new Supplier { Id = supplier1Id, Name = "TechCorp", Address = new Address { Country = "США" }, ContactEmail = "tech@corp.com", ContactPhone = "+1-555-0100", IsActive = true, CreatedAt = now },
            new Supplier { Id = supplier2Id, Name = "FurnitureWorld", Address = new Address { Country = "Германия" }, ContactEmail = "info@furniture.de", ContactPhone = "+49-30-12345", IsActive = true, CreatedAt = now },
            new Supplier { Id = supplier3Id, Name = "BookDistributor", Address = new Address { Country = "Россия" }, ContactEmail = "books@dist.ru", ContactPhone = "+7-495-1234567", IsActive = true, CreatedAt = now }
        };
        await Suppliers.AddRangeAsync(suppliers, cancellationToken);
        await SaveChangesAsync(cancellationToken);

        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), Name = "Ноутбук", Description = "Мощный ноутбук", SKU = "LAP-001", Price = new Money { Amount = 89999m, Currency = "RUB" }, Stock = 15, CategoryId = electronicsId, SupplierId = supplier1Id, CreatedAt = now },
            new Product { Id = Guid.NewGuid(), Name = "Смартфон", Description = "Современный смартфон", SKU = "PHN-001", Price = new Money { Amount = 45999m, Currency = "RUB" }, Stock = 30, CategoryId = electronicsId, SupplierId = supplier1Id, CreatedAt = now },
            new Product { Id = Guid.NewGuid(), Name = "Стол письменный", Description = "Деревянный стол", SKU = "DSK-001", Price = new Money { Amount = 12999m, Currency = "RUB" }, Stock = 8, CategoryId = furnitureId, SupplierId = supplier2Id, CreatedAt = now },
            new Product { Id = Guid.NewGuid(), Name = "Кресло офисное", Description = "Эргономичное кресло", SKU = "CHR-001", Price = new Money { Amount = 8999m, Currency = "RUB" }, Stock = 12, CategoryId = furnitureId, SupplierId = supplier2Id, CreatedAt = now },
            new Product { Id = Guid.NewGuid(), Name = "Война и мир", Description = "Роман Толстого", SKU = "BOK-001", Price = new Money { Amount = 899m, Currency = "RUB" }, Stock = 50, CategoryId = booksId, SupplierId = supplier3Id, CreatedAt = now },
            new Product { Id = Guid.NewGuid(), Name = "Преступление и наказание", Description = "Роман Достоевского", SKU = "BOK-002", Price = new Money { Amount = 699m, Currency = "RUB" }, Stock = 40, CategoryId = booksId, SupplierId = supplier3Id, CreatedAt = now }
        };
        await Products.AddRangeAsync(products, cancellationToken);
        await SaveChangesAsync(cancellationToken);
    }
}
