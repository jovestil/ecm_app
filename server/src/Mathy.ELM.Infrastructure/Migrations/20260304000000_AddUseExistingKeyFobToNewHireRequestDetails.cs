using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUseExistingKeyFobToNewHireRequestDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseExistingKeyFob",
                table: "NewHireRequestDetails",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseExistingKeyFob",
                table: "NewHireRequestDetails");
        }
    }
}
