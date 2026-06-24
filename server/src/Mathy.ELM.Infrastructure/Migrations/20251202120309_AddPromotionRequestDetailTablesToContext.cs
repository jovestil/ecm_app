using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionRequestDetailTablesToContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PTApplicationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    AccessNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTApplicationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTApplicationRequests_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PTApplicationRequests_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PTCreditCardDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    KwikTripCard = table.Column<bool>(type: "bit", nullable: true),
                    CompanyExpenseCard = table.Column<bool>(type: "bit", nullable: true),
                    FuelOnlyCard = table.Column<bool>(type: "bit", nullable: true),
                    EEExpenseCard = table.Column<bool>(type: "bit", nullable: true),
                    WeeklyLimit = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    FuelCardlockAccess = table.Column<bool>(type: "bit", nullable: true),
                    FuelCardlockAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTCreditCardDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTCreditCardDetails_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PTFolderRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    FolderType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FolderName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTFolderRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTFolderRequests_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PTITComputerRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    ComputerRequirementsId = table.Column<int>(type: "int", nullable: false),
                    ComputerRequirementsDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsChild = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTITComputerRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTITComputerRequirements_ComputerRequirements_ComputerRequirementsId",
                        column: x => x.ComputerRequirementsId,
                        principalTable: "ComputerRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PTITComputerRequirements_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PTITDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    EmailRequired = table.Column<bool>(type: "bit", nullable: true),
                    AlternateDeliveryLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MSOfficeLicenseE5 = table.Column<bool>(type: "bit", nullable: true),
                    MSOfficeLicenseF3 = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTITDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTITDetails_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PTITPhoneRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    DeskPhone = table.Column<bool>(type: "bit", nullable: true),
                    CompanyCellphone = table.Column<bool>(type: "bit", nullable: true),
                    BYODCellphone = table.Column<bool>(type: "bit", nullable: true),
                    WorkPhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkExtension = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkCell = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReusingExistingPhone = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTITPhoneRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTITPhoneRequirements_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PTITTabletProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    TabletProfileId = table.Column<int>(type: "int", nullable: false),
                    TabletProfileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RolesRequiredForNewHire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTITTabletProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTITTabletProfiles_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PTITTabletProfiles_TabletProfiles_TabletProfileId",
                        column: x => x.TabletProfileId,
                        principalTable: "TabletProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PTVehicleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PTRequestDetailId = table.Column<int>(type: "int", nullable: false),
                    IsApprovedToOperate = table.Column<bool>(type: "bit", nullable: true),
                    LicenseClass = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DrugAndAlcoholProfile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NeedCompanyCar = table.Column<bool>(type: "bit", nullable: true),
                    IsApplicationPart2Complete = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PTVehicleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PTVehicleDetails_PromotionRequestDetails_PTRequestDetailId",
                        column: x => x.PTRequestDetailId,
                        principalTable: "PromotionRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PTApplicationRequests_ApplicationId",
                table: "PTApplicationRequests",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PTApplicationRequests_IsDeleted",
                table: "PTApplicationRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTApplicationRequests_PTRequestDetailId",
                table: "PTApplicationRequests",
                column: "PTRequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PTCreditCardDetails_CompanyExpenseCard",
                table: "PTCreditCardDetails",
                column: "CompanyExpenseCard");

            migrationBuilder.CreateIndex(
                name: "IX_PTCreditCardDetails_IsDeleted",
                table: "PTCreditCardDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTCreditCardDetails_PTRequestDetailId",
                table: "PTCreditCardDetails",
                column: "PTRequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PTFolderRequests_IsDeleted",
                table: "PTFolderRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTFolderRequests_PTRequestDetailId",
                table: "PTFolderRequests",
                column: "PTRequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PTITComputerRequirements_ComputerRequirementsId",
                table: "PTITComputerRequirements",
                column: "ComputerRequirementsId");

            migrationBuilder.CreateIndex(
                name: "IX_PTITComputerRequirements_IsDeleted",
                table: "PTITComputerRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTITComputerRequirements_PTRequestDetailId",
                table: "PTITComputerRequirements",
                column: "PTRequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PTITDetails_EmailRequired",
                table: "PTITDetails",
                column: "EmailRequired");

            migrationBuilder.CreateIndex(
                name: "IX_PTITDetails_IsDeleted",
                table: "PTITDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTITDetails_PTRequestDetailId",
                table: "PTITDetails",
                column: "PTRequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PTITPhoneRequirements_IsDeleted",
                table: "PTITPhoneRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTITPhoneRequirements_PTRequestDetailId",
                table: "PTITPhoneRequirements",
                column: "PTRequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PTITTabletProfiles_IsDeleted",
                table: "PTITTabletProfiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTITTabletProfiles_PTRequestDetailId",
                table: "PTITTabletProfiles",
                column: "PTRequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PTITTabletProfiles_TabletProfileId",
                table: "PTITTabletProfiles",
                column: "TabletProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PTVehicleDetails_IsDeleted",
                table: "PTVehicleDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PTVehicleDetails_LicenseClass",
                table: "PTVehicleDetails",
                column: "LicenseClass");

            migrationBuilder.CreateIndex(
                name: "IX_PTVehicleDetails_PTRequestDetailId",
                table: "PTVehicleDetails",
                column: "PTRequestDetailId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PTApplicationRequests");

            migrationBuilder.DropTable(
                name: "PTCreditCardDetails");

            migrationBuilder.DropTable(
                name: "PTFolderRequests");

            migrationBuilder.DropTable(
                name: "PTITComputerRequirements");

            migrationBuilder.DropTable(
                name: "PTITDetails");

            migrationBuilder.DropTable(
                name: "PTITPhoneRequirements");

            migrationBuilder.DropTable(
                name: "PTITTabletProfiles");

            migrationBuilder.DropTable(
                name: "PTVehicleDetails");
        }
    }
}
