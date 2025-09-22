using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations
{
    public class TemporaryPermissionConfiguration : IEntityTypeConfiguration<TemporaryPermission>
    {
        public void Configure(EntityTypeBuilder<TemporaryPermission> builder)
        {
            builder.ToTable("TemporaryPermissions");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.ResourceType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Reason)
                .HasMaxLength(500);

            builder.Property(e => e.RevokeReason)
                .HasMaxLength(500);

            // Configure the relationship with User (the user who receives the permission)
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship with GrantedByUser (the user who granted the permission)
            builder.HasOne(e => e.GrantedByUser)
                .WithMany()
                .HasForeignKey(e => e.GrantedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship with DelegatedFromUser (the user who delegated the permission)
            builder.HasOne(e => e.DelegatedFromUser)
                .WithMany()
                .HasForeignKey(e => e.DelegatedFrom)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure the relationship with RevokedByUser (the user who revoked the permission)
            builder.HasOne(e => e.RevokedByUser)
                .WithMany()
                .HasForeignKey(e => e.RevokedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for better query performance
            builder.HasIndex(e => new { e.UserId, e.ResourceType, e.ResourceId })
                .HasDatabaseName("IX_TemporaryPermissions_User_Resource");

            builder.HasIndex(e => e.ResourceType)
                .HasDatabaseName("IX_TemporaryPermissions_ResourceType");

            builder.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_TemporaryPermissions_IsActive");

            builder.HasIndex(e => e.IsRevoked)
                .HasDatabaseName("IX_TemporaryPermissions_IsRevoked");

            builder.HasIndex(e => e.ExpiresAt)
                .HasDatabaseName("IX_TemporaryPermissions_ExpiresAt");

            builder.HasIndex(e => e.EffectiveFrom)
                .HasDatabaseName("IX_TemporaryPermissions_EffectiveFrom");
        }
    }
}