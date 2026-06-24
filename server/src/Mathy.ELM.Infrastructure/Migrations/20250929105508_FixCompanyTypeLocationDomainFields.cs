using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCompanyTypeLocationDomainFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Domain column with proper length constraint (200) if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'CompanyTypeLocation'
                    AND COLUMN_NAME = 'Domain'
                )
                BEGIN
                    ALTER TABLE CompanyTypeLocation
                    ADD Domain nvarchar(200) NULL
                END");

            // Add EmailDomain column with proper length constraint (500) if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'CompanyTypeLocation'
                    AND COLUMN_NAME = 'EmailDomain'
                )
                BEGIN
                    ALTER TABLE CompanyTypeLocation
                    ADD EmailDomain nvarchar(500) NULL
                END");

            // Update existing columns to have correct length constraints if they exist with wrong type
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'CompanyTypeLocation'
                    AND COLUMN_NAME = 'Domain'
                    AND CHARACTER_MAXIMUM_LENGTH = -1
                )
                BEGIN
                    ALTER TABLE CompanyTypeLocation
                    ALTER COLUMN Domain nvarchar(200) NULL
                END");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'CompanyTypeLocation'
                    AND COLUMN_NAME = 'EmailDomain'
                    AND CHARACTER_MAXIMUM_LENGTH = -1
                )
                BEGIN
                    ALTER TABLE CompanyTypeLocation
                    ALTER COLUMN EmailDomain nvarchar(500) NULL
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Domain column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'CompanyTypeLocation'
                    AND COLUMN_NAME = 'Domain'
                )
                BEGIN
                    ALTER TABLE CompanyTypeLocation
                    DROP COLUMN Domain
                END");

            // Remove EmailDomain column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'CompanyTypeLocation'
                    AND COLUMN_NAME = 'EmailDomain'
                )
                BEGIN
                    ALTER TABLE CompanyTypeLocation
                    DROP COLUMN EmailDomain
                END");
        }
    }
}
