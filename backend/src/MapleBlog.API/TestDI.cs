using Microsoft.Extensions.DependencyInjection;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.API
{
    /// <summary>
    /// Simple DI test class to verify dependency injection configuration
    /// </summary>
    public static class DITest
    {
        /// <summary>
        /// Test method to verify DI configuration works correctly
        /// </summary>
        public static bool TestDependencyInjection(IServiceProvider serviceProvider)
        {
            try
            {
                // Test BlogDbContext registration
                var blogDbContext = serviceProvider.GetRequiredService<BlogDbContext>();
                if (blogDbContext == null)
                {
                    Console.WriteLine("❌ BlogDbContext DI 失败");
                    return false;
                }
                Console.WriteLine("✅ BlogDbContext DI 成功");

                // Test Repository registrations
                var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
                if (userRepository == null)
                {
                    Console.WriteLine("❌ IUserRepository DI 失败");
                    return false;
                }
                Console.WriteLine("✅ IUserRepository DI 成功");

                var postRepository = serviceProvider.GetRequiredService<IPostRepository>();
                if (postRepository == null)
                {
                    Console.WriteLine("❌ IPostRepository DI 失败");
                    return false;
                }
                Console.WriteLine("✅ IPostRepository DI 成功");

                var categoryRepository = serviceProvider.GetRequiredService<ICategoryRepository>();
                if (categoryRepository == null)
                {
                    Console.WriteLine("❌ ICategoryRepository DI 失败");
                    return false;
                }
                Console.WriteLine("✅ ICategoryRepository DI 成功");

                var tagRepository = serviceProvider.GetRequiredService<ITagRepository>();
                if (tagRepository == null)
                {
                    Console.WriteLine("❌ ITagRepository DI 失败");
                    return false;
                }
                Console.WriteLine("✅ ITagRepository DI 成功");

                Console.WriteLine("🎉 所有DI配置测试通过！");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DI测试失败: {ex.Message}");
                return false;
            }
        }
    }
}