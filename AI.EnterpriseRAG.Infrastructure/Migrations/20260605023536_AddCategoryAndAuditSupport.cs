using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.EnterpriseRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAndAuditSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_DocumentCategory_DocumentCategoryId",
                table: "documents");

            migrationBuilder.RenameColumn(
                name: "DocumentCategoryId",
                table: "documents",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_documents_DocumentCategoryId",
                table: "documents",
                newName: "IX_documents_CategoryId");

            migrationBuilder.CreateTable(
                name: "PermissionAuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "BIGINT", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetUserId = table.Column<long>(type: "BIGINT", nullable: true),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PermissionType = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IP = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionAuditLog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserCategoryPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<long>(type: "BIGINT", nullable: false),
                    CategoryId = table.Column<long>(type: "BIGINT", nullable: false),
                    PermissionType = table.Column<int>(type: "int", nullable: false),
                    GrantedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GrantedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCategoryPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCategoryPermission_DocumentCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "DocumentCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCategoryPermission_sys_users_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 6, 5, 2, 35, 35, 881, DateTimeKind.Utc).AddTicks(7441), "AQAAAAIAAYagAAAAEI5aVO4HZsbpuQuPs5dmXkHQ1dBFSBikXz7ThqmVlLYzYdhIonzTU7NrK+br4aae6A==" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLog_CreateTime",
                table: "PermissionAuditLog",
                column: "CreateTime");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLog_DocumentId",
                table: "PermissionAuditLog",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLog_UserId",
                table: "PermissionAuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAuditLog_UserId_CreateTime",
                table: "PermissionAuditLog",
                columns: new[] { "UserId", "CreateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryPermission_CategoryId",
                table: "UserCategoryPermission",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryPermission_UserId",
                table: "UserCategoryPermission",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCategoryPermission_UserId_CategoryId",
                table: "UserCategoryPermission",
                columns: new[] { "UserId", "CategoryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_DocumentCategory_CategoryId",
                table: "documents",
                column: "CategoryId",
                principalTable: "DocumentCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_DocumentCategory_CategoryId",
                table: "documents");

            migrationBuilder.DropTable(
                name: "PermissionAuditLog");

            migrationBuilder.DropTable(
                name: "UserCategoryPermission");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "documents",
                newName: "DocumentCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_documents_CategoryId",
                table: "documents",
                newName: "IX_documents_DocumentCategoryId");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 6, 3, 5, 28, 17, 371, DateTimeKind.Utc).AddTicks(7789), "AQAAAAIAAYagAAAAEO0KpWVUHO6jKTAr9+ouJTBmxj7bYUFS4T5dzqAqbH9edV4/UuLM2L5tGHLUuY0nDg==" });

            migrationBuilder.AddForeignKey(
                name: "FK_documents_DocumentCategory_DocumentCategoryId",
                table: "documents",
                column: "DocumentCategoryId",
                principalTable: "DocumentCategory",
                principalColumn: "Id");
        }
    }
}
