namespace Domain.Repositories;

using Domain.Entities;

public interface ISupplierRepository : IGenericRepository<Supplier>
{
    Task<IEnumerable<Supplier>> GetActiveSuppliersAsync();
    Task<ILookup<string, Supplier>> GetSuppliersLookupByCountryAsync();
    Task<Dictionary<Guid, Supplier>> GetSuppliersDictionaryAsync();
}
