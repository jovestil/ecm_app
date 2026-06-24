using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewHireRequestDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewHireRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PreferredFirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FirstDayEmployment = table.Column<DateTime>(type: "date", nullable: false),
                    ReferredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Rehire = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    LocationCode = table.Column<int>(type: "int", nullable: false),
                    EmploymentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HourlySalaried = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PositionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayrollDeptCode = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewHireRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewHireRequestDetails_HRRequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "HRRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewHireRequestDetails_CompanyCode",
                table: "NewHireRequestDetails",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_NewHireRequestDetails_FirstDayEmployment",
                table: "NewHireRequestDetails",
                column: "FirstDayEmployment");

            migrationBuilder.CreateIndex(
                name: "IX_NewHireRequestDetails_IsDeleted",
                table: "NewHireRequestDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NewHireRequestDetails_RequestDetailId",
                table: "NewHireRequestDetails",
                column: "RequestDetailId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewHireRequestDetails");
        }
    }
}
