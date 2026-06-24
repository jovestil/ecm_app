using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyCodeToTerminationReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyCode",
                table: "TerminationReasons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 1,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 2,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 3,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 4,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 5,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 6,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 7,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 8,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 9,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 10,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 11,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 12,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 13,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 14,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 15,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 16,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 17,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 18,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 19,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 20,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 21,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 22,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 23,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 24,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 25,
                column: "CompanyCode",
                value: 0);

            migrationBuilder.UpdateData(
                table: "TerminationReasons",
                keyColumn: "Id",
                keyValue: 26,
                column: "CompanyCode",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "TerminationReasons");
        }
    }
}
