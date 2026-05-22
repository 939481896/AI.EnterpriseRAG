using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.EnterpriseRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreateTimeToDocumentChunk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "KeyWords",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "Similarity",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "VectorJson",
                table: "document_chunks");

            migrationBuilder.AlterColumn<string>(
                name: "SectionTitle",
                table: "document_chunks",
                type: "varchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ChunkId",
                table: "document_chunks",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateTime",
                table: "document_chunks",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "agent_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserIntent = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExecutionPlan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FinalAnswer = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TotalCostSeconds = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_sessions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "agent_steps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SessionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StepType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StepIndex = table.Column<int>(type: "int", nullable: false),
                    Thought = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToolName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToolArguments = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToolResult = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMs = table.Column<long>(type: "BIGINT", nullable: false),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agent_steps_agent_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "agent_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 5, 22, 1, 17, 18, 212, DateTimeKind.Utc).AddTicks(6091), "AQAAAAIAAYagAAAAEBl9/QYaEUcf8zObNisH21TNjk3NZG1SffbDLFgUFCB3zSyxsNTJj0m3giqpsO1nkg==" });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_ChunkId",
                table: "document_chunks",
                column: "ChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_sessions_StartTime",
                table: "agent_sessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_agent_sessions_TenantId",
                table: "agent_sessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_sessions_UserId",
                table: "agent_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_steps_SessionId",
                table: "agent_steps",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_agent_steps_StepIndex",
                table: "agent_steps",
                column: "StepIndex");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_steps");

            migrationBuilder.DropTable(
                name: "agent_sessions");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_ChunkId",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "CreateTime",
                table: "document_chunks");

            migrationBuilder.AlterColumn<string>(
                name: "SectionTitle",
                table: "document_chunks",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "ChunkId",
                table: "document_chunks",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Embedding",
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

            migrationBuilder.AddColumn<string>(
                name: "KeyWords",
                table: "document_chunks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<float>(
                name: "Similarity",
                table: "document_chunks",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "VectorJson",
                table: "document_chunks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 4, 1, 2, 58, 46, 81, DateTimeKind.Utc).AddTicks(3408), "AQAAAAIAAYagAAAAEKc6TJy3vWFkmNCh5MVc1AP8st9TEU0rOYkR7GFiZz5m1lWh3AoT1+Ex6bfKTuIgIg==" });
        }
    }
}
