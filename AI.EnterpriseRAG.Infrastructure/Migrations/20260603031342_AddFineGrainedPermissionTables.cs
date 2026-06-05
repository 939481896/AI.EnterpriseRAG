using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.EnterpriseRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFineGrainedPermissionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DocumentCategoryId",
                table: "documents",
                type: "BIGINT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentCategory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "BIGINT", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CategoryName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentId = table.Column<long>(type: "BIGINT", nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentCategory_DocumentCategory_ParentId",
                        column: x => x.ParentId,
                        principalTable: "DocumentCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentTag",
                columns: table => new
                {
                    Id = table.Column<long>(type: "BIGINT", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TagName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TagColor = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTag", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RoleDocumentPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoleId = table.Column<long>(type: "BIGINT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PermissionType = table.Column<int>(type: "int", nullable: false),
                    GrantedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GrantedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleDocumentPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleDocumentPermission_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleDocumentPermission_sys_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "sys_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserDocumentPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<long>(type: "BIGINT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PermissionType = table.Column<int>(type: "int", nullable: false),
                    GrantedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GrantedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDocumentPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDocumentPermission_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDocumentPermission_sys_users_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DocumentTagRelation",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TagId = table.Column<long>(type: "BIGINT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTagRelation", x => new { x.DocumentId, x.TagId });
                    table.ForeignKey(
                        name: "FK_DocumentTagRelation_DocumentTag_TagId",
                        column: x => x.TagId,
                        principalTable: "DocumentTag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentTagRelation_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 6, 3, 3, 13, 41, 906, DateTimeKind.Utc).AddTicks(9636), "AQAAAAIAAYagAAAAEJgTlqeS90krY5dhCDjoVkE6wqfg/CsIzKbVb5Etm8LZ8ADGty1erJpIk+7W+xG/2w==" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_DocumentCategoryId",
                table: "documents",
                column: "DocumentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategory_CategoryCode",
                table: "DocumentCategory",
                column: "CategoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentCategory_ParentId",
                table: "DocumentCategory",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTag_TagName",
                table: "DocumentTag",
                column: "TagName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTagRelation_TagId",
                table: "DocumentTagRelation",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDocumentPermission_DocumentId",
                table: "RoleDocumentPermission",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDocumentPermission_RoleId",
                table: "RoleDocumentPermission",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocumentPermission_DocumentId",
                table: "UserDocumentPermission",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocumentPermission_UserId",
                table: "UserDocumentPermission",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocumentPermission_UserId_DocumentId",
                table: "UserDocumentPermission",
                columns: new[] { "UserId", "DocumentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_DocumentCategory_DocumentCategoryId",
                table: "documents",
                column: "DocumentCategoryId",
                principalTable: "DocumentCategory",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_DocumentCategory_DocumentCategoryId",
                table: "documents");

            migrationBuilder.DropTable(
                name: "DocumentCategory");

            migrationBuilder.DropTable(
                name: "DocumentTagRelation");

            migrationBuilder.DropTable(
                name: "RoleDocumentPermission");

            migrationBuilder.DropTable(
                name: "UserDocumentPermission");

            migrationBuilder.DropTable(
                name: "DocumentTag");

            migrationBuilder.DropIndex(
                name: "IX_documents_DocumentCategoryId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "DocumentCategoryId",
                table: "documents");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 6, 3, 2, 56, 52, 607, DateTimeKind.Utc).AddTicks(8414), "AQAAAAIAAYagAAAAEKqTrsxJBOQsk6X9M0pOh4oIm31ZHFxfVLDlsDA4aV2dYCo4Pl4f8v3D+An7gG5MoQ==" });
        }
    }
}
