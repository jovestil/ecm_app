using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmitterEmailToHRRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubmitterEmail",
                table: "HRRequests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                comment: "Email of the user who submitted the request (captured at submission time)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmitterEmail",
                table: "HRRequests");
        }
    }
}
