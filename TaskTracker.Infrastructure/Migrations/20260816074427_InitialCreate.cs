using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UrgencyLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrgencyLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Stage = table.Column<int>(type: "INTEGER", nullable: false),
                    UrgencyLevelId = table.Column<int>(type: "INTEGER", nullable: false),
                    Deadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskItems_UrgencyLevels_UrgencyLevelId",
                        column: x => x.UrgencyLevelId,
                        principalTable: "UrgencyLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "UrgencyLevels",
                columns: new[] { "Id", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, true, "Low", 1 },
                    { 2, true, "Medium", 2 },
                    { 3, true, "High", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_Deadline",
                table: "TaskItems",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_IsDeleted_DeletedAtUtc",
                table: "TaskItems",
                columns: new[] { "IsDeleted", "DeletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_Stage",
                table: "TaskItems",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_UrgencyLevelId",
                table: "TaskItems",
                column: "UrgencyLevelId");

            migrationBuilder.CreateIndex(
                name: "UX_TaskItems_Title_Active",
                table: "TaskItems",
                column: "Title",
                unique: true,
                filter: "\"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskItems");

            migrationBuilder.DropTable(
                name: "UrgencyLevels");
        }
    }
}
