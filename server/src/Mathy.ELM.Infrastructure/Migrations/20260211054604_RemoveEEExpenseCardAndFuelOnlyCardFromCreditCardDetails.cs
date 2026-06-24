using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEEExpenseCardAndFuelOnlyCardFromCreditCardDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EEExpenseCard",
                table: "CreditCardDetails");

            migrationBuilder.DropColumn(
                name: "FuelOnlyCard",
                table: "CreditCardDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EEExpenseCard",
                table: "CreditCardDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FuelOnlyCard",
                table: "CreditCardDetails",
                type: "bit",
                nullable: true);
        }
    }
}
