using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDLAndUpdateEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TriggerType and SubmissionFreq columns (also in InitialCreate for new databases)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND name = 'SubmissionFreq')
                BEGIN
                    ALTER TABLE [EmailTemplates] ADD [SubmissionFreq] int NULL DEFAULT 0
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND name = 'TriggerType')
                BEGIN
                    ALTER TABLE [EmailTemplates] ADD [TriggerType] nvarchar(10) NOT NULL DEFAULT ''
                END
            ");

            migrationBuilder.CreateTable(
                name: "CompanyDL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    SiteDL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SecurityDL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreditCardDL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FleetDL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ComplianceDL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SafetyDL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HRDL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ITDL = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDL", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDL_CompanyCode",
                table: "CompanyDL",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDL_IsDeleted",
                table: "CompanyDL",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyDL");

            migrationBuilder.DropColumn(
                name: "SubmissionFreq",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "EmailTemplates");
        }
    }
}
