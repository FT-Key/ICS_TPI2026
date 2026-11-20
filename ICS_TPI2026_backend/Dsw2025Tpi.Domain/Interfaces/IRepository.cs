using Dsw2025Tpi.Domain.Entities;
using System.Linq.Expressions;

namespace Dsw2025Tpi.Domain.Interfaces;

public interface IRepository
{
    Task<T?> GetById<T>(Guid id, params string[] include) where T : EntityBase;
    Task<IEnumerable<T>?> GetAll<T>(params string[] include) where T : EntityBase;
    Task<T?> First<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase;
    Task<IEnumerable<T>?> GetFiltered<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase;
    Task<T> Add<T>(T entity) where T : EntityBase;
    Task<T> Update<T>(T entity) where T : EntityBase;
    Task<T> Delete<T>(T entity) where T : EntityBase;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta la operación dentro de una transacción de base de datos: si algo falla,
    /// ningún cambio se persiste (TP4-02, alta de orden + descuento de stock).
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
