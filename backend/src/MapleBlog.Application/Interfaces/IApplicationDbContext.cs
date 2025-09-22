using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Application database context interface
    /// </summary>
    public interface IApplicationDbContext
    {
        /// <summary>
        /// Users DbSet
        /// </summary>
        DbSet<User> Users { get; }

        /// <summary>
        /// Posts DbSet
        /// </summary>
        DbSet<Post> Posts { get; }

        /// <summary>
        /// Categories DbSet
        /// </summary>
        DbSet<Category> Categories { get; }

        /// <summary>
        /// Tags DbSet
        /// </summary>
        DbSet<Tag> Tags { get; }

        /// <summary>
        /// Email Verification Tokens DbSet
        /// </summary>
        DbSet<EmailVerificationToken> EmailVerificationTokens { get; }

        /// <summary>
        /// Search Queries DbSet
        /// </summary>
        DbSet<SearchQuery> SearchQueries { get; }

        /// <summary>
        /// Search Indexes DbSet
        /// </summary>
        DbSet<SearchIndex> SearchIndexes { get; }

        /// <summary>
        /// Popular Searches DbSet
        /// </summary>
        DbSet<PopularSearch> PopularSearches { get; }

        /// <summary>
        /// Save changes asynchronously
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected entries</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}