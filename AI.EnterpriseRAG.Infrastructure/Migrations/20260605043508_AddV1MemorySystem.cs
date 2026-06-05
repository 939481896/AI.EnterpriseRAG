using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AI.EnterpriseRAG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddV1MemorySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversation_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastInteractionAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_sessions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "conversation_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SessionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenceChunks = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CostSeconds = table.Column<decimal>(type: "DECIMAL(18,6)", nullable: false),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SearchSimilarity = table.Column<float>(type: "float", nullable: true),
                    ContextTokenCount = table.Column<int>(type: "int", nullable: false),
                    PromptTokenCount = table.Column<int>(type: "int", nullable: false),
                    UsedHyDE = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UsedMultiQuery = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UsedSelfReflection = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SelfReflectionConfidence = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversation_messages_conversation_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "conversation_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 6, 5, 4, 35, 7, 908, DateTimeKind.Utc).AddTicks(4730), "AQAAAAIAAYagAAAAEMdkI+zHenyIW8EFJlU9bVdZP0/wqrf2qviq/bJ9r55DkruP6eh+3acI5FOIVOQhLA==" });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_CreatedAt",
                table: "conversation_messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_SessionId",
                table: "conversation_messages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_SessionId_SequenceNumber",
                table: "conversation_messages",
                columns: new[] { "SessionId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_sessions_LastInteractionAt",
                table: "conversation_sessions",
                column: "LastInteractionAt");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_sessions_UserId",
                table: "conversation_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_conversation_sessions_UserId_IsActive",
                table: "conversation_sessions",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_messages");

            migrationBuilder.DropTable(
                name: "conversation_sessions");

            migrationBuilder.UpdateData(
                table: "sys_users",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "CreateTime", "PasswordHash" },
                values: new object[] { new DateTime(2026, 6, 5, 2, 35, 35, 881, DateTimeKind.Utc).AddTicks(7441), "AQAAAAIAAYagAAAAEI5aVO4HZsbpuQuPs5dmXkHQ1dBFSBikXz7ThqmVlLYzYdhIonzTU7NrK+br4aae6A==" });
        }
    }
}
