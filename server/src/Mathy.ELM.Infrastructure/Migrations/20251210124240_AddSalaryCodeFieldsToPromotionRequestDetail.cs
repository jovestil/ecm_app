using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryCodeFieldsToPromotionRequestDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSalaryCode",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewSalaryCode",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSalaryCode",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "NewSalaryCode",
                table: "PromotionRequestDetails");
        }
    }
}
