using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// Tag repository implementation with Entity Framework Core using BlogDbContext
    /// </summary>
    public class TagRepository : BlogBaseRepository<Tag>, ITagRepository
    {
        public TagRepository(BlogDbContext context) : base(context)
        {
        }

        public async Task<Tag?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant(), cancellationToken);
        }

        public async Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(t => EF.Functions.Like(t.Name, name), cancellationToken);
        }

        public async Task<IReadOnlyList<Tag>> GetByNamesAsync(
            IEnumerable<string> names,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var nameList = names.Select(n => n.ToLowerInvariant()).ToList();
            if (!nameList.Any())
                return new List<Tag>();

            var query = _dbSet.Where(t => nameList.Contains(t.Name.ToLower()));

            if (activeOnly)
                query = query.Where(t => t.IsActive);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Tag>> GetPopularAsync(
            int count = 50,
            bool activeOnly = true,
            int minUseCount = 1,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(t => t.UseCount >= minUseCount);

            if (activeOnly)
                query = query.Where(t => t.IsActive);

            return await query
                .OrderByDescending(t => t.UseCount)
                .ThenBy(t => t.Name)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TagTrendingInfo>> GetTrendingAsync(
            int count = 20,
            int daysBack = 7,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

            var query = from tag in _dbSet
                        join postTag in _context.PostTags on tag.Id equals postTag.TagId
                        join post in _context.Posts on postTag.PostId equals post.Id
                        where post.PublishedAt >= cutoffDate && post.Status == PostStatus.Published
                        group new { tag, postTag } by new { tag.Id, tag.Name, tag.Slug, tag.UseCount } into g
                        select new
                        {
                            Tag = g.Key,
                            RecentUsageCount = g.Count(),
                            TrendScore = (double)g.Count() / daysBack * g.Key.UseCount
                        };

            if (activeOnly)
            {
                query = query.Where(x => _dbSet.Any(t => t.Id == x.Tag.Id && t.IsActive));
            }

            var results = await query
                .OrderByDescending(x => x.TrendScore)
                .Take(count)
                .ToListAsync(cancellationToken);

            var tags = await _dbSet
                .Where(t => results.Select(r => r.Tag.Id).Contains(t.Id))
                .ToListAsync(cancellationToken);

            return results.Select(r => new TagTrendingInfo
            {
                Tag = tags.First(t => t.Id == r.Tag.Id),
                RecentUseCount = r.RecentUsageCount,
                TrendingScore = r.TrendScore
            }).ToList();
        }

        public async Task<IReadOnlyList<Tag>> SearchAsync(
            string searchTerm,
            bool activeOnly = true,
            int limit = 50,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Tag>();

            var query = _dbSet.Where(t =>
                EF.Functions.Like(t.Name, $"%{searchTerm}%") ||
                EF.Functions.Like(t.Description, $"%{searchTerm}%"));

            if (activeOnly)
                query = query.Where(t => t.IsActive);

            return await query
                .OrderByDescending(t => t.UseCount)
                .ThenBy(t => t.Name)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Tag>> GetSuggestionsAsync(
            string partialName,
            int limit = 10,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partialName))
                return new List<Tag>();

            var query = _dbSet.Where(t => t.Name.StartsWith(partialName));

            if (activeOnly)
                query = query.Where(t => t.IsActive);

            return await query
                .OrderByDescending(t => t.UseCount)
                .ThenBy(t => t.Name)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RelatedTagInfo>> GetRelatedTagsAsync(
            Guid tagId,
            int count = 10,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            // Find posts that have the specified tag
            var postsWithTag = await _context.PostTags
                .Where(pt => pt.TagId == tagId)
                .Select(pt => pt.PostId)
                .ToListAsync(cancellationToken);

            if (!postsWithTag.Any())
                return new List<RelatedTagInfo>();

            // Find other tags that appear in the same posts
            var query = from pt in _context.PostTags
                        join tag in _dbSet on pt.TagId equals tag.Id
                        where postsWithTag.Contains(pt.PostId) && pt.TagId != tagId
                        group pt by new { tag.Id, tag.Name, tag.Slug, tag.UseCount } into g
                        select new
                        {
                            Tag = g.Key,
                            CoOccurrenceCount = g.Count(),
                            RelatedScore = (double)g.Count() / postsWithTag.Count * g.Key.UseCount
                        };

            if (activeOnly)
            {
                query = query.Where(x => _dbSet.Any(t => t.Id == x.Tag.Id && t.IsActive));
            }

            var results = await query
                .OrderByDescending(x => x.RelatedScore)
                .Take(count)
                .ToListAsync(cancellationToken);

            var tags = await _dbSet
                .Where(t => results.Select(r => r.Tag.Id).Contains(t.Id))
                .ToListAsync(cancellationToken);

            return results.Select(r => new RelatedTagInfo
            {
                Tag = tags.First(t => t.Id == r.Tag.Id),
                CoOccurrenceCount = r.CoOccurrenceCount,
                CorrelationScore = r.RelatedScore
            }).ToList();
        }

        public async Task<IReadOnlyList<Tag>> GetUnusedTagsAsync(
            int? olderThanDays = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(t => t.UseCount == 0);

            if (olderThanDays.HasValue)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays.Value);
                query = query.Where(t => t.CreatedAt < cutoffDate);
            }

            return await query
                .OrderBy(t => t.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TagWithPostCount>> GetTagsWithPostCountsAsync(
            bool activeOnly = true,
            bool publishedPostsOnly = true,
            bool orderByUseCount = true,
            CancellationToken cancellationToken = default)
        {
            var query = from tag in _dbSet
                        select new
                        {
                            Tag = tag,
                            PostCount = publishedPostsOnly
                                ? tag.PostTags.Count(pt => pt.Post.Status == PostStatus.Published)
                                : tag.PostTags.Count(),
                            PublishedPostCount = tag.PostTags.Count(pt => pt.Post.Status == PostStatus.Published),
                            LastUsedDate = tag.PostTags.Max(pt => (DateTime?)pt.CreatedAt)
                        };

            if (activeOnly)
                query = query.Where(t => t.Tag.IsActive);

            if (orderByUseCount)
                query = query.OrderByDescending(t => t.Tag.UseCount).ThenBy(t => t.Tag.Name);
            else
                query = query.OrderBy(t => t.Tag.Name);

            var results = await query.ToListAsync(cancellationToken);

            return results.Select(r => new TagWithPostCount
            {
                Tag = r.Tag,
                PostCount = r.PostCount,
                PublishedPostCount = r.PublishedPostCount,
                LastUsedDate = r.LastUsedDate
            }).ToList();
        }

        public async Task<IReadOnlyList<Tag>> GetByPopularityLevelAsync(
            TagPopularityLevel popularityLevel,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var (minUse, maxUse) = popularityLevel switch
            {
                TagPopularityLevel.Unused => (0, 0),
                TagPopularityLevel.Low => (1, 5),
                TagPopularityLevel.Medium => (6, 20),
                TagPopularityLevel.High => (21, 50),
                TagPopularityLevel.VeryHigh => (51, int.MaxValue),
                _ => (0, int.MaxValue)
            };

            var query = _dbSet.Where(t => t.UseCount >= minUse && t.UseCount <= maxUse);

            if (activeOnly)
                query = query.Where(t => t.IsActive);

            return await query
                .OrderByDescending(t => t.UseCount)
                .ThenBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Tag>> GetOrCreateTagsAsync(
            IEnumerable<string> tagNames,
            Guid? createdByUserId = null,
            CancellationToken cancellationToken = default)
        {
            var cleanTagNames = tagNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!cleanTagNames.Any())
                return new List<Tag>();

            // Find existing tags
            var existingTags = await GetByNamesAsync(cleanTagNames, false, cancellationToken);
            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Create missing tags
            var newTags = new List<Tag>();
            foreach (var tagName in cleanTagNames.Where(name => !existingTagNames.Contains(name)))
            {
                try
                {
                    var newTag = new Tag();
                    newTag.SetNameAndSlug(tagName);
                    await AddAsync(newTag, cancellationToken);
                    newTags.Add(newTag);
                }
                catch (ArgumentException)
                {
                    // Skip invalid tag names
                    continue;
                }
            }

            if (newTags.Any())
                await SaveChangesAsync(cancellationToken);

            return existingTags.Concat(newTags).ToList();
        }

        public async Task<int> UpdateUseCountsAsync(
            IDictionary<Guid, int> tagUsageCounts,
            CancellationToken cancellationToken = default)
        {
            var tagIds = tagUsageCounts.Keys.ToList();
            var tags = await _dbSet
                .Where(t => tagIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var tag in tags)
            {
                if (tagUsageCounts.TryGetValue(tag.Id, out var newCount))
                {
                    tag.UpdateUseCount(newCount);
                }
            }

            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> BulkUpdateStatusAsync(
            IEnumerable<Guid> tagIds,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            var tagIdList = tagIds.ToList();
            if (!tagIdList.Any())
                return 0;

            var tags = await _dbSet
                .Where(t => tagIdList.Contains(t.Id))
                .ToListAsync(cancellationToken);

            foreach (var tag in tags)
            {
                if (isActive)
                    tag.Activate();
                else
                    tag.Deactivate();
            }

            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> MergeTagsAsync(
            IEnumerable<Guid> sourceTagIds,
            Guid targetTagId,
            CancellationToken cancellationToken = default)
        {
            var sourceTagIdList = sourceTagIds.ToList();
            if (!sourceTagIdList.Any() || sourceTagIdList.Contains(targetTagId))
                return 0;

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Get source tags to calculate merged use count
                var sourceTags = await _dbSet
                    .Where(t => sourceTagIdList.Contains(t.Id))
                    .ToListAsync(cancellationToken);

                var targetTag = await _dbSet
                    .FirstOrDefaultAsync(t => t.Id == targetTagId, cancellationToken);

                if (targetTag == null)
                    return 0;

                // Get all PostTags for source tags
                var sourcePostTags = await _context.PostTags
                    .Where(pt => sourceTagIdList.Contains(pt.TagId))
                    .ToListAsync(cancellationToken);

                // Update PostTags to point to target tag (avoiding duplicates)
                var existingTargetPostIds = await _context.PostTags
                    .Where(pt => pt.TagId == targetTagId)
                    .Select(pt => pt.PostId)
                    .ToListAsync(cancellationToken);

                var newPostTags = sourcePostTags
                    .Where(pt => !existingTargetPostIds.Contains(pt.PostId))
                    .Select(pt => new PostTag
                    {
                        PostId = pt.PostId,
                        TagId = targetTagId,
                        CreatedAt = pt.CreatedAt
                    })
                    .ToList();

                if (newPostTags.Any())
                {
                    await _context.PostTags.AddRangeAsync(newPostTags, cancellationToken);
                }

                // Remove old PostTags
                _context.PostTags.RemoveRange(sourcePostTags);

                // Update target tag use count
                var totalMergedUseCount = sourceTags.Sum(t => t.UseCount);
                targetTag.UpdateUseCount(targetTag.UseCount + totalMergedUseCount);

                // Remove source tags
                _dbSet.RemoveRange(sourceTags);

                var result = await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> IsSlugAvailableAsync(
            string slug,
            Guid? excludeTagId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            var query = _dbSet.Where(t => t.Slug == slug.ToLowerInvariant());

            if (excludeTagId.HasValue)
                query = query.Where(t => t.Id != excludeTagId.Value);

            return !await query.AnyAsync(cancellationToken);
        }

        public async Task<TagStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var totalTags = await _dbSet.CountAsync(cancellationToken);
            var activeTags = await _dbSet.CountAsync(t => t.IsActive, cancellationToken);
            var inactiveTags = totalTags - activeTags;
            var usedTags = await _dbSet.CountAsync(t => t.UseCount > 0, cancellationToken);
            var unusedTags = totalTags - usedTags;

            var totalTagUsages = await _dbSet.SumAsync(t => t.UseCount, cancellationToken);
            var averageUsagePerTag = totalTags > 0 ? (double)totalTagUsages / totalTags : 0;

            var mostUsedTag = await _dbSet
                .OrderByDescending(t => t.UseCount)
                .FirstOrDefaultAsync(cancellationToken);

            var mostRecentTag = await _dbSet
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var tagsByPopularity = new Dictionary<TagPopularityLevel, int>();
            foreach (TagPopularityLevel level in Enum.GetValues<TagPopularityLevel>())
            {
                var tagsAtLevel = await GetByPopularityLevelAsync(level, false, cancellationToken);
                tagsByPopularity[level] = tagsAtLevel.Count;
            }

            return new TagStatistics
            {
                TotalTags = totalTags,
                ActiveTags = activeTags,
                InactiveTags = inactiveTags,
                UsedTags = usedTags,
                UnusedTags = unusedTags,
                TotalTagUsages = totalTagUsages,
                AverageUsagePerTag = averageUsagePerTag,
                MostUsedTag = mostUsedTag,
                MostUsedTagCount = mostUsedTag?.UseCount ?? 0,
                MostRecentTag = mostRecentTag,
                TagsByPopularity = tagsByPopularity
            };
        }

        /// <summary>
        /// 获取最常用的标签
        /// </summary>
        public async Task<IReadOnlyList<Tag>> GetMostUsedAsync(int skip, int take, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.IsActive && t.UseCount > 0)
                .OrderByDescending(t => t.UseCount)
                .ThenBy(t => t.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 根据ID列表获取标签
        /// </summary>
        public async Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();
            if (!idList.Any())
                return new List<Tag>();

            return await _dbSet
                .Where(t => idList.Contains(t.Id))
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }
    }
}