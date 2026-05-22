using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.EnterpriseRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "documents",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "documents",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UploadedBy",
                table: "documents",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 22, 7, 45, 50, 182, DateTimeKind.Utc).AddTicks(3271), "AQAAAAIAAYagAAAAEJ6HA9iiNt/MVjzefr/Im+/CgIWyEDLkKidlhgKEIZFk11T/V1XLGiHK7ZKVkB4DsA==" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_TenantId",
                table: "documents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_TenantId_Status",
                table: "documents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_UploadedBy",
                table: "documents",
                column: "UploadedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_documents_TenantId",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_TenantId_Status",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_UploadedBy",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "UploadedBy",
                table: "documents");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 22, 1, 17, 18, 212, DateTimeKind.Utc).AddTicks(6091), "AQAAAAIAAYagAAAAEBl9/QYaEUcf8zObNisH21TNjk3NZG1SffbDLFgUFCB3zSyxsNTJj0m3giqpsO1nkg==" });
        }
    }
}
