using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceFuelOnlyAndEEExpenseWithCreditExpenseTypeInPTCreditCardDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EEExpenseCard",
                table: "PTCreditCardDetails");

            migrationBuilder.DropColumn(
                name: "FuelOnlyCard",
                table: "PTCreditCardDetails");

            migrationBuilder.AddColumn<string>(
                name: "CreditExpenseType",
                table: "PTCreditCardDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditExpenseType",
                table: "PTCreditCardDetails");

            migrationBuilder.AddColumn<bool>(
                name: "EEExpenseCard",
                table: "PTCreditCardDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FuelOnlyCard",
                table: "PTCreditCardDetails",
                type: "bit",
                nullable: true);
        }
    }
}
