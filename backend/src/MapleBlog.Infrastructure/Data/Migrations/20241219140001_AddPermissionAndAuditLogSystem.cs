using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MapleBlog.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionAndAuditLogSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 添加权限实体的Scope字段
            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Permissions",
                type: "TEXT",
                nullable: false,
                defaultValue: "Own");

            // 更新权限表的索引 - 先删除旧的唯一索引
            migrationBuilder.DropIndex(
                name: "IX_Permissions_Resource_Action",
                table: "Permissions");

            // 添加新的包含Scope的唯一索引
            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Resource_Action_Scope",
                table: "Permissions",
                columns: new[] { "Resource", "Action", "Scope" },
                unique: true,
                filter: "IsDeleted = 0");

            // 添加Scope字段的性能索引
            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Scope",
                table: "Permissions",
                column: "Scope");

            // 更新RolePermissions表结构
            migrationBuilder.AddColumn<Guid>(
                name: "GrantedBy",
                table: "RolePermissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GrantedAt",
                table: "RolePermissions",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "RolePermissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTemporary",
                table: "RolePermissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RolePermissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            // 添加RolePermissions表的新索引
            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IsActive",
                table: "RolePermissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IsTemporary",
                table: "RolePermissions",
                column: "IsTemporary");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_ExpiresAt",
                table: "RolePermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_GrantedBy",
                table: "RolePermissions",
                column: "GrantedBy");

            // 更新AuditLogs表结构 - 添加新字段
            migrationBuilder.AddColumn<string>(
                name: "RiskLevel",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Low");

            migrationBuilder.AddColumn<bool>(
                name: "IsSensitive",
                table: "AuditLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "AuditLogs",
                type: "TEXT",
                nullable: true);

            // 添加AuditLogs表的新索引
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
                name: "IX_AuditLogs_CreatedAt_RiskLevel",
                table: "AuditLogs",
                columns: new[] { "CreatedAt", "RiskLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsSensitive_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "IsSensitive", "CreatedAt" });

            // 添加外键约束
            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_GrantedBy",
                table: "RolePermissions",
                column: "GrantedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Users_GrantedBy",
                table: "RolePermissions",
                column: "GrantedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 移除外键约束
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Users_GrantedBy",
                table: "RolePermissions");

            // 移除AuditLogs表的新索引
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_RiskLevel",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Category",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_IsSensitive",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CreatedAt_RiskLevel",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_IsSensitive_CreatedAt",
                table: "AuditLogs");

            // 移除RolePermissions表的新索引
            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_IsActive",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_IsTemporary",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_ExpiresAt",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_GrantedBy",
                table: "RolePermissions");

            // 移除权限表的新索引
            migrationBuilder.DropIndex(
                name: "IX_Permissions_Resource_Action_Scope",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Scope",
                table: "Permissions");

            // 移除AuditLogs表的新字段
            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IsSensitive",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "AuditLogs");

            // 移除RolePermissions表的新字段
            migrationBuilder.DropColumn(
                name: "GrantedBy",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "GrantedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "IsTemporary",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RolePermissions");

            // 移除权限表的Scope字段
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Permissions");

            // 恢复原来的索引
            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Resource_Action",
                table: "Permissions",
                columns: new[] { "Resource", "Action" },
                unique: true,
                filter: "IsDeleted = 0");
        }
    }
}