using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.EnterpriseRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAgeField1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChunkId",
                table: "document_chunks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "document_chunks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "document_chunks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SectionLevel",
                table: "document_chunks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SectionTitle",
                table: "document_chunks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkId",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "SectionLevel",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "SectionTitle",
                table: "document_chunks");
        }
    }
}
