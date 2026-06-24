using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddADCredentialsToNewHireRequestDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdPassword",
                table: "NewHireRequestDetails",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetworkId",
                table: "NewHireRequestDetails",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkEmail",
                table: "NewHireRequestDetails",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdPassword",
                table: "NewHireRequestDetails");

            migrationBuilder.DropColumn(
                name: "NetworkId",
                table: "NewHireRequestDetails");

            migrationBuilder.DropColumn(
                name: "WorkEmail",
                table: "NewHireRequestDetails");
        }
    }
}
