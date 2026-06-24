using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesForLatestDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailDomain",
                table: "CompanyTypeLocation");

            migrationBuilder.AddColumn<string>(
                name: "EmailDomain",
                table: "PayrollDepartments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeptCode",
                table: "CompanyDL",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ServiceDeskSyncData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewHireRequestId = table.Column<int>(type: "int", nullable: false),
                    ServiceDeskID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HasBuildingAccess = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasPhoneRequirements = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasComputerRequirements = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasTabletProfiles = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasITApplications = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    HasSoftwareAccessReq = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDeskSyncData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDeskSyncData_NewHireRequestDetails_NewHireRequestId",
                        column: x => x.NewHireRequestId,
                        principalTable: "NewHireRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskSyncData_IsDeleted",
                table: "ServiceDeskSyncData",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskSyncData_NewHireRequestId",
                table: "ServiceDeskSyncData",
                column: "NewHireRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeskSyncData_ServiceDeskID",
                table: "ServiceDeskSyncData",
                column: "ServiceDeskID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceDeskSyncData");

            migrationBuilder.DropColumn(
                name: "EmailDomain",
                table: "PayrollDepartments");

            migrationBuilder.DropColumn(
                name: "DeptCode",
                table: "CompanyDL");

            migrationBuilder.AddColumn<string>(
                name: "EmailDomain",
                table: "CompanyTypeLocation",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
