using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelSnapshotForPromotionRequestRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NO-OP: Shadow FK columns were already removed by RemoveShadowForeignKeys migration (commit 4e60e82)
            // This migration only updates the ModelSnapshot to reflect the corrected DbContext configuration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PTApplicationRequests_PromotionRequestDetails_PTRequestDetailId",
                table: "PTApplicationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PTBuildingAccessRequirements_PromotionRequestDetails_PTRequestDetailId",
                table: "PTBuildingAccessRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_PTFolderRequests_PromotionRequestDetails_PTRequestDetailId",
                table: "PTFolderRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PTITComputerRequirements_PromotionRequestDetails_PTRequestDetailId",
                table: "PTITComputerRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_PTITTabletProfiles_PromotionRequestDetails_PTRequestDetailId",
                table: "PTITTabletProfiles");

            migrationBuilder.AddColumn<int>(
                name: "PromotionRequestDetailId",
                table: "PTITTabletProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromotionRequestDetailId",
                table: "PTITComputerRequirements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromotionRequestDetailId",
                table: "PTFolderRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromotionRequestDetailId",
                table: "PTApplicationRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PTCreditCardDetailId",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PTITDetailId",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PTITPhoneRequirementId",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PTVehicleDetailId",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PTITTabletProfiles_PromotionRequestDetailId",
                table: "PTITTabletProfiles",
                column: "PromotionRequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PTITComputerRequirements_PromotionRequestDetailId",
                table: "PTITComputerRequirements",
                column: "PromotionRequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PTFolderRequests_PromotionRequestDetailId",
                table: "PTFolderRequests",
                column: "PromotionRequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PTApplicationRequests_PromotionRequestDetailId",
                table: "PTApplicationRequests",
                column: "PromotionRequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequestDetails_PTCreditCardDetailId",
                table: "PromotionRequestDetails",
                column: "PTCreditCardDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequestDetails_PTITDetailId",
                table: "PromotionRequestDetails",
                column: "PTITDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequestDetails_PTITPhoneRequirementId",
                table: "PromotionRequestDetails",
                column: "PTITPhoneRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequestDetails_PTVehicleDetailId",
                table: "PromotionRequestDetails",
                column: "PTVehicleDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionRequestDetails_PTCreditCardDetails_PTCreditCardDetailId",
                table: "PromotionRequestDetails",
                column: "PTCreditCardDetailId",
                principalTable: "PTCreditCardDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionRequestDetails_PTITDetails_PTITDetailId",
                table: "PromotionRequestDetails",
                column: "PTITDetailId",
                principalTable: "PTITDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionRequestDetails_PTITPhoneRequirements_PTITPhoneRequirementId",
                table: "PromotionRequestDetails",
                column: "PTITPhoneRequirementId",
                principalTable: "PTITPhoneRequirements",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PromotionRequestDetails_PTVehicleDetails_PTVehicleDetailId",
                table: "PromotionRequestDetails",
                column: "PTVehicleDetailId",
                principalTable: "PTVehicleDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PTApplicationRequests_PromotionRequestDetails_PTRequestDetailId",
                table: "PTApplicationRequests",
                column: "PTRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PTApplicationRequests_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTApplicationRequests",
                column: "PromotionRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PTBuildingAccessRequirements_PromotionRequestDetails_PTRequestDetailId",
                table: "PTBuildingAccessRequirements",
                column: "PTRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PTFolderRequests_PromotionRequestDetails_PTRequestDetailId",
                table: "PTFolderRequests",
                column: "PTRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PTFolderRequests_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTFolderRequests",
                column: "PromotionRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PTITComputerRequirements_PromotionRequestDetails_PTRequestDetailId",
                table: "PTITComputerRequirements",
                column: "PTRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PTITComputerRequirements_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTITComputerRequirements",
                column: "PromotionRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PTITTabletProfiles_PromotionRequestDetails_PTRequestDetailId",
                table: "PTITTabletProfiles",
                column: "PTRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PTITTabletProfiles_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTITTabletProfiles",
                column: "PromotionRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id");
        }
    }
}
