using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignEntitiesWithDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "LocationType",
                table: "TabletProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "EmploymentStatuses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LocationType",
                table: "BuildingAccessRequirements",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LocationType",
                table: "Applications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TabletProfiles_LocationType",
                table: "TabletProfiles",
                column: "LocationType");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatuses_Notes",
                table: "EmploymentStatuses",
                column: "Notes");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingAccessRequirements_LocationType",
                table: "BuildingAccessRequirements",
                column: "LocationType");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_LocationType",
                table: "Applications",
                column: "LocationType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TabletProfiles_LocationType",
                table: "TabletProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmploymentStatuses_Notes",
                table: "EmploymentStatuses");

            migrationBuilder.DropIndex(
                name: "IX_BuildingAccessRequirements_LocationType",
                table: "BuildingAccessRequirements");

            migrationBuilder.DropIndex(
                name: "IX_Applications_LocationType",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "LocationType",
                table: "TabletProfiles");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "EmploymentStatuses");

            migrationBuilder.DropColumn(
                name: "LocationType",
                table: "BuildingAccessRequirements");

            migrationBuilder.DropColumn(
                name: "LocationType",
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
    }
}
