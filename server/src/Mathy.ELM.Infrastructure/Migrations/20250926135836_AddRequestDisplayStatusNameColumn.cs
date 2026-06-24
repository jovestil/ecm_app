using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestDisplayStatusNameColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before adding it
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'RequestStatuses'
                    AND COLUMN_NAME = 'RequestDisplayStatusName'
                )
                BEGIN
                    ALTER TABLE RequestStatuses
                    ADD RequestDisplayStatusName nvarchar(50) NULL
                END");

            // Update existing records with display status names (only update NULL values to avoid overwriting existing data)
            migrationBuilder.Sql("UPDATE RequestStatuses SET RequestDisplayStatusName = 'Submitted' WHERE RequestStatusName = 'Pending' AND RequestDisplayStatusName IS NULL");
            migrationBuilder.Sql("UPDATE RequestStatuses SET RequestDisplayStatusName = 'Submitted' WHERE RequestStatusName = 'Processing' AND RequestDisplayStatusName IS NULL");
            migrationBuilder.Sql("UPDATE RequestStatuses SET RequestDisplayStatusName = 'Submitted' WHERE RequestStatusName = 'Completed' AND RequestDisplayStatusName IS NULL");
            migrationBuilder.Sql("UPDATE RequestStatuses SET RequestDisplayStatusName = 'Submitted' WHERE RequestStatusName = 'Failed' AND RequestDisplayStatusName IS NULL");
            migrationBuilder.Sql("UPDATE RequestStatuses SET RequestDisplayStatusName = 'Submitted' WHERE RequestStatusName = 'Cancelled' AND RequestDisplayStatusName IS NULL");
            migrationBuilder.Sql("UPDATE RequestStatuses SET RequestDisplayStatusName = 'Draft' WHERE RequestStatusName = 'Draft' AND RequestDisplayStatusName IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before dropping it
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'RequestStatuses'
                    AND COLUMN_NAME = 'RequestDisplayStatusName'
                )
                BEGIN
                    ALTER TABLE RequestStatuses
                    DROP COLUMN RequestDisplayStatusName
                END");
        }
    }
}
