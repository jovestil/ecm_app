using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureEmailTemplateColumnsExist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Recipients column if it doesn't exist
            migrationBuilder.Sql(@"
                  IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND name = 'Recipients')
                  BEGIN
                      ALTER TABLE [EmailTemplates] ADD [Recipients] nvarchar(1000) NOT NULL DEFAULT ''
                  END
              ");

            // Add TriggerType column if it doesn't exist
            migrationBuilder.Sql(@"
                  IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND name = 'TriggerType')
                  BEGIN
                      ALTER TABLE [EmailTemplates] ADD [TriggerType] nvarchar(10) NOT NULL DEFAULT ''
                  END
              ");

            // Add SubmissionFreq column if it doesn't exist
            migrationBuilder.Sql(@"
                  IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND name = 'SubmissionFreq')
                  BEGIN
                      ALTER TABLE [EmailTemplates] ADD [SubmissionFreq] int NULL DEFAULT 0
                  END
              ");

            // Add ContentStyling column if it doesn't exist
            migrationBuilder.Sql(@"
                  IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EmailTemplates]') AND name = 'ContentStyling')
                  BEGIN
                      ALTER TABLE [EmailTemplates] ADD [ContentStyling] nvarchar(max) NOT NULL DEFAULT ''
                  END
              ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration needed - we don't want to remove these columns
        }
    }
}
