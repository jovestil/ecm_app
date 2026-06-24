using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateTerminationReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TerminationReasons_CompanyCode_ReasonCode",
                table: "TerminationReasons");

            migrationBuilder.CreateIndex(
                name: "IX_TerminationReasons_CompanyCode_ReasonCode",
                table: "TerminationReasons",
                columns: new[] { "CompanyCode", "ReasonCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TerminationReasons_CompanyCode_ReasonCode",
                table: "TerminationReasons");

            migrationBuilder.CreateIndex(
                name: "IX_TerminationReasons_CompanyCode_ReasonCode",
                table: "TerminationReasons",
                columns: new[] { "CompanyCode", "ReasonCode" },
                unique: true);
        }
    }
}
