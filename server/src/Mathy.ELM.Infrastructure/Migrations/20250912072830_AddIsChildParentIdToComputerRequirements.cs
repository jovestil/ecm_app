using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsChildParentIdToComputerRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsChild",
                table: "ComputerRequirements",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "ComputerRequirements",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsChild",
                table: "ComputerRequirements");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ComputerRequirements");
        }
    }
}
