using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncScheduleConfigTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncScheduleConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastExecuted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastExecutionResult = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncScheduleConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncScheduleConfigs_IsActive",
                table: "SyncScheduleConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SyncScheduleConfigs_IsDeleted",
                table: "SyncScheduleConfigs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SyncScheduleConfigs_SyncType",
                table: "SyncScheduleConfigs",
                column: "SyncType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncScheduleConfigs");
        }
    }
}
