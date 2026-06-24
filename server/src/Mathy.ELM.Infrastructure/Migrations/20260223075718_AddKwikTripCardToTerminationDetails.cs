using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKwikTripCardToTerminationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KwikCard4DigitNo",
                table: "TerminationRequestDetails",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WithKwikTripCard",
                table: "TerminationRequestDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KwikCard4DigitNo",
                table: "TerminationRequestDetails");

            migrationBuilder.DropColumn(
                name: "WithKwikTripCard",
                table: "TerminationRequestDetails");
        }
    }
}
