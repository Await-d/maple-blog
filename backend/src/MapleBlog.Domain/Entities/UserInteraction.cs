using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities
{
    /// <summary>
    /// User interaction entity for tracking user behavior
    /// </summary>
    public class UserInteraction : BaseEntity
    {
        /// <summary>
        /// User ID who performed the interaction
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Post ID that was interacted with (optional for general interactions)
        /// </summary>
        public Guid? PostId { get; set; }

        /// <summary>
        /// Type of interaction (view, like, comment, share, download, etc.)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string InteractionType { get; set; } = string.Empty;

        /// <summary>
        /// Duration of interaction (for views, reading time, etc.)
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// IP address of the user (for analytics and fraud detection)
        /// </summary>
        [StringLength(45)] // IPv6 length
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string (for device/browser analytics)
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Referrer URL (how user reached the content)
        /// </summary>
        [StringLength(500)]
        public string? Referrer { get; set; }

        /// <summary>
        /// Session ID to group interactions
        /// </summary>
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Additional metadata as JSON
        /// </summary>
        public string? Metadata { get; set; }

        // Navigation properties

        /// <summary>
        /// User who performed the interaction
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// Post that was interacted with
        /// </summary>
        public virtual Post? Post { get; set; }

        // Business methods

        /// <summary>
        /// Check if this is a meaningful engagement (not just a quick view)
        /// </summary>
        /// <returns>True if this represents meaningful engagement</returns>
        public bool IsMeaningfulEngagement()
        {
            return InteractionType.ToLower() switch
            {
                "view" => Duration?.TotalSeconds > 30, // More than 30 seconds
                "like" or "comment" or "share" => true,
                _ => false
            };
        }

        /// <summary>
        /// Get the weight of this interaction for recommendation algorithms
        /// </summary>
        /// <returns>Interaction weight (1-5)</returns>
        public int GetRecommendationWeight()
        {
            return InteractionType.ToLower() switch
            {
                "view" => IsMeaningfulEngagement() ? 2 : 1,
                "like" => 3,
                "comment" => 5,
                "share" => 4,
                "download" => 3,
                _ => 1
            };
        }

        /// <summary>
        /// Check if this interaction indicates content preference
        /// </summary>
        /// <returns>True if this indicates user likes this type of content</returns>
        public bool IndicatesPreference()
        {
            return InteractionType.ToLower() switch
            {
                "like" or "comment" or "share" or "download" => true,
                "view" => IsMeaningfulEngagement(),
                _ => false
            };
        }
    }
}