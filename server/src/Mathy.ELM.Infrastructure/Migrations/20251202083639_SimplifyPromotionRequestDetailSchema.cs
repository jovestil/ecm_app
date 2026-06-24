using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPromotionRequestDetailSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentFunctionalDeptCode",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "CurrentTimeCardSupervisorId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "CurrentVacationSupervisorId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "NewTimeCardSupervisorId",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "PayrollType",
                table: "PromotionRequestDetails");

            migrationBuilder.DropColumn(
                name: "RequiresAccess",
                table: "PromotionRequestDetails");

            migrationBuilder.RenameColumn(
                name: "NewVacationSupervisorId",
                table: "PromotionRequestDetails",
                newName: "NewSupervisorId");

            migrationBuilder.RenameColumn(
                name: "NewFunctionalDeptCode",
                table: "PromotionRequestDetails",
                newName: "CurrentSupervisorId");

            migrationBuilder.AlterColumn<int>(
                name: "NewPhysicalLocationCode",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NewSupervisorId",
                table: "PromotionRequestDetails",
                newName: "NewVacationSupervisorId");

            migrationBuilder.RenameColumn(
                name: "CurrentSupervisorId",
                table: "PromotionRequestDetails",
                newName: "NewFunctionalDeptCode");

            migrationBuilder.AlterColumn<int>(
                name: "NewPhysicalLocationCode",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CurrentFunctionalDeptCode",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentTimeCardSupervisorId",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVacationSupervisorId",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewTimeCardSupervisorId",
                table: "PromotionRequestDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PayrollType",
                table: "PromotionRequestDetails",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresAccess",
                table: "PromotionRequestDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
