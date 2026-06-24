using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class typo_column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeDeparmentCode",
                table: "HRRequestDetails");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeDepartmentCode",
                table: "HRRequestDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeDepartmentCode",
                table: "HRRequestDetails");

            migrationBuilder.AddColumn<int>(
                name: "EmployeeDeparmentCode",
                table: "HRRequestDetails",
                type: "int",
                nullable: true);
        }
    }
}
