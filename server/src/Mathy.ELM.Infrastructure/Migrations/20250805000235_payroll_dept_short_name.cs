using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class payroll_dept_short_name : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollDepartmentShortNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    DeptCode = table.Column<int>(type: "int", nullable: false),
                    DeptShortName = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollDepartmentShortNames", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDepartmentShortNames_CompanyCode",
                table: "PayrollDepartmentShortNames",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDepartmentShortNames_CompanyCode_DeptCode",
                table: "PayrollDepartmentShortNames",
                columns: new[] { "CompanyCode", "DeptCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDepartmentShortNames_DeptCode",
                table: "PayrollDepartmentShortNames",
                column: "DeptCode");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDepartmentShortNames_IsDeleted",
                table: "PayrollDepartmentShortNames",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollDepartmentShortNames");
        }
    }
}
