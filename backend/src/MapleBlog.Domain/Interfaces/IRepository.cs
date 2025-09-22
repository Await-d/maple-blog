using System.Linq.Expressions;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Generic repository interface for domain entities
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
    public interface IRepository<T> where T : BaseEntity
    {
        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Entity if found, null otherwise</returns>
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all entities</returns>
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds entities matching the specified criteria
        /// </summary>
        /// <param name="predicate">Search criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching entities</returns>
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the first entity matching the specified criteria
        /// </summary>
        /// <param name="predicate">Search criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>First matching entity or null</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if any entity matches the specified criteria
        /// </summary>
        /// <param name="predicate">Search criteria</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if any entity matches, false otherwise</returns>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of entities matching the specified criteria
        /// </summary>
        /// <param name="predicate">Search criteria (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of matching entities</returns>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets entities with paging support
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of entities per page</param>
        /// <param name="predicate">Optional filter criteria</param>
        /// <param name="orderBy">Optional ordering function</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paged list of entities</returns>
        Task<IReadOnlyList<T>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new entity
        /// </summary>
        /// <param name="entity">Entity to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Added entity</returns>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple entities
        /// </summary>
        /// <param name="entities">Entities to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <returns>Updated entity</returns>
        T Update(T entity);

        /// <summary>
        /// Updates multiple entities
        /// </summary>
        /// <param name="entities">Entities to update</param>
        void UpdateRange(IEnumerable<T> entities);

        /// <summary>
        /// Removes an entity
        /// </summary>
        /// <param name="entity">Entity to remove</param>
        void Remove(T entity);

        /// <summary>
        /// Removes an entity by ID
        /// </summary>
        /// <param name="id">ID of the entity to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes multiple entities
        /// </summary>
        /// <param name="entities">Entities to remove</param>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// Removes entities matching the specified criteria
        /// </summary>
        /// <param name="predicate">Criteria for entities to remove</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RemoveRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable interface for the entity
        /// </summary>
        /// <returns>IQueryable for the entity type</returns>
        IQueryable<T> GetQueryable();

        /// <summary>
        /// Saves all pending changes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected entities</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a database transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Database transaction</returns>
        Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a database transaction
    /// </summary>
    public interface IDbTransaction : IDisposable
    {
        /// <summary>
        /// Commits the transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}