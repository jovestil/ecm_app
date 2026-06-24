using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPTServiceDeskSyncData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PTServiceDeskSyncData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PTServiceDeskSyncData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTServiceDeskSyncData_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PTServiceDeskSyncData_IsDeleted",
                table: "PTServiceDeskSyncData",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTServiceDeskSyncData_PTRequestDetailId",
                table: "PTServiceDeskSyncData",
                column: "PTRequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PTServiceDeskSyncData_ServiceDeskID",
                table: "PTServiceDeskSyncData",
                column: "ServiceDeskID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PTServiceDeskSyncData");
        }
    }
}
