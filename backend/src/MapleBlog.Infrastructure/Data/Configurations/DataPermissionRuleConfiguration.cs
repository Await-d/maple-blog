using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Data.Configurations
{
    public class DataPermissionRuleConfiguration : IEntityTypeConfiguration<DataPermissionRule>
    {
        public void Configure(EntityTypeBuilder<DataPermissionRule> builder)
        {
            builder.ToTable("DataPermissionRules");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.ResourceType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Conditions)
                .HasMaxLength(2000);

            builder.Property(e => e.Remarks)
                .HasMaxLength(500);

            // Configure the relationship with User (the user who has the permission)
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship with GrantedByUser (the user who granted the permission)
            builder.HasOne(e => e.GrantedByUser)
                .WithMany()
                .HasForeignKey(e => e.GrantedBy)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure the relationship with Role
            builder.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for better query performance
            builder.HasIndex(e => new { e.UserId, e.ResourceType, e.Operation })
                .HasDatabaseName("IX_DataPermissionRules_User_Resource_Operation");

            builder.HasIndex(e => e.ResourceType)
                .HasDatabaseName("IX_DataPermissionRules_ResourceType");

            builder.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_DataPermissionRules_IsActive");

            builder.HasIndex(e => e.EffectiveFrom)
                .HasDatabaseName("IX_DataPermissionRules_EffectiveFrom");

            builder.HasIndex(e => e.EffectiveTo)
                .HasDatabaseName("IX_DataPermissionRules_EffectiveTo");
        }
    }
}