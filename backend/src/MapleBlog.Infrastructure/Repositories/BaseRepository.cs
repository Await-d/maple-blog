using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// Legacy base repository - now delegates to BlogBaseRepository for compatibility
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from BaseEntity</typeparam>
    [Obsolete("Use BlogBaseRepository<T> instead. This class will be removed in a future version.")]
    public class BaseRepository<T> : BlogBaseRepository<T> where T : BaseEntity
    {
        public BaseRepository(BlogDbContext context) : base(context)
        {
        }

        // Obsolete constructor for ApplicationDbContext - for backwards compatibility
        [Obsolete("ApplicationDbContext is deprecated. Use BlogDbContext instead.")]
        public BaseRepository(ApplicationDbContext context) : base(ConvertContext(context))
        {
        }

        private static BlogDbContext ConvertContext(ApplicationDbContext appContext)
        {
            // This is a temporary solution for backwards compatibility
            // In practice, you should inject BlogDbContext directly
            var optionsBuilder = new DbContextOptionsBuilder<BlogDbContext>();
            var connectionString = appContext.Database.GetConnectionString();
            optionsBuilder.UseSqlite(connectionString);
            return new BlogDbContext(optionsBuilder.Options);
        }

        // All methods are now inherited from BlogBaseRepository<T>
        // This class exists only for backwards compatibility
    }
}