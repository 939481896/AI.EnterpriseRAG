using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.EnterpriseRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileHashForDuplicateDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "documents",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdateTime",
                table: "documents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 6, 3, 5, 28, 17, 371, DateTimeKind.Utc).AddTicks(7789), "AQAAAAIAAYagAAAAEO0KpWVUHO6jKTAr9+ouJTBmxj7bYUFS4T5dzqAqbH9edV4/UuLM2L5tGHLUuY0nDg==" });

            migrationBuilder.CreateIndex(
                name: "IX_documents_FileHash",
                table: "documents",
                column: "FileHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_documents_FileHash",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "UpdateTime",
                table: "documents");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 6, 3, 3, 13, 41, 906, DateTimeKind.Utc).AddTicks(9636), "AQAAAAIAAYagAAAAEJgTlqeS90krY5dhCDjoVkE6wqfg/CsIzKbVb5Etm8LZ8ADGty1erJpIk+7W+xG/2w==" });
        }
    }
}
