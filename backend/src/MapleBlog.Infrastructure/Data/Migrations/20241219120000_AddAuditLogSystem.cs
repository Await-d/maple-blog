using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapleBlog.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UserEmail = table.Column<string>(type: "TEXT", maxLength: 320, nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ResourceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OldValues = table.Column<string>(type: "TEXT", nullable: true),
                    NewValues = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RequestPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    Duration = table.Column<long>(type: "INTEGER", nullable: true),
                    Result = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Success"),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    AdditionalData = table.Column<string>(type: "TEXT", nullable: true),
                    RiskLevel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Low"),
                    IsSensitive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // 创建性能索引
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ResourceType",
                table: "AuditLogs",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ResourceId",
                table: "AuditLogs",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IpAddress",
                table: "AuditLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Result",
                table: "AuditLogs",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RiskLevel",
                table: "AuditLogs",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsSensitive",
                table: "AuditLogs",
                column: "IsSensitive");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt",
                table: "AuditLogs",
                column: "CreatedAt");

            // 复合索引 - 提高查询性能
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action_ResourceType",
                table: "AuditLogs",
                columns: new[] { "Action", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAt_RiskLevel",
                table: "AuditLogs",
                columns: new[] { "CreatedAt", "RiskLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsSensitive_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "IsSensitive", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");
        }
    }
}