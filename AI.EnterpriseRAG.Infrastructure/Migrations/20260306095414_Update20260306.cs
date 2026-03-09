using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.EnterpriseRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update20260306 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Similarity",
                table: "document_chunks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "ContextTokenCount",
                table: "chat_conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokenCount",
                table: "chat_conversations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "SearchSimilarity",
                table: "chat_conversations",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Similarity",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "ContextTokenCount",
                table: "chat_conversations");

            migrationBuilder.DropColumn(
                name: "PromptTokenCount",
                table: "chat_conversations");

            migrationBuilder.DropColumn(
                name: "SearchSimilarity",
                table: "chat_conversations");
        }
    }
}
