using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmploymentStatusMapperTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmploymentStatusMapper",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActiveStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LayOffStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReturnToWorkStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TerminationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsUnion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentStatusMapper", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatusMapper_ActiveStatus",
                table: "EmploymentStatusMapper",
                column: "ActiveStatus");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatusMapper_IsUnion",
                table: "EmploymentStatusMapper",
                column: "IsUnion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmploymentStatusMapper");
        }
    }
}
