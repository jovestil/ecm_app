using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesToMatchDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeCardSupervisorId",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "VacationSupervisorId",
                table: "Employees",
                newName: "SupervisorId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "UnionCrafts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "CompanyCode",
                table: "UnionCrafts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CraftCode",
                table: "UnionCrafts",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ViewpointSyncDate",
                table: "UnionCrafts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeID",
                table: "NewHireRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsChild",
                table: "ITComputerRequirements",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "ITComputerRequirements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeLicenseClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicenseClass = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    IsUnion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ViewpointSyncDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLicenseClasses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnionCrafts_CompanyCode",
                table: "UnionCrafts",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_UnionCrafts_CraftCode",
                table: "UnionCrafts",
                column: "CraftCode");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SupervisorId",
                table: "Employees",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLicenseClasses_IsDeleted",
                table: "EmployeeLicenseClasses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLicenseClasses_LicenseClass",
                table: "EmployeeLicenseClasses",
                column: "LicenseClass");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeLicenseClasses");

            migrationBuilder.DropIndex(
                name: "IX_UnionCrafts_CompanyCode",
                table: "UnionCrafts");

            migrationBuilder.DropIndex(
                name: "IX_UnionCrafts_CraftCode",
                table: "UnionCrafts");

            migrationBuilder.DropIndex(
                name: "IX_Employees_SupervisorId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CompanyCode",
                table: "UnionCrafts");

            migrationBuilder.DropColumn(
                name: "CraftCode",
                table: "UnionCrafts");

            migrationBuilder.DropColumn(
                name: "ViewpointSyncDate",
                table: "UnionCrafts");

            migrationBuilder.DropColumn(
                name: "EmployeeID",
                table: "NewHireRequestDetails");

            migrationBuilder.DropColumn(
                name: "IsChild",
                table: "ITComputerRequirements");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "ITComputerRequirements");

            migrationBuilder.RenameColumn(
                name: "SupervisorId",
                table: "Employees",
                newName: "VacationSupervisorId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "UnionCrafts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<int>(
                name: "TimeCardSupervisorId",
                table: "Employees",
                type: "int",
                nullable: true);
        }
    }
}
