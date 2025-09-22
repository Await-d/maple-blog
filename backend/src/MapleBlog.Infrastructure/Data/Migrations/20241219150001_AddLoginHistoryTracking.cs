using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapleBlog.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginHistoryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create LoginHistories table
            migrationBuilder.CreateTable(
                name: "LoginHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Result = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Failed"),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DeviceInfo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BrowserInfo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OperatingSystem = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SessionExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LogoutAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SessionDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    LoginType = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Standard"),
                    TwoFactorUsed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    TwoFactorMethod = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    RiskScore = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    RiskFactors = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsFlagged = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsBlocked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes for performance optimization
            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_UserId",
                table: "LoginHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_Email",
                table: "LoginHistories",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_IpAddress",
                table: "LoginHistories",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_CreatedAt",
                table: "LoginHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_IsSuccessful",
                table: "LoginHistories",
                column: "IsSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_Result",
                table: "LoginHistories",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_RiskScore",
                table: "LoginHistories",
                column: "RiskScore");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_IsFlagged",
                table: "LoginHistories",
                column: "IsFlagged");

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_IsBlocked",
                table: "LoginHistories",
                column: "IsBlocked");

            // Composite indexes for common query patterns
            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_Email_CreatedAt",
                table: "LoginHistories",
                columns: new[] { "Email", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_IpAddress_CreatedAt",
                table: "LoginHistories",
                columns: new[] { "IpAddress", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_UserId_CreatedAt",
                table: "LoginHistories",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginHistories_IsSuccessful_CreatedAt",
                table: "LoginHistories",
                columns: new[] { "IsSuccessful", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginHistories");
        }
    }
}