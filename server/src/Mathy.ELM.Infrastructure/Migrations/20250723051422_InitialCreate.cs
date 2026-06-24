using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Mathy.ELM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApplicationDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmailType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "varchar(max)", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: ""),
                    SubmissionFreq = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ContentStyling = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: ""),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    EmployeeNumber = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PersonalEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WorkEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NetworkId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PayrollCompanyCode = table.Column<int>(type: "int", nullable: true),
                    PayrollGroupCode = table.Column<int>(type: "int", nullable: true),
                    PayrollDeptCode = table.Column<int>(type: "int", nullable: true),
                    PositionCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TimeCardSupervisorId = table.Column<int>(type: "int", nullable: true),
                    VacationSupervisorId = table.Column<int>(type: "int", nullable: true),
                    FunctionalDeptCode = table.Column<int>(type: "int", nullable: true),
                    PhysicalLocationCode = table.Column<int>(type: "int", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TerminationReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReturnToWorkDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmploymentStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ViewpointSyncDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FunctionalDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FunctionalDeptCode = table.Column<int>(type: "int", nullable: false),
                    FunctionalDeptName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_FunctionalDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HRRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmittedBy = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "varchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    DeptCode = table.Column<int>(type: "int", nullable: false),
                    DeptName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_PayrollDepartments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    GroupCode = table.Column<int>(type: "int", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_PayrollGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationCode = table.Column<int>(type: "int", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_PhysicalLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    PositionCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestStatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestStatusDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestTypeDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TerminationReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReasonDescription = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminationReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCompanyAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CompanyCode = table.Column<int>(type: "int", nullable: false),
                    CanSubmitRequests = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanyAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompanyAccess_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    ToEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CcEmail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "varchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastAttempt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "varchar(max)", nullable: true),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailTemplateId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationQueue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationQueue_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NotificationQueue_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationQueue_HRRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "HRRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HRRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentRequestId = table.Column<int>(type: "int", nullable: false),
                    RequestTypeId = table.Column<int>(type: "int", nullable: false),
                    RequestStatusId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeNetworkId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    EmployeePositonCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EmployeeCompanyCode = table.Column<int>(type: "int", nullable: true),
                    EmployeeDeparmentCode = table.Column<int>(type: "int", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "date", nullable: true),
                    ProcessingNotes = table.Column<string>(type: "varchar(max)", nullable: true),
                    ViewpointProcessed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ViewpointProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ViewpointErrorMessage = table.Column<string>(type: "varchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HRRequestDetails_HRRequests_ParentRequestId",
                        column: x => x.ParentRequestId,
                        principalTable: "HRRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HRRequestDetails_RequestStatuses_RequestStatusId",
                        column: x => x.RequestStatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HRRequestDetails_RequestTypes_RequestTypeId",
                        column: x => x.RequestTypeId,
                        principalTable: "RequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CreditCardDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    KwikTripCard = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExpenseCard = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WeeklyLimit = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    CardlockAccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CardlockAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCardDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditCardDetails_HRRequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "HRRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ITDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    EmailRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AlternateDeliveryLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITDetails_HRRequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "HRRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LayoffRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    LastDayWorked = table.Column<DateTime>(type: "date", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LayoffRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LayoffRequestDetails_HRRequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "HRRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    CurrentPayrollCompanyCode = table.Column<int>(type: "int", nullable: true),
                    CurrentPayrollGroupCode = table.Column<int>(type: "int", nullable: true),
                    CurrentPayrollDeptCode = table.Column<int>(type: "int", nullable: true),
                    CurrentPositionCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CurrentTimeCardSupervisorId = table.Column<int>(type: "int", nullable: true),
                    CurrentVacationSupervisorId = table.Column<int>(type: "int", nullable: true),
                    CurrentFunctionalDeptCode = table.Column<int>(type: "int", nullable: true),
                    CurrentPhysicalLocationCode = table.Column<int>(type: "int", nullable: true),
                    CurrentStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NewPayrollCompanyCode = table.Column<int>(type: "int", nullable: false),
                    NewPayrollGroupCode = table.Column<int>(type: "int", nullable: false),
                    NewPayrollDeptCode = table.Column<int>(type: "int", nullable: false),
                    NewPositionCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NewTimeCardSupervisorId = table.Column<int>(type: "int", nullable: false),
                    NewVacationSupervisorId = table.Column<int>(type: "int", nullable: true),
                    NewFunctionalDeptCode = table.Column<int>(type: "int", nullable: true),
                    NewPhysicalLocationCode = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PayrollType = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    RequiresAccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionRequestDetails_HRRequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "HRRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnToWorkRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnToWorkRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnToWorkRequestDetails_HRRequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "HRRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TerminationRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContestUnemployment = table.Column<bool>(type: "bit", nullable: true),
                    ForwardEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ForwardDeskPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ForwardCellPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AutoReply = table.Column<string>(type: "varchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerminationRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerminationRequestDetails_HRRequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "HRRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    CompanyVehicleApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DriverClassification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DrugProfile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyCar = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ApplicationPart2Complete = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleDetails_HRRequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "HRRequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ITDetailId = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    AccessNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationRequests_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationRequests_ITDetails_ITDetailId",
                        column: x => x.ITDetailId,
                        principalTable: "ITDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FolderRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ITDetailId = table.Column<int>(type: "int", nullable: false),
                    FolderType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FolderName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FolderRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FolderRequests_ITDetails_ITDetailId",
                        column: x => x.ITDetailId,
                        principalTable: "ITDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "RequestStatuses",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "IsDeleted", "ModifiedBy", "ModifiedDate", "RequestStatusDescription", "RequestStatusName" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Request submitted and awaiting processing", "Pending" },
                    { 2, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Request is currently being processed", "Processing" },
                    { 3, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Request has been completed successfully", "Completed" },
                    { 4, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Request processing failed", "Failed" },
                    { 5, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Request was cancelled", "Cancelled" },
                    { 6, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Request was drafted", "Draft" }
                });

            migrationBuilder.InsertData(
                table: "RequestTypes",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "IsDeleted", "ModifiedBy", "ModifiedDate", "RequestTypeDescription", "RequestTypeName" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Employee promotion or transfer request", "Promotion" },
                    { 2, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Employee layoff request", "Layoff" },
                    { 3, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Employee termination request", "Termination" },
                    { 4, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "Return to work request for laid-off employees", "ReturnToWork" },
                    { 5, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "New hire request (future implementation)", "NewHire" }
                });

            migrationBuilder.InsertData(
                table: "TerminationReasons",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "IsDeleted", "ModifiedBy", "ModifiedDate", "ReasonCode", "ReasonDescription" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT SCHOOL", "VT SCHOOL" },
                    { 2, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT SALARY", "VT SALARY" },
                    { 3, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT RETIRE", "VT RETIRE" },
                    { 4, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT PERSONL", "VT PERSONL" },
                    { 5, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT NOSHOW", "VT NOSHOW" },
                    { 6, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT NOAVAIL", "VT NOAVAIL" },
                    { 7, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT NO WORK", "VT NO WORK" },
                    { 8, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT NO FIT", "VT NO FIT" },
                    { 9, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT MOVE", "VT MOVE" },
                    { 10, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT FAMILY", "VT FAMILY" },
                    { 11, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT EVERIFY", "VT EVERIFY" },
                    { 12, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT DIF JOB", "VT DIF JOB" },
                    { 13, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VT DEGREE", "VT DEGREE" },
                    { 14, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "VOLUNTARY", "VOLUNTARY" },
                    { 15, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "UR", "UR" },
                    { 16, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "TRANSFER", "TRANSFER" },
                    { 17, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "RETIRED", "RETIRED" },
                    { 18, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "MERIT", "MERIT" },
                    { 19, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "IT SAFETY", "IT SAFETY" },
                    { 20, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "IT PERF", "IT PERF" },
                    { 21, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "IT DA POL", "IT DA POL" },
                    { 22, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "IT BEHAVR", "IT BEHAVR" },
                    { 23, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "IT ATTEND", "IT ATTEND" },
                    { 24, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "INVOLUNTAR", "INVOLUNTAR" },
                    { 25, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "DISABLED", "DISABLED" },
                    { 26, 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, null, null, "DECEASED", "DECEASED" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_ApplicationId",
                table: "ApplicationRequests",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_IsDeleted",
                table: "ApplicationRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_ITDetailId",
                table: "ApplicationRequests",
                column: "ITDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicationName",
                table: "Applications",
                column: "ApplicationName");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_IsActive",
                table: "Applications",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_IsDeleted",
                table: "Applications",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyCode",
                table: "Companies",
                column: "CompanyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsActive",
                table: "Companies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsDeleted",
                table: "Companies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardDetails_IsDeleted",
                table: "CreditCardDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardDetails_RequestDetailId",
                table: "CreditCardDetails",
                column: "RequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_EmailType",
                table: "EmailTemplates",
                column: "EmailType");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_IsActive",
                table: "EmailTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_IsDeleted",
                table: "EmailTemplates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_RequestType",
                table: "EmailTemplates",
                column: "RequestType");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyCode",
                table: "Employees",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyCode_EmployeeNumber",
                table: "Employees",
                columns: new[] { "CompanyCode", "EmployeeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmploymentStatus",
                table: "Employees",
                column: "EmploymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FirstName",
                table: "Employees",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsDeleted",
                table: "Employees",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_LastName",
                table: "Employees",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_NetworkId",
                table: "Employees",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_FolderRequests_IsDeleted",
                table: "FolderRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FolderRequests_ITDetailId",
                table: "FolderRequests",
                column: "ITDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionalDepartment_IsActive",
                table: "FunctionalDepartments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionalDepartment_IsDeleted",
                table: "FunctionalDepartments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionalDepartments_FunctionalDeptCode",
                table: "FunctionalDepartments",
                column: "FunctionalDeptCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HRRequestDetails_EffectiveDate",
                table: "HRRequestDetails",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequestDetails_EmployeeId",
                table: "HRRequestDetails",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequestDetails_EmployeeNetworkId",
                table: "HRRequestDetails",
                column: "EmployeeNetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequestDetails_IsDeleted",
                table: "HRRequestDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequestDetails_ParentRequestId",
                table: "HRRequestDetails",
                column: "ParentRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequestDetails_RequestStatusId",
                table: "HRRequestDetails",
                column: "RequestStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequestDetails_RequestTypeId",
                table: "HRRequestDetails",
                column: "RequestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequests_IsDeleted",
                table: "HRRequests",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequests_SubmittedBy",
                table: "HRRequests",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HRRequests_SubmittedDate",
                table: "HRRequests",
                column: "SubmittedDate");

            migrationBuilder.CreateIndex(
                name: "IX_ITDetails_IsDeleted",
                table: "ITDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ITDetails_RequestDetailId",
                table: "ITDetails",
                column: "RequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LayoffRequestDetails_IsDeleted",
                table: "LayoffRequestDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LayoffRequestDetails_LastDayWorked",
                table: "LayoffRequestDetails",
                column: "LastDayWorked");

            migrationBuilder.CreateIndex(
                name: "IX_LayoffRequestDetails_RequestDetailId",
                table: "LayoffRequestDetails",
                column: "RequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_CreatedDate",
                table: "NotificationQueue",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_EmailTemplateId",
                table: "NotificationQueue",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_IsDeleted",
                table: "NotificationQueue",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_RequestId",
                table: "NotificationQueue",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_Status",
                table: "NotificationQueue",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueue_TemplateId",
                table: "NotificationQueue",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDepartments_CompanyCode_DeptCode",
                table: "PayrollDepartments",
                columns: new[] { "CompanyCode", "DeptCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDepartments_DeptCode",
                table: "PayrollDepartments",
                column: "DeptCode");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDepartments_IsActive",
                table: "PayrollDepartments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDepartments_IsDeleted",
                table: "PayrollDepartments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollGroups_CompanyCode_GroupCode",
                table: "PayrollGroups",
                columns: new[] { "CompanyCode", "GroupCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollGroups_GroupCode",
                table: "PayrollGroups",
                column: "GroupCode");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollGroups_IsActive",
                table: "PayrollGroups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollGroups_IsDeleted",
                table: "PayrollGroups",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalLocations_IsActive",
                table: "PhysicalLocations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalLocations_IsDeleted",
                table: "PhysicalLocations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalLocations_LocationCode",
                table: "PhysicalLocations",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_CompanyCode_PositionCode",
                table: "Positions",
                columns: new[] { "CompanyCode", "PositionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_IsActive",
                table: "Positions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_IsDeleted",
                table: "Positions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PositionCode",
                table: "Positions",
                column: "PositionCode");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequestDetails_IsDeleted",
                table: "PromotionRequestDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRequestDetails_RequestDetailId",
                table: "PromotionRequestDetails",
                column: "RequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatuses_IsActive",
                table: "RequestStatuses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatuses_IsDeleted",
                table: "RequestStatuses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RequestStatuses_RequestStatusName",
                table: "RequestStatuses",
                column: "RequestStatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_IsActive",
                table: "RequestTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_IsDeleted",
                table: "RequestTypes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTypes_RequestTypeName",
                table: "RequestTypes",
                column: "RequestTypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnToWorkRequestDetails_IsDeleted",
                table: "ReturnToWorkRequestDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnToWorkRequestDetails_RequestDetailId",
                table: "ReturnToWorkRequestDetails",
                column: "RequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TerminationReasons_IsActive",
                table: "TerminationReasons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TerminationReasons_IsDeleted",
                table: "TerminationReasons",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TerminationReasons_ReasonCode",
                table: "TerminationReasons",
                column: "ReasonCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TerminationRequestDetails_IsDeleted",
                table: "TerminationRequestDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TerminationRequestDetails_Reason",
                table: "TerminationRequestDetails",
                column: "ReasonCode");

            migrationBuilder.CreateIndex(
                name: "IX_TerminationRequestDetails_RequestDetailId",
                table: "TerminationRequestDetails",
                column: "RequestDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyAccess_CompanyCode",
                table: "UserCompanyAccess",
                column: "CompanyCode");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyAccess_CompanyId",
                table: "UserCompanyAccess",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyAccess_IsDeleted",
                table: "UserCompanyAccess",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyAccess_UserId",
                table: "UserCompanyAccess",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyAccess_UserId_CompanyCode",
                table: "UserCompanyAccess",
                columns: new[] { "UserId", "CompanyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDetails_IsDeleted",
                table: "VehicleDetails",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDetails_RequestDetailId",
                table: "VehicleDetails",
                column: "RequestDetailId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationRequests");

            migrationBuilder.DropTable(
                name: "CreditCardDetails");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "FolderRequests");

            migrationBuilder.DropTable(
                name: "FunctionalDepartments");

            migrationBuilder.DropTable(
                name: "LayoffRequestDetails");

            migrationBuilder.DropTable(
                name: "NotificationQueue");

            migrationBuilder.DropTable(
                name: "PayrollDepartments");

            migrationBuilder.DropTable(
                name: "PayrollGroups");

            migrationBuilder.DropTable(
                name: "PhysicalLocations");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "PromotionRequestDetails");

            migrationBuilder.DropTable(
                name: "ReturnToWorkRequestDetails");

            migrationBuilder.DropTable(
                name: "TerminationReasons");

            migrationBuilder.DropTable(
                name: "TerminationRequestDetails");

            migrationBuilder.DropTable(
                name: "UserCompanyAccess");

            migrationBuilder.DropTable(
                name: "VehicleDetails");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "ITDetails");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "HRRequestDetails");

            migrationBuilder.DropTable(
                name: "HRRequests");

            migrationBuilder.DropTable(
                name: "RequestStatuses");

            migrationBuilder.DropTable(
                name: "RequestTypes");
        }
    }
}
