using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionRequestDetailChildTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "FK_PTApplicationRequests_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTApplicationRequests",
                column: "PromotionRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PTFolderRequests_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTFolderRequests",
                column: "PromotionRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PTITComputerRequirements_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTITComputerRequirements",
                column: "PromotionRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PTITTabletProfiles_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTITTabletProfiles",
                column: "PromotionRequestDetailId",
                principalTable: "PromotionRequestDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRequestDetails_PTCreditCardDetails_PTCreditCardDetailId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRequestDetails_PTITDetails_PTITDetailId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRequestDetails_PTITPhoneRequirements_PTITPhoneRequirementId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PromotionRequestDetails_PTVehicleDetails_PTVehicleDetailId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_PTApplicationRequests_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTApplicationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PTFolderRequests_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTFolderRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PTITComputerRequirements_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTITComputerRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_PTITTabletProfiles_PromotionRequestDetails_PromotionRequestDetailId",
                table: "PTITTabletProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PTITTabletProfiles_PromotionRequestDetailId",
                table: "PTITTabletProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PTITComputerRequirements_PromotionRequestDetailId",
                table: "PTITComputerRequirements");

            migrationBuilder.DropIndex(
                name: "IX_PTFolderRequests_PromotionRequestDetailId",
                table: "PTFolderRequests");

            migrationBuilder.DropIndex(
                name: "IX_PTApplicationRequests_PromotionRequestDetailId",
                table: "PTApplicationRequests");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRequestDetails_PTCreditCardDetailId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRequestDetails_PTITDetailId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRequestDetails_PTITPhoneRequirementId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRequestDetails_PTVehicleDetailId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "PromotionRequestDetailId",
                table: "PTITTabletProfiles");

            migrationBuilder.DropColumn(
                name: "PromotionRequestDetailId",
                table: "PTITComputerRequirements");

            migrationBuilder.DropColumn(
                name: "PromotionRequestDetailId",
                table: "PTFolderRequests");

            migrationBuilder.DropColumn(
                name: "PromotionRequestDetailId",
                table: "PTApplicationRequests");

            migrationBuilder.DropColumn(
                name: "PTCreditCardDetailId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "PTITDetailId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "PTITPhoneRequirementId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "PTVehicleDetailId",
                table: "PromotionRequestDetails");
        }
    }
}
