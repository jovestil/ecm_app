using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepFieldsToPayrollDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HRPartner",
                table: "PayrollDepartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HRRep",
                table: "PayrollDepartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayrollRep",
                table: "PayrollDepartments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SafetyRep",
                table: "PayrollDepartments",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HRPartner",
                table: "PayrollDepartments");

            migrationBuilder.DropColumn(
                name: "HRRep",
                table: "PayrollDepartments");

            migrationBuilder.DropColumn(
                name: "PayrollRep",
                table: "PayrollDepartments");

            migrationBuilder.DropColumn(
                name: "SafetyRep",
                table: "PayrollDepartments");
        }
    }
}
