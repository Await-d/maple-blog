using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Infrastructure.Configuration;

/// <summary>
/// Entity Framework configuration for LoginHistory entity
/// </summary>
public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
{
    public void Configure(EntityTypeBuilder<LoginHistory> builder)
    {
        // Table name
        builder.ToTable("LoginHistories");

        // Primary key
        builder.HasKey(lh => lh.Id);
        builder.Property(lh => lh.Id)
            .ValueGeneratedOnAdd();

        // User relationship
        builder.HasOne(lh => lh.User)
            .WithMany()
            .HasForeignKey(lh => lh.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Required fields
        builder.Property(lh => lh.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Optional fields with length limits
        builder.Property(lh => lh.UserName)
            .HasMaxLength(256);

        builder.Property(lh => lh.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(lh => lh.UserAgent)
            .HasMaxLength(1000);

        builder.Property(lh => lh.DeviceInfo)
            .HasMaxLength(500);

        builder.Property(lh => lh.BrowserInfo)
            .HasMaxLength(500);

        builder.Property(lh => lh.OperatingSystem)
            .HasMaxLength(200);

        builder.Property(lh => lh.Location)
            .HasMaxLength(500);

        builder.Property(lh => lh.Country)
            .HasMaxLength(100);

        builder.Property(lh => lh.City)
            .HasMaxLength(200);

        builder.Property(lh => lh.SessionId)
            .HasMaxLength(256);

        builder.Property(lh => lh.FailureReason)
            .HasMaxLength(1000);

        builder.Property(lh => lh.TwoFactorMethod)
            .HasMaxLength(100);

        builder.Property(lh => lh.RiskFactors)
            .HasMaxLength(2000);

        // JSON metadata field - unlimited length
        builder.Property(lh => lh.MetadataJson);

        // Default values
        builder.Property(lh => lh.IsSuccessful)
            .HasDefaultValue(false);

        builder.Property(lh => lh.Result)
            .HasDefaultValue(LoginResult.Failed);

        builder.Property(lh => lh.LoginType)
            .HasDefaultValue(LoginType.Standard);

        builder.Property(lh => lh.TwoFactorUsed)
            .HasDefaultValue(false);

        builder.Property(lh => lh.RiskScore)
            .HasDefaultValue(0);

        builder.Property(lh => lh.IsFlagged)
            .HasDefaultValue(false);

        builder.Property(lh => lh.IsBlocked)
            .HasDefaultValue(false);

        // Indexes for performance
        builder.HasIndex(lh => lh.UserId)
            .HasDatabaseName("IX_LoginHistories_UserId");

        builder.HasIndex(lh => lh.Email)
            .HasDatabaseName("IX_LoginHistories_Email");

        builder.HasIndex(lh => lh.IpAddress)
            .HasDatabaseName("IX_LoginHistories_IpAddress");

        builder.HasIndex(lh => lh.CreatedAt)
            .HasDatabaseName("IX_LoginHistories_CreatedAt");

        builder.HasIndex(lh => lh.IsSuccessful)
            .HasDatabaseName("IX_LoginHistories_IsSuccessful");

        builder.HasIndex(lh => lh.Result)
            .HasDatabaseName("IX_LoginHistories_Result");

        builder.HasIndex(lh => lh.RiskScore)
            .HasDatabaseName("IX_LoginHistories_RiskScore");

        builder.HasIndex(lh => lh.IsFlagged)
            .HasDatabaseName("IX_LoginHistories_IsFlagged");

        builder.HasIndex(lh => lh.IsBlocked)
            .HasDatabaseName("IX_LoginHistories_IsBlocked");

        // Composite indexes for common queries
        builder.HasIndex(lh => new { lh.Email, lh.CreatedAt })
            .HasDatabaseName("IX_LoginHistories_Email_CreatedAt");

        builder.HasIndex(lh => new { lh.IpAddress, lh.CreatedAt })
            .HasDatabaseName("IX_LoginHistories_IpAddress_CreatedAt");

        builder.HasIndex(lh => new { lh.UserId, lh.CreatedAt })
            .HasDatabaseName("IX_LoginHistories_UserId_CreatedAt");

        builder.HasIndex(lh => new { lh.IsSuccessful, lh.CreatedAt })
            .HasDatabaseName("IX_LoginHistories_IsSuccessful_CreatedAt");
    }
}