namespace Domain.Specifications;

using Domain.Entities;

public class ProductsWithIncludesSpec : BaseSpecification<Product>
{
    public ProductsWithIncludesSpec() : base(p => !p.IsDeleted)
    {
        AddInclude(p => p.Category);
        AddInclude(p => p.Supplier);
        ApplyOrderBy(p => p.Name);
        ApplyNoTracking();
        ApplySplitQuery();
    }
}

public class ProductsByCategorySpec : BaseSpecification<Product>
{
    public ProductsByCategorySpec(Guid categoryId)
        : base(p => !p.IsDeleted && p.CategoryId == categoryId)
    {
        AddInclude(p => p.Category);
        AddInclude(p => p.Supplier);
        ApplyNoTracking();
        ApplySplitQuery();
    }
}

public class ProductsBySupplierSpec : BaseSpecification<Product>
{
    public ProductsBySupplierSpec(Guid supplierId)
        : base(p => !p.IsDeleted && p.SupplierId == supplierId)
    {
        AddInclude(p => p.Category);
        AddInclude(p => p.Supplier);
        ApplyNoTracking();
        ApplySplitQuery();
    }
}

public class ProductsPagedSpec : BaseSpecification<Product>
{
    public ProductsPagedSpec(int pageNumber, int pageSize)
        : base(p => !p.IsDeleted)
    {
        AddInclude(p => p.Category);
        AddInclude(p => p.Supplier);
        ApplyOrderBy(p => p.Name);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        ApplyNoTracking();
        ApplySplitQuery();
    }
}
