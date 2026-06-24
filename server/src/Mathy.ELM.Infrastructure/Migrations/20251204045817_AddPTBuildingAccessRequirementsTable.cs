using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPTBuildingAccessRequirementsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PTBuildingAccessRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    AccessId = table.Column<int>(type: "int", nullable: false),
                    AccessDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTBuildingAccessRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTBuildingAccessRequirements_BuildingAccessRequirements_AccessId",
                        column: x => x.AccessId,
                        principalTable: "BuildingAccessRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PTBuildingAccessRequirements_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PTBuildingAccessRequirements_AccessId",
                table: "PTBuildingAccessRequirements",
                column: "AccessId");

            migrationBuilder.CreateIndex(
                name: "IX_PTBuildingAccessRequirements_IsDeleted",
                table: "PTBuildingAccessRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTBuildingAccessRequirements_PTRequestDetailId",
                table: "PTBuildingAccessRequirements",
                column: "PTRequestDetailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PTBuildingAccessRequirements");
        }
    }
}
