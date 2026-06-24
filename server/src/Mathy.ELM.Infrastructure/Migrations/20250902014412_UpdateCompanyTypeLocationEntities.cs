using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCompanyTypeLocationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyTypeLocation",
                table: "TabletProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyTypeLocation",
                table: "BuildingAccessRequirements");

            migrationBuilder.DropColumn(
                name: "CompanyTypeLocation",
                table: "Applications");

            migrationBuilder.AddColumn<int>(
                name: "CompanyTypeLocationId",
                table: "TabletProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyTypeLocationId",
                table: "BuildingAccessRequirements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompanyTypeLocationId",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CompanyTypeLocation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    LocationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsUnion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyTypeLocation", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TabletProfiles_CompanyTypeLocationId",
                table: "TabletProfiles",
                column: "CompanyTypeLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingAccessRequirements_CompanyTypeLocationId",
                table: "BuildingAccessRequirements",
                column: "CompanyTypeLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CompanyTypeLocationId",
                table: "Applications",
                column: "CompanyTypeLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTypeLocation_CompanyCode",
                table: "CompanyTypeLocation",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTypeLocation_IsDeleted",
                table: "CompanyTypeLocation",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyTypeLocation_LocationType",
                table: "CompanyTypeLocation",
                column: "LocationType");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_CompanyTypeLocation_CompanyTypeLocationId",
                table: "Applications",
                column: "CompanyTypeLocationId",
                principalTable: "CompanyTypeLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BuildingAccessRequirements_CompanyTypeLocation_CompanyTypeLocationId",
                table: "BuildingAccessRequirements",
                column: "CompanyTypeLocationId",
                principalTable: "CompanyTypeLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TabletProfiles_CompanyTypeLocation_CompanyTypeLocationId",
                table: "TabletProfiles",
                column: "CompanyTypeLocationId",
                principalTable: "CompanyTypeLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_CompanyTypeLocation_CompanyTypeLocationId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_BuildingAccessRequirements_CompanyTypeLocation_CompanyTypeLocationId",
                table: "BuildingAccessRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_TabletProfiles_CompanyTypeLocation_CompanyTypeLocationId",
                table: "TabletProfiles");

            migrationBuilder.DropTable(
                name: "CompanyTypeLocation");

            migrationBuilder.DropIndex(
                name: "IX_TabletProfiles_CompanyTypeLocationId",
                table: "TabletProfiles");

            migrationBuilder.DropIndex(
                name: "IX_BuildingAccessRequirements_CompanyTypeLocationId",
                table: "BuildingAccessRequirements");

            migrationBuilder.DropIndex(
                name: "IX_Applications_CompanyTypeLocationId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "CompanyTypeLocationId",
                table: "TabletProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyTypeLocationId",
                table: "BuildingAccessRequirements");

            migrationBuilder.DropColumn(
                name: "CompanyTypeLocationId",
                table: "Applications");

            migrationBuilder.AddColumn<string>(
                name: "CompanyTypeLocation",
                table: "TabletProfiles",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyTypeLocation",
                table: "BuildingAccessRequirements",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyTypeLocation",
                table: "Applications",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");
        }
    }
}
