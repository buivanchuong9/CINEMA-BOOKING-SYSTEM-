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
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate); // Expression biến cái hàm đó sang Entity để đọc sang SQL , predicate điều kiện lọc
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate); // Lấy phần tử đầu tiên thỏa mãn điều kiện
    
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
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null); // Đếm số lượng entity thỏa mãn điều kiện
    
    // EXISTS Operation
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate); // Kiểm tra xem có entity nào thỏa mãn điều kiện không
}
