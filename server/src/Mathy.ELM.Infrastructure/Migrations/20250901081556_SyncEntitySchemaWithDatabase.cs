using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncEntitySchemaWithDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationRequests_ITDetails_ITDetailId",
                table: "ApplicationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditCardDetails_HRRequestDetails_RequestDetailId",
                table: "CreditCardDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_FolderRequests_ITDetails_ITDetailId",
                table: "FolderRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ITDetails_HRRequestDetails_RequestDetailId",
                table: "ITDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleDetails_HRRequestDetails_RequestDetailId",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "ApplicationPart2Complete",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "CompanyCar",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "CompanyVehicleApproved",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "DrugProfile",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "CardlockAccess",
                table: "CreditCardDetails");

            migrationBuilder.DropColumn(
                name: "ExpenseCard",
                table: "CreditCardDetails");

            migrationBuilder.RenameColumn(
                name: "RequestDetailId",
                table: "VehicleDetails",
                newName: "NewHireRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_VehicleDetails_RequestDetailId",
                table: "VehicleDetails",
                newName: "IX_VehicleDetails_NewHireRequestId");

            migrationBuilder.RenameColumn(
                name: "RequestDetailId",
                table: "ITDetails",
                newName: "NewHireRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_ITDetails_RequestDetailId",
                table: "ITDetails",
                newName: "IX_ITDetails_NewHireRequestId");

            migrationBuilder.RenameColumn(
                name: "EmployeePositonCode",
                table: "HRRequestDetails",
                newName: "EmployeePositionCode");

            migrationBuilder.RenameColumn(
                name: "ITDetailId",
                table: "FolderRequests",
                newName: "NewHireRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_FolderRequests_ITDetailId",
                table: "FolderRequests",
                newName: "IX_FolderRequests_NewHireRequestId");

            migrationBuilder.RenameColumn(
                name: "RequestDetailId",
                table: "CreditCardDetails",
                newName: "NewHireRequestId");

            migrationBuilder.RenameColumn(
                name: "CardlockAddress",
                table: "CreditCardDetails",
                newName: "FuelCardlockAddress");

            migrationBuilder.RenameIndex(
                name: "IX_CreditCardDetails_RequestDetailId",
                table: "CreditCardDetails",
                newName: "IX_CreditCardDetails_NewHireRequestId");

            migrationBuilder.RenameColumn(
                name: "ApplicationName",
                table: "Applications",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "ApplicationDescription",
                table: "Applications",
                newName: "Description");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_ApplicationName",
                table: "Applications",
                newName: "IX_Applications_Name");

            migrationBuilder.RenameColumn(
                name: "ITDetailId",
                table: "ApplicationRequests",
                newName: "NewHireRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationRequests_ITDetailId",
                table: "ApplicationRequests",
                newName: "IX_ApplicationRequests_NewHireRequestId");

            migrationBuilder.AddColumn<string>(
                name: "DrugAndAlcoholProfile",
                table: "VehicleDetails",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApplicationPart2Complete",
                table: "VehicleDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedToOperate",
                table: "VehicleDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedCompanyCar",
                table: "VehicleDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PositionCode",
                table: "NewHireRequestDetails",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "EmploymentStatus",
                table: "NewHireRequestDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprentice",
                table: "NewHireRequestDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnion",
                table: "NewHireRequestDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnionWage",
                table: "NewHireRequestDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "NewHireRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnionCraftId",
                table: "NewHireRequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "EmailRequired",
                table: "ITDetails",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MSOfficeLicenseE5",
                table: "ITDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MSOfficeLicenseF3",
                table: "ITDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeDepartmentCode",
                table: "HRRequestDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "KwikTripCard",
                table: "CreditCardDetails",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompanyExpenseCard",
                table: "CreditCardDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EEExpenseCard",
                table: "CreditCardDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FuelCardlockAccess",
                table: "CreditCardDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FuelOnlyCard",
                table: "CreditCardDetails",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyTypeLocation",
                table: "Applications",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BuildingAccessRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyTypeLocation = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingAccessRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComputerRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComputerRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalaryTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    SalaryCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ViewpointSyncDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalaryTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ViewpointSyncDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ITPhoneRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewHireRequestId = table.Column<int>(type: "int", nullable: false),
                    DeskPhone = table.Column<bool>(type: "bit", nullable: true),
                    CompanyCellphone = table.Column<bool>(type: "bit", nullable: true),
                    BYODCellphone = table.Column<bool>(type: "bit", nullable: true),
                    WorkPhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WorkExtension = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReusingExistingPhone = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITPhoneRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITPhoneRequirements_NewHireRequestDetails_NewHireRequestId",
                        column: x => x.NewHireRequestId,
                        principalTable: "NewHireRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TabletProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyTypeLocation = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TabletProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnionCrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnionCrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewHireBuildingAccessRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewHireRequestId = table.Column<int>(type: "int", nullable: false),
                    AccessId = table.Column<int>(type: "int", nullable: false),
                    AccessDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewHireBuildingAccessRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewHireBuildingAccessRequirements_BuildingAccessRequirements_AccessId",
                        column: x => x.AccessId,
                        principalTable: "BuildingAccessRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewHireBuildingAccessRequirements_NewHireRequestDetails_NewHireRequestId",
                        column: x => x.NewHireRequestId,
                        principalTable: "NewHireRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ITComputerRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewHireRequestId = table.Column<int>(type: "int", nullable: false),
                    ComputerRequirementsId = table.Column<int>(type: "int", nullable: false),
                    ComputerRequirementsDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITComputerRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITComputerRequirements_ComputerRequirements_ComputerRequirementsId",
                        column: x => x.ComputerRequirementsId,
                        principalTable: "ComputerRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITComputerRequirements_NewHireRequestDetails_NewHireRequestId",
                        column: x => x.NewHireRequestId,
                        principalTable: "NewHireRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ITTabletProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NewHireRequestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ITTabletProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITTabletProfiles_NewHireRequestDetails_NewHireRequestId",
                        column: x => x.NewHireRequestId,
                        principalTable: "NewHireRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITTabletProfiles_TabletProfiles_TabletProfileId",
                        column: x => x.TabletProfileId,
                        principalTable: "TabletProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ITDetails_EmailRequired",
                table: "ITDetails",
                column: "EmailRequired");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardDetails_CompanyExpenseCard",
                table: "CreditCardDetails",
                column: "CompanyExpenseCard");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingAccessRequirements_IsActive",
                table: "BuildingAccessRequirements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingAccessRequirements_IsDeleted",
                table: "BuildingAccessRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerRequirements_IsActive",
                table: "ComputerRequirements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ComputerRequirements_IsDeleted",
                table: "ComputerRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryTypes_CompanyCode",
                table: "EmployeeSalaryTypes",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryTypes_IsActive",
                table: "EmployeeSalaryTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryTypes_IsDeleted",
                table: "EmployeeSalaryTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatuses_CompanyCode",
                table: "EmploymentStatuses",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatuses_IsActive",
                table: "EmploymentStatuses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentStatuses_IsDeleted",
                table: "EmploymentStatuses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ITComputerRequirements_ComputerRequirementsId",
                table: "ITComputerRequirements",
                column: "ComputerRequirementsId");

            migrationBuilder.CreateIndex(
                name: "IX_ITComputerRequirements_IsDeleted",
                table: "ITComputerRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ITComputerRequirements_NewHireRequestId",
                table: "ITComputerRequirements",
                column: "NewHireRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ITPhoneRequirements_IsDeleted",
                table: "ITPhoneRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ITPhoneRequirements_NewHireRequestId",
                table: "ITPhoneRequirements",
                column: "NewHireRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ITTabletProfiles_IsDeleted",
                table: "ITTabletProfiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ITTabletProfiles_NewHireRequestId",
                table: "ITTabletProfiles",
                column: "NewHireRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ITTabletProfiles_TabletProfileId",
                table: "ITTabletProfiles",
                column: "TabletProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_NewHireBuildingAccessRequirements_AccessId",
                table: "NewHireBuildingAccessRequirements",
                column: "AccessId");

            migrationBuilder.CreateIndex(
                name: "IX_NewHireBuildingAccessRequirements_IsDeleted",
                table: "NewHireBuildingAccessRequirements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NewHireBuildingAccessRequirements_NewHireRequestId",
                table: "NewHireBuildingAccessRequirements",
                column: "NewHireRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TabletProfiles_IsActive",
                table: "TabletProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TabletProfiles_IsDeleted",
                table: "TabletProfiles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UnionCrafts_IsActive",
                table: "UnionCrafts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UnionCrafts_IsDeleted",
                table: "UnionCrafts",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationRequests_NewHireRequestDetails_NewHireRequestId",
                table: "ApplicationRequests",
                column: "NewHireRequestId",
                principalTable: "NewHireRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditCardDetails_NewHireRequestDetails_NewHireRequestId",
                table: "CreditCardDetails",
                column: "NewHireRequestId",
                principalTable: "NewHireRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FolderRequests_NewHireRequestDetails_NewHireRequestId",
                table: "FolderRequests",
                column: "NewHireRequestId",
                principalTable: "NewHireRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITDetails_NewHireRequestDetails_NewHireRequestId",
                table: "ITDetails",
                column: "NewHireRequestId",
                principalTable: "NewHireRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleDetails_NewHireRequestDetails_NewHireRequestId",
                table: "VehicleDetails",
                column: "NewHireRequestId",
                principalTable: "NewHireRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationRequests_NewHireRequestDetails_NewHireRequestId",
                table: "ApplicationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditCardDetails_NewHireRequestDetails_NewHireRequestId",
                table: "CreditCardDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_FolderRequests_NewHireRequestDetails_NewHireRequestId",
                table: "FolderRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ITDetails_NewHireRequestDetails_NewHireRequestId",
                table: "ITDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleDetails_NewHireRequestDetails_NewHireRequestId",
                table: "VehicleDetails");

            migrationBuilder.DropTable(
                name: "EmployeeSalaryTypes");

            migrationBuilder.DropTable(
                name: "EmploymentStatuses");

            migrationBuilder.DropTable(
                name: "ITComputerRequirements");

            migrationBuilder.DropTable(
                name: "ITPhoneRequirements");

            migrationBuilder.DropTable(
                name: "ITTabletProfiles");

            migrationBuilder.DropTable(
                name: "NewHireBuildingAccessRequirements");

            migrationBuilder.DropTable(
                name: "UnionCrafts");

            migrationBuilder.DropTable(
                name: "ComputerRequirements");

            migrationBuilder.DropTable(
                name: "TabletProfiles");

            migrationBuilder.DropTable(
                name: "BuildingAccessRequirements");

            migrationBuilder.DropIndex(
                name: "IX_ITDetails_EmailRequired",
                table: "ITDetails");

            migrationBuilder.DropIndex(
                name: "IX_CreditCardDetails_CompanyExpenseCard",
                table: "CreditCardDetails");

            migrationBuilder.DropColumn(
                name: "DrugAndAlcoholProfile",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "IsApplicationPart2Complete",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "IsApprovedToOperate",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "NeedCompanyCar",
                table: "VehicleDetails");

            migrationBuilder.DropColumn(
                name: "IsApprentice",
                table: "NewHireRequestDetails");

            migrationBuilder.DropColumn(
                name: "IsUnion",
                table: "NewHireRequestDetails");

            migrationBuilder.DropColumn(
                name: "IsUnionWage",
                table: "NewHireRequestDetails");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "NewHireRequestDetails");

            migrationBuilder.DropColumn(
                name: "UnionCraftId",
                table: "NewHireRequestDetails");

            migrationBuilder.DropColumn(
                name: "MSOfficeLicenseE5",
                table: "ITDetails");

            migrationBuilder.DropColumn(
                name: "MSOfficeLicenseF3",
                table: "ITDetails");

            migrationBuilder.DropColumn(
                name: "CompanyExpenseCard",
                table: "CreditCardDetails");

            migrationBuilder.DropColumn(
                name: "EEExpenseCard",
                table: "CreditCardDetails");

            migrationBuilder.DropColumn(
                name: "FuelCardlockAccess",
                table: "CreditCardDetails");

            migrationBuilder.DropColumn(
                name: "FuelOnlyCard",
                table: "CreditCardDetails");

            migrationBuilder.DropColumn(
                name: "CompanyTypeLocation",
                table: "Applications");

            migrationBuilder.RenameColumn(
                name: "NewHireRequestId",
                table: "VehicleDetails",
                newName: "RequestDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_VehicleDetails_NewHireRequestId",
                table: "VehicleDetails",
                newName: "IX_VehicleDetails_RequestDetailId");

            migrationBuilder.RenameColumn(
                name: "NewHireRequestId",
                table: "ITDetails",
                newName: "RequestDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_ITDetails_NewHireRequestId",
                table: "ITDetails",
                newName: "IX_ITDetails_RequestDetailId");

            migrationBuilder.RenameColumn(
                name: "EmployeePositionCode",
                table: "HRRequestDetails",
                newName: "EmployeePositonCode");

            migrationBuilder.RenameColumn(
                name: "NewHireRequestId",
                table: "FolderRequests",
                newName: "ITDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_FolderRequests_NewHireRequestId",
                table: "FolderRequests",
                newName: "IX_FolderRequests_ITDetailId");

            migrationBuilder.RenameColumn(
                name: "NewHireRequestId",
                table: "CreditCardDetails",
                newName: "RequestDetailId");

            migrationBuilder.RenameColumn(
                name: "FuelCardlockAddress",
                table: "CreditCardDetails",
                newName: "CardlockAddress");

            migrationBuilder.RenameIndex(
                name: "IX_CreditCardDetails_NewHireRequestId",
                table: "CreditCardDetails",
                newName: "IX_CreditCardDetails_RequestDetailId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Applications",
                newName: "ApplicationName");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Applications",
                newName: "ApplicationDescription");

            migrationBuilder.RenameIndex(
                name: "IX_Applications_Name",
                table: "Applications",
                newName: "IX_Applications_ApplicationName");

            migrationBuilder.RenameColumn(
                name: "NewHireRequestId",
                table: "ApplicationRequests",
                newName: "ITDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationRequests_NewHireRequestId",
                table: "ApplicationRequests",
                newName: "IX_ApplicationRequests_ITDetailId");

            migrationBuilder.AddColumn<bool>(
                name: "ApplicationPart2Complete",
                table: "VehicleDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompanyCar",
                table: "VehicleDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CompanyVehicleApproved",
                table: "VehicleDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DrugProfile",
                table: "VehicleDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PositionCode",
                table: "NewHireRequestDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "EmploymentStatus",
                table: "NewHireRequestDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<bool>(
                name: "EmailRequired",
                table: "ITDetails",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeDepartmentCode",
                table: "HRRequestDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "KwikTripCard",
                table: "CreditCardDetails",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CardlockAccess",
                table: "CreditCardDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExpenseCard",
                table: "CreditCardDetails",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationRequests_ITDetails_ITDetailId",
                table: "ApplicationRequests",
                column: "ITDetailId",
                principalTable: "ITDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditCardDetails_HRRequestDetails_RequestDetailId",
                table: "CreditCardDetails",
                column: "RequestDetailId",
                principalTable: "HRRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FolderRequests_ITDetails_ITDetailId",
                table: "FolderRequests",
                column: "ITDetailId",
                principalTable: "ITDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITDetails_HRRequestDetails_RequestDetailId",
                table: "ITDetails",
                column: "RequestDetailId",
                principalTable: "HRRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleDetails_HRRequestDetails_RequestDetailId",
                table: "VehicleDetails",
                column: "RequestDetailId",
                principalTable: "HRRequestDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
