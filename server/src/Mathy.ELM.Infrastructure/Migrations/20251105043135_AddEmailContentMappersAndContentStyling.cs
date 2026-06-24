using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailContentMappersAndContentStyling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ContentStyling column (also in InitialCreate for new databases)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND name = 'ContentStyling')
                BEGIN
                    ALTER TABLE [EmailTemplates] ADD [ContentStyling] nvarchar(max) NOT NULL DEFAULT ''
                END
            ");

            migrationBuilder.CreateTable(
                name: "EmailContentMappers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentPartType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentSource = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailContentMappers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailContentMappers");

            migrationBuilder.DropColumn(
                name: "ContentStyling",
                table: "EmailTemplates");
        }
    }
}
