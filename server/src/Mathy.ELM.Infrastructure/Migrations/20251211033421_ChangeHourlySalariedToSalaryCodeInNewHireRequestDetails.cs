using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeHourlySalariedToSalaryCodeInNewHireRequestDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HourlySalaried",
                table: "NewHireRequestDetails");

            migrationBuilder.AddColumn<int>(
                name: "SalaryCode",
                table: "NewHireRequestDetails",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalaryCode",
                table: "NewHireRequestDetails");

            migrationBuilder.AddColumn<string>(
                name: "HourlySalaried",
                table: "NewHireRequestDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
