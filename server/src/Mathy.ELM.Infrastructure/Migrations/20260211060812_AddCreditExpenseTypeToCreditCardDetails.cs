using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditExpenseTypeToCreditCardDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditExpenseType",
                table: "CreditCardDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditExpenseType",
                table: "CreditCardDetails");
        }
    }
}
