namespace Infrastructure.Repositories;

using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

public class UnitOfWork(AppDbContext context, ILoggerFactory loggerFactory) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public IProductRepository Products { get; } = new ProductRepository(context, loggerFactory.CreateLogger<ProductRepository>());
    public ICategoryRepository Categories { get; } = new CategoryRepository(context, loggerFactory.CreateLogger<CategoryRepository>());
    public ISupplierRepository Suppliers { get; } = new SupplierRepository(context, loggerFactory.CreateLogger<SupplierRepository>());

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
