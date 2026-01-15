using System.Linq.Expressions;

namespace BE.Core.Interfaces;

/// <summary>
/// Generic Repository Interface - Base CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    // READ Operations
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    
    // CREATE Operation
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    
    // UPDATE Operation
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    
    // DELETE Operation
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    
    // COUNT Operation
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    
    // EXISTS Operation
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}
