using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.Entities;

namespace Mathy.ELM.Infrastructure.Data;

public class MathyELMContext : DbContext
{
    public MathyELMContext(DbContextOptions<MathyELMContext> options) : base(options)
    {
    }

    // Core Request Tables
    public DbSet<HRRequest> HRRequests { get; set; }
    public DbSet<HRRequestDetail> HRRequestDetails { get; set; }
    public DbSet<RequestType> RequestTypes { get; set; }
    public DbSet<RequestStatus> RequestStatuses { get; set; }

    // Request Type Specific Tables
    public DbSet<PromotionRequestDetail> PromotionRequestDetails { get; set; }
    public DbSet<LayoffRequestDetail> LayoffRequestDetails { get; set; }
    public DbSet<TerminationRequestDetail> TerminationRequestDetails { get; set; }
    public DbSet<ReturnToWorkRequestDetail> ReturnToWorkRequestDetails { get; set; }
    public DbSet<NewHireRequestDetail> NewHireRequestDetails { get; set; }

    // Access Features Tables
    public DbSet<ITDetail> ITDetails { get; set; }
    public DbSet<CreditCardDetail> CreditCardDetails { get; set; }
    public DbSet<VehicleDetail> VehicleDetails { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<ApplicationRequest> ApplicationRequests { get; set; }
    public DbSet<FolderRequest> FolderRequests { get; set; }
    public DbSet<ITPhoneRequirement> ITPhoneRequirements { get; set; }
    public DbSet<ITTabletProfile> ITTabletProfiles { get; set; }
    public DbSet<ITComputerRequirement> ITComputerRequirements { get; set; }

    // Promotion Request Detail Tables
    public DbSet<PTCreditCardDetail> PTCreditCardDetails { get; set; }
    public DbSet<PTVehicleDetail> PTVehicleDetails { get; set; }
    public DbSet<PTITDetail> PTITDetails { get; set; }
    public DbSet<PTApplicationRequest> PTApplicationRequests { get; set; }
    public DbSet<PTFolderRequest> PTFolderRequests { get; set; }
    public DbSet<PTITPhoneRequirement> PTITPhoneRequirements { get; set; }
    public DbSet<PTITTabletProfile> PTITTabletProfiles { get; set; }
    public DbSet<PTITComputerRequirement> PTITComputerRequirements { get; set; }
    public DbSet<PTBuildingAccessRequirement> PTBuildingAccessRequirements { get; set; }

    // Reference Data Tables
    public DbSet<Company> Companies { get; set; }
    public DbSet<PayrollGroup> PayrollGroups { get; set; }
    public DbSet<PayrollDepartment> PayrollDepartments { get; set; }
    public DbSet<PayrollDepartmentShortName> PayrollDepartmentShortNames { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<PhysicalLocation> PhysicalLocations { get; set; }
    public DbSet<FunctionalDepartment> FunctionalDepartments { get; set; }
    public DbSet<UnionCraft> UnionCrafts { get; set; }
    public DbSet<EmployeeLicenseClass> EmployeeLicenseClasses { get; set; }
    public DbSet<EmploymentStatus> EmploymentStatuses { get; set; }
    public DbSet<EmploymentStatusMapper> EmploymentStatusMappers { get; set; }
    public DbSet<EmployeeSalaryType> EmployeeSalaryTypes { get; set; }
    public DbSet<ApprenticePercentage> ApprenticePercentages { get; set; }
    public DbSet<TabletProfile> TabletProfiles { get; set; }
    public DbSet<ComputerRequirement> ComputerRequirements { get; set; }
    public DbSet<BuildingAccessRequirement> BuildingAccessRequirements { get; set; }
    public DbSet<NewHireBuildingAccessRequirement> NewHireBuildingAccessRequirements { get; set; }
    public DbSet<CompanyTypeLocation> CompanyTypeLocations { get; set; }
    public DbSet<CompanyDL> CompanyDLs { get; set; }

    // Employee and User Management Tables
    public DbSet<Employee> Employees { get; set; }
    public DbSet<UserCompanyAccess> UserCompanyAccess { get; set; }
    public DbSet<TerminationReason> TerminationReasons { get; set; }

    // ServiceDesk Integration Tables
    public DbSet<ServiceDeskSyncData> ServiceDeskSyncDatas { get; set; }
    public DbSet<PTServiceDeskSyncData> PTServiceDeskSyncDatas { get; set; }

    // Notification Tables
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<EmailContentMapper> EmailContentMappers { get; set; }
    public DbSet<NotificationQueue> NotificationQueue { get; set; }

    // System Configuration Tables
    public DbSet<SyncScheduleConfig> SyncScheduleConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCoreRequestTables(modelBuilder);
        ConfigureRequestSpecificTables(modelBuilder);
        ConfigureAccessFeaturesTables(modelBuilder);
        ConfigureReferenceDataTables(modelBuilder);
        ConfigureUserManagementTables(modelBuilder);
        ConfigureNotificationTables(modelBuilder);
        ConfigureSystemConfigurationTables(modelBuilder);

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void ConfigureCoreRequestTables(ModelBuilder modelBuilder)
    {
        // HRRequests configuration
        modelBuilder.Entity<HRRequest>(entity =>
        {
            entity.ToTable("HRRequests");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SubmittedBy).IsRequired();
            entity.Property(e => e.SubmittedDate).HasColumnType("datetime2");
            entity.Property(e => e.SubmitterName).HasMaxLength(100);
            entity.Property(e => e.SubmitterEmail).HasMaxLength(255);
            entity.Property(e => e.Notes).HasColumnType("varchar(max)");

            // Indexes
            entity.HasIndex(e => e.SubmittedBy).HasDatabaseName("IX_HRRequests_SubmittedBy");
            entity.HasIndex(e => e.SubmittedDate).HasDatabaseName("IX_HRRequests_SubmittedDate");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_HRRequests_IsDeleted");
        });

        // RequestTypes configuration
        modelBuilder.Entity<RequestType>(entity =>
        {
            entity.ToTable("RequestTypes");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RequestTypeName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.RequestTypeDescription).HasMaxLength(255);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            // Indexes
            entity.HasIndex(e => e.RequestTypeName)
                .IsUnique()
                .HasDatabaseName("IX_RequestTypes_RequestTypeName");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_RequestTypes_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_RequestTypes_IsDeleted");
        });

        // RequestStatuses configuration
        modelBuilder.Entity<RequestStatus>(entity =>
        {
            entity.ToTable("RequestStatuses");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RequestStatusName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.RequestStatusDescription).HasMaxLength(255);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            // Indexes
            entity.HasIndex(e => e.RequestStatusName)
                .IsUnique()
                .HasDatabaseName("IX_RequestStatuses_RequestStatusName");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_RequestStatuses_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_RequestStatuses_IsDeleted");
        });

        // HRRequestDetails configuration
        modelBuilder.Entity<HRRequestDetail>(entity =>
        {
            entity.ToTable("HRRequestDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ParentRequestId).IsRequired();
            entity.Property(e => e.RequestTypeId).IsRequired();
            entity.Property(e => e.RequestStatusId)
                .IsRequired()
                .HasDefaultValue(1);
            
            // Employee Information
            entity.Property(e => e.EmployeeId).IsRequired();
            entity.Property(e => e.EmployeeNetworkId).HasMaxLength(255);
            entity.Property(e => e.EmployeePositionCode).HasMaxLength(10);
            
            // Request Details
            entity.Property(e => e.EffectiveDate).HasColumnType("date");
            entity.Property(e => e.ProcessingNotes).HasColumnType("varchar(max)");
            
            // Viewpoint Integration
            entity.Property(e => e.ViewpointProcessed)
                .IsRequired()
                .HasDefaultValue(false);
            entity.Property(e => e.ViewpointProcessedDate).HasColumnType("datetime2");
            entity.Property(e => e.ViewpointErrorMessage).HasColumnType("varchar(max)");
            
            // Foreign Keys
            entity.HasOne(e => e.ParentRequest)
                .WithMany(r => r.Details)
                .HasForeignKey(e => e.ParentRequestId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.RequestType)
                .WithMany(rt => rt.HRRequestDetails)
                .HasForeignKey(e => e.RequestTypeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.RequestStatus)
                .WithMany(rs => rs.HRRequestDetails)
                .HasForeignKey(e => e.RequestStatusId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.ParentRequestId).HasDatabaseName("IX_HRRequestDetails_ParentRequestId");
            entity.HasIndex(e => e.RequestTypeId).HasDatabaseName("IX_HRRequestDetails_RequestTypeId");
            entity.HasIndex(e => e.RequestStatusId).HasDatabaseName("IX_HRRequestDetails_RequestStatusId");
            entity.HasIndex(e => e.EmployeeId).HasDatabaseName("IX_HRRequestDetails_EmployeeId");
            entity.HasIndex(e => e.EmployeeNetworkId).HasDatabaseName("IX_HRRequestDetails_EmployeeNetworkId");
            entity.HasIndex(e => e.EffectiveDate).HasDatabaseName("IX_HRRequestDetails_EffectiveDate");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_HRRequestDetails_IsDeleted");
        });
    }

    private void ConfigureRequestSpecificTables(ModelBuilder modelBuilder)
    {
        // PromotionRequestDetails configuration
        modelBuilder.Entity<PromotionRequestDetail>(entity =>
        {
            entity.ToTable("PromotionRequestDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RequestDetailId).IsRequired();
            
            // Current Position
            entity.Property(e => e.CurrentPositionCode).HasMaxLength(10);
            entity.Property(e => e.CurrentStatus).HasMaxLength(10);
            
            // New Position
            entity.Property(e => e.NewPayrollCompanyCode).IsRequired();
            entity.Property(e => e.NewPayrollGroupCode).IsRequired();
            entity.Property(e => e.NewPayrollDeptCode).IsRequired();
            entity.Property(e => e.NewPositionCode)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.NewPhysicalLocationCode).IsRequired();
            entity.Property(e => e.NewStatus)
                .IsRequired()
                .HasMaxLength(10);

            // Work Email
            entity.Property(e => e.CurrentWorkEmail).HasMaxLength(255);
            entity.Property(e => e.NewWorkEmail).HasMaxLength(255);

            // Foreign Key to HRRequestDetail
            entity.HasOne(e => e.HRRequestDetail)
                .WithOne(rd => rd.PromotionDetails)
                .HasForeignKey<PromotionRequestDetail>(e => e.RequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // Note: 1:1 and 1:Many relationships with PT* child tables are configured from child side only
            // This prevents shadow FK properties from being created on PromotionRequestDetail

            // Indexes
            entity.HasIndex(e => e.RequestDetailId).HasDatabaseName("IX_PromotionRequestDetails_RequestDetailId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PromotionRequestDetails_IsDeleted");
        });

        // LayoffRequestDetails configuration
        modelBuilder.Entity<LayoffRequestDetail>(entity =>
        {
            entity.ToTable("LayoffRequestDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RequestDetailId).IsRequired();
            entity.Property(e => e.LastDayWorked)
                .IsRequired()
                .HasColumnType("date");
            
            // Foreign Key
            entity.HasOne(e => e.HRRequestDetail)
                .WithOne(rd => rd.LayoffDetails)
                .HasForeignKey<LayoffRequestDetail>(e => e.RequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.RequestDetailId).HasDatabaseName("IX_LayoffRequestDetails_RequestDetailId");
            entity.HasIndex(e => e.LastDayWorked).HasDatabaseName("IX_LayoffRequestDetails_LastDayWorked");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_LayoffRequestDetails_IsDeleted");
        });

        // TerminationRequestDetails configuration
        modelBuilder.Entity<TerminationRequestDetail>(entity =>
        {
            entity.ToTable("TerminationRequestDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RequestDetailId).IsRequired();
            entity.Property(e => e.ReasonCode)
                .IsRequired()
                .HasMaxLength(20);
            
            // Communication Forwarding
            entity.Property(e => e.ForwardEmail).HasMaxLength(255);
            entity.Property(e => e.ForwardDeskPhone).HasMaxLength(50);
            entity.Property(e => e.ForwardCellPhone).HasMaxLength(50);
            entity.Property(e => e.AutoReply).HasColumnType("varchar(max)");

            // Kwik Trip Card
            entity.Property(e => e.WithKwikTripCard).HasDefaultValue(false);
            entity.Property(e => e.KwikCard4DigitNo).HasMaxLength(4);

            // Foreign Key
            entity.HasOne(e => e.HRRequestDetail)
                .WithOne(rd => rd.TerminationDetails)
                .HasForeignKey<TerminationRequestDetail>(e => e.RequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.RequestDetailId).HasDatabaseName("IX_TerminationRequestDetails_RequestDetailId");
            entity.HasIndex(e => e.ReasonCode).HasDatabaseName("IX_TerminationRequestDetails_Reason");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_TerminationRequestDetails_IsDeleted");
        });

        // ReturnToWorkRequestDetails configuration
        modelBuilder.Entity<ReturnToWorkRequestDetail>(entity =>
        {
            entity.ToTable("ReturnToWorkRequestDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RequestDetailId).IsRequired();
            
            // Foreign Key
            entity.HasOne(e => e.HRRequestDetail)
                .WithOne(rd => rd.ReturnToWorkDetails)
                .HasForeignKey<ReturnToWorkRequestDetail>(e => e.RequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.RequestDetailId).HasDatabaseName("IX_ReturnToWorkRequestDetails_RequestDetailId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ReturnToWorkRequestDetails_IsDeleted");
        });

        // NewHireRequestDetails configuration
        modelBuilder.Entity<NewHireRequestDetail>(entity =>
        {
            entity.ToTable("NewHireRequestDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RequestDetailId).IsRequired();
            
            // Personal Information
            entity.Property(e => e.EmployeeId);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50); // Removed .IsRequired() to match Arnie's nullable schema
            entity.Property(e => e.LastName)
                .HasMaxLength(50); // Removed .IsRequired() to match Arnie's nullable schema
            entity.Property(e => e.MiddleInitial).HasMaxLength(3);
            entity.Property(e => e.Suffix).HasMaxLength(10);
            entity.Property(e => e.PreferredFirstName).HasMaxLength(50);
            entity.Property(e => e.FirstDayEmployment)
                .HasColumnType("date"); // Removed .IsRequired() to match Arnie's nullable schema
            entity.Property(e => e.ReferredBy).HasMaxLength(100);
            entity.Property(e => e.Rehire)
                .HasDefaultValue(false); // Removed .IsRequired() to match Arnie's nullable schema

            // Position Information
            // Removed .IsRequired() to match Arnie's nullable schema
            entity.Property(e => e.CompanyCode);
            entity.Property(e => e.LocationCode);
            entity.Property(e => e.EmploymentStatus)
                .HasMaxLength(20); // Removed .IsRequired() to match Arnie's nullable schema
            entity.Property(e => e.SalaryCode);
            entity.Property(e => e.PositionCode)
                .HasMaxLength(10); // Removed .IsRequired() to match Arnie's nullable schema
            entity.Property(e => e.NetworkId)
                .HasMaxLength(255);
            entity.Property(e => e.WorkEmail)
                .HasMaxLength(255);
            entity.Property(e => e.AdPassword)
                .HasMaxLength(1000);

            // Foreign Key
            entity.HasOne(e => e.HRRequestDetail)
                .WithOne(rd => rd.NewHireDetails)
                .HasForeignKey<NewHireRequestDetail>(e => e.RequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.RequestDetailId).HasDatabaseName("IX_NewHireRequestDetails_RequestDetailId");
            entity.HasIndex(e => e.FirstDayEmployment).HasDatabaseName("IX_NewHireRequestDetails_FirstDayEmployment");
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_NewHireRequestDetails_CompanyCode");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_NewHireRequestDetails_IsDeleted");
        });
    }

    private void ConfigureAccessFeaturesTables(ModelBuilder modelBuilder)
    {
        // CreditCardDetails configuration
        modelBuilder.Entity<CreditCardDetail>(entity =>
        {
            entity.ToTable("CreditCardDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.CreditExpenseType).HasMaxLength(50);
            entity.Property(e => e.WeeklyLimit).HasColumnType("decimal(10,2)");
            entity.Property(e => e.FuelCardlockAddress).HasMaxLength(500);

            // Foreign Key
            entity.HasOne(e => e.NewHireRequest)
                .WithOne(nh => nh.CreditCardDetail)
                .HasForeignKey<CreditCardDetail>(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_CreditCardDetails_NewHireRequestId");
            entity.HasIndex(e => e.CompanyExpenseCard).HasDatabaseName("IX_CreditCardDetails_CompanyExpenseCard");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_CreditCardDetails_IsDeleted");
        });

        // VehicleDetails configuration
        modelBuilder.Entity<VehicleDetail>(entity =>
        {
            entity.ToTable("VehicleDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.DriverClassification).HasMaxLength(100);
            entity.Property(e => e.DrugAndAlcoholProfile).HasMaxLength(30);
            
            // Foreign Key
            entity.HasOne(e => e.NewHireRequest)
                .WithOne(nh => nh.VehicleDetail)
                .HasForeignKey<VehicleDetail>(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_VehicleDetails_NewHireRequestId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_VehicleDetails_IsDeleted");
        });

        // ITDetails configuration
        modelBuilder.Entity<ITDetail>(entity =>
        {
            entity.ToTable("ITDetails");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.AlternateDeliveryLocation).HasMaxLength(500);
            
            // Foreign Key
            entity.HasOne(e => e.NewHireRequest)
                .WithOne(nh => nh.ITDetail)
                .HasForeignKey<ITDetail>(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_ITDetails_NewHireRequestId");
            entity.HasIndex(e => e.EmailRequired).HasDatabaseName("IX_ITDetails_EmailRequired");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ITDetails_IsDeleted");
        });

        // Applications configuration
        modelBuilder.Entity<Application>(entity =>
        {
            entity.ToTable("Applications");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.LocationType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            // Indexes
            entity.HasIndex(e => e.LocationType).HasDatabaseName("IX_Applications_LocationType");
            entity.HasIndex(e => e.Name).HasDatabaseName("IX_Applications_Name");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Applications_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_Applications_IsDeleted");
        });

        // ApplicationRequests configuration
        modelBuilder.Entity<ApplicationRequest>(entity =>
        {
            entity.ToTable("ApplicationRequests");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.ApplicationId).IsRequired();
            entity.Property(e => e.AccessNotes).HasMaxLength(500);
            
            // Foreign Keys
            entity.HasOne(e => e.NewHireRequest)
                .WithMany(nh => nh.ApplicationRequests)
                .HasForeignKey(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Application)
                .WithMany(a => a.ApplicationRequests)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_ApplicationRequests_NewHireRequestId");
            entity.HasIndex(e => e.ApplicationId).HasDatabaseName("IX_ApplicationRequests_ApplicationId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ApplicationRequests_IsDeleted");
        });

        // FolderRequests configuration
        modelBuilder.Entity<FolderRequest>(entity =>
        {
            entity.ToTable("FolderRequests");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.FolderType)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.FolderName)
                .IsRequired()
                .HasMaxLength(500);
            
            // Foreign Key
            entity.HasOne(e => e.NewHireRequest)
                .WithMany(nh => nh.FolderRequests)
                .HasForeignKey(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_FolderRequests_NewHireRequestId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_FolderRequests_IsDeleted");
        });

        // ITPhoneRequirements configuration
        modelBuilder.Entity<ITPhoneRequirement>(entity =>
        {
            entity.ToTable("ITPhoneRequirements");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();

            // Foreign Key
            entity.HasOne(e => e.NewHireRequest)
                .WithOne(nh => nh.ITPhoneRequirement)
                .HasForeignKey<ITPhoneRequirement>(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_ITPhoneRequirements_NewHireRequestId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ITPhoneRequirements_IsDeleted");
        });

        // ITTabletProfiles configuration
        modelBuilder.Entity<ITTabletProfile>(entity =>
        {
            entity.ToTable("ITTabletProfiles");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.TabletProfileId).IsRequired();
            entity.Property(e => e.TabletProfileName).HasMaxLength(255);
            entity.Property(e => e.RolesRequiredForNewHire)
                .IsRequired()
                .HasMaxLength(1000);
            
            // Foreign Keys
            entity.HasOne(e => e.NewHireRequest)
                .WithMany(nh => nh.ITTabletProfiles)
                .HasForeignKey(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.TabletProfile)
                .WithMany(tp => tp.ITTabletProfiles)
                .HasForeignKey(e => e.TabletProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_ITTabletProfiles_NewHireRequestId");
            entity.HasIndex(e => e.TabletProfileId).HasDatabaseName("IX_ITTabletProfiles_TabletProfileId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ITTabletProfiles_IsDeleted");
        });

        // ITComputerRequirements configuration
        modelBuilder.Entity<ITComputerRequirement>(entity =>
        {
            entity.ToTable("ITComputerRequirements");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.ComputerRequirementsId).IsRequired();
            entity.Property(e => e.ComputerRequirementsDescription).HasMaxLength(255);
            entity.Property(e => e.IsChild).HasDefaultValue(false);
            entity.Property(e => e.ParentId);
            
            // Foreign Keys
            entity.HasOne(e => e.NewHireRequest)
                .WithMany(nh => nh.ITComputerRequirements)
                .HasForeignKey(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ComputerRequirement)
                .WithMany(cr => cr.ITComputerRequirements)
                .HasForeignKey(e => e.ComputerRequirementsId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_ITComputerRequirements_NewHireRequestId");
            entity.HasIndex(e => e.ComputerRequirementsId).HasDatabaseName("IX_ITComputerRequirements_ComputerRequirementsId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ITComputerRequirements_IsDeleted");
        });

        // PTCreditCardDetails configuration
        modelBuilder.Entity<PTCreditCardDetail>(entity =>
        {
            entity.ToTable("PTCreditCardDetails");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.WeeklyLimit).HasColumnType("decimal(10,2)");
            entity.Property(e => e.FuelCardlockAddress).HasMaxLength(500);

            // Foreign Key
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithOne(p => p.PTCreditCardDetail)
                .HasForeignKey<PTCreditCardDetail>(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTCreditCardDetails_PTRequestDetailId");
            entity.HasIndex(e => e.CompanyExpenseCard).HasDatabaseName("IX_PTCreditCardDetails_CompanyExpenseCard");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTCreditCardDetails_IsDeleted");
        });

        // PTVehicleDetails configuration
        modelBuilder.Entity<PTVehicleDetail>(entity =>
        {
            entity.ToTable("PTVehicleDetails");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.LicenseClass).HasMaxLength(10);
            entity.Property(e => e.DrugAndAlcoholProfile).HasMaxLength(30);

            // Foreign Key
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithOne(p => p.PTVehicleDetail)
                .HasForeignKey<PTVehicleDetail>(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTVehicleDetails_PTRequestDetailId");
            entity.HasIndex(e => e.LicenseClass).HasDatabaseName("IX_PTVehicleDetails_LicenseClass");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTVehicleDetails_IsDeleted");
        });

        // PTITDetails configuration
        modelBuilder.Entity<PTITDetail>(entity =>
        {
            entity.ToTable("PTITDetails");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.AlternateDeliveryLocation).HasMaxLength(500);

            // Foreign Key
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithOne(p => p.PTITDetail)
                .HasForeignKey<PTITDetail>(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTITDetails_PTRequestDetailId");
            entity.HasIndex(e => e.EmailRequired).HasDatabaseName("IX_PTITDetails_EmailRequired");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTITDetails_IsDeleted");
        });

        // PTApplicationRequests configuration
        modelBuilder.Entity<PTApplicationRequest>(entity =>
        {
            entity.ToTable("PTApplicationRequests");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.ApplicationId).IsRequired();
            entity.Property(e => e.AccessNotes).HasMaxLength(500);

            // Foreign Keys
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithMany(p => p.PTApplicationRequests)
                .HasForeignKey(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Application)
                .WithMany()
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTApplicationRequests_PTRequestDetailId");
            entity.HasIndex(e => e.ApplicationId).HasDatabaseName("IX_PTApplicationRequests_ApplicationId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTApplicationRequests_IsDeleted");
        });

        // PTFolderRequests configuration
        modelBuilder.Entity<PTFolderRequest>(entity =>
        {
            entity.ToTable("PTFolderRequests");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.FolderType)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.FolderName)
                .IsRequired()
                .HasMaxLength(500);

            // Foreign Key
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithMany(p => p.PTFolderRequests)
                .HasForeignKey(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTFolderRequests_PTRequestDetailId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTFolderRequests_IsDeleted");
        });

        // PTITPhoneRequirements configuration
        modelBuilder.Entity<PTITPhoneRequirement>(entity =>
        {
            entity.ToTable("PTITPhoneRequirements");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.WorkPhoneNumber).HasMaxLength(50);
            entity.Property(e => e.WorkExtension).HasMaxLength(50);
            entity.Property(e => e.WorkCell).HasMaxLength(50);

            // Foreign Key
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithOne(p => p.PTITPhoneRequirement)
                .HasForeignKey<PTITPhoneRequirement>(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTITPhoneRequirements_PTRequestDetailId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTITPhoneRequirements_IsDeleted");
        });

        // PTITTabletProfiles configuration
        modelBuilder.Entity<PTITTabletProfile>(entity =>
        {
            entity.ToTable("PTITTabletProfiles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.TabletProfileId).IsRequired();
            entity.Property(e => e.TabletProfileName).HasMaxLength(255);
            entity.Property(e => e.RolesRequiredForNewHire)
                .IsRequired()
                .HasMaxLength(1000);

            // Foreign Keys
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithMany(p => p.PTITTabletProfiles)
                .HasForeignKey(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TabletProfile)
                .WithMany()
                .HasForeignKey(e => e.TabletProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTITTabletProfiles_PTRequestDetailId");
            entity.HasIndex(e => e.TabletProfileId).HasDatabaseName("IX_PTITTabletProfiles_TabletProfileId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTITTabletProfiles_IsDeleted");
        });

        // PTITComputerRequirements configuration
        modelBuilder.Entity<PTITComputerRequirement>(entity =>
        {
            entity.ToTable("PTITComputerRequirements");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.ComputerRequirementsId).IsRequired();
            entity.Property(e => e.ComputerRequirementsDescription).HasMaxLength(255);
            entity.Property(e => e.IsChild).HasDefaultValue(false);
            entity.Property(e => e.ParentId);

            // Foreign Keys
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithMany(p => p.PTITComputerRequirements)
                .HasForeignKey(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ComputerRequirement)
                .WithMany()
                .HasForeignKey(e => e.ComputerRequirementsId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTITComputerRequirements_PTRequestDetailId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTITComputerRequirements_IsDeleted");
        });

        // PTBuildingAccessRequirements configuration
        modelBuilder.Entity<PTBuildingAccessRequirement>(entity =>
        {
            entity.ToTable("PTBuildingAccessRequirements");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.AccessId).IsRequired();
            entity.Property(e => e.AccessDescription).HasMaxLength(255).IsRequired();

            // Foreign Keys
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithMany(p => p.PTBuildingAccessRequirements)
                .HasForeignKey(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.BuildingAccessRequirement)
                .WithMany()
                .HasForeignKey(e => e.AccessId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTBuildingAccessRequirements_PTRequestDetailId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTBuildingAccessRequirements_IsDeleted");
        });

        // ServiceDeskSyncData configuration
        modelBuilder.Entity<ServiceDeskSyncData>(entity =>
        {
            entity.ToTable("ServiceDeskSyncData");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.ServiceDeskID)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.HasBuildingAccess).HasDefaultValue(false);
            entity.Property(e => e.HasPhoneRequirements).HasDefaultValue(false);
            entity.Property(e => e.HasComputerRequirements).HasDefaultValue(false);
            entity.Property(e => e.HasTabletProfiles).HasDefaultValue(false);
            entity.Property(e => e.HasITApplications).HasDefaultValue(false);
            entity.Property(e => e.HasSoftwareAccessReq).HasDefaultValue(false);

            // Foreign Key
            entity.HasOne(e => e.NewHireRequestDetail)
                .WithOne(nh => nh.ServiceDeskSyncData)
                .HasForeignKey<ServiceDeskSyncData>(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_ServiceDeskSyncData_NewHireRequestId");
            entity.HasIndex(e => e.ServiceDeskID).HasDatabaseName("IX_ServiceDeskSyncData_ServiceDeskID");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ServiceDeskSyncData_IsDeleted");
        });

        // PTServiceDeskSyncData configuration
        modelBuilder.Entity<PTServiceDeskSyncData>(entity =>
        {
            entity.ToTable("PTServiceDeskSyncData");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PTRequestDetailId).IsRequired();
            entity.Property(e => e.ServiceDeskID)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.HasBuildingAccess).HasDefaultValue(false);
            entity.Property(e => e.HasPhoneRequirements).HasDefaultValue(false);
            entity.Property(e => e.HasComputerRequirements).HasDefaultValue(false);
            entity.Property(e => e.HasTabletProfiles).HasDefaultValue(false);
            entity.Property(e => e.HasITApplications).HasDefaultValue(false);
            entity.Property(e => e.HasSoftwareAccessReq).HasDefaultValue(false);

            // Foreign Key
            entity.HasOne(e => e.PromotionRequestDetail)
                .WithOne(p => p.PTServiceDeskSyncData)
                .HasForeignKey<PTServiceDeskSyncData>(e => e.PTRequestDetailId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            entity.HasIndex(e => e.PTRequestDetailId).HasDatabaseName("IX_PTServiceDeskSyncData_PTRequestDetailId");
            entity.HasIndex(e => e.ServiceDeskID).HasDatabaseName("IX_PTServiceDeskSyncData_ServiceDeskID");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PTServiceDeskSyncData_IsDeleted");
        });
    }

    private void ConfigureReferenceDataTables(ModelBuilder modelBuilder)
    {
        // Companies configuration
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.CompanyName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => e.CompanyCode)
                .IsUnique()
                .HasDatabaseName("IX_Companies_CompanyCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Companies_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_Companies_IsDeleted");
        });

        // PayrollGroups configuration
        modelBuilder.Entity<PayrollGroup>(entity =>
        {
            entity.ToTable("PayrollGroups");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.GroupCode).IsRequired();
            entity.Property(e => e.GroupName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => new { e.CompanyCode, e.GroupCode })
                .IsUnique()
                .HasDatabaseName("IX_PayrollGroups_CompanyCode_GroupCode");
            entity.HasIndex(e => e.GroupCode).HasDatabaseName("IX_PayrollGroups_GroupCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_PayrollGroups_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PayrollGroups_IsDeleted");
        });

        // PayrollDepartments configuration
        modelBuilder.Entity<PayrollDepartment>(entity =>
        {
            entity.ToTable("PayrollDepartments");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.DeptCode).IsRequired();
            entity.Property(e => e.DeptName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");

            // Representative Fields
            entity.Property(e => e.HRPartner);
            entity.Property(e => e.HRRep);
            entity.Property(e => e.SafetyRep);
            entity.Property(e => e.PayrollRep);

            // Indexes
            entity.HasIndex(e => new { e.CompanyCode, e.DeptCode })
                .IsUnique()
                .HasDatabaseName("IX_PayrollDepartments_CompanyCode_DeptCode");
            entity.HasIndex(e => e.DeptCode).HasDatabaseName("IX_PayrollDepartments_DeptCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_PayrollDepartments_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PayrollDepartments_IsDeleted");
        });

        // PayrollDepartmentShortNames configuration
        modelBuilder.Entity<PayrollDepartmentShortName>(entity =>
        {
            entity.ToTable("PayrollDepartmentShortNames");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.DeptCode).IsRequired();
            entity.Property(e => e.DeptShortName)
                .IsRequired()
                .HasMaxLength(25);
            
            // Indexes
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_PayrollDepartmentShortNames_CompanyCode");
            entity.HasIndex(e => e.DeptCode).HasDatabaseName("IX_PayrollDepartmentShortNames_DeptCode");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PayrollDepartmentShortNames_IsDeleted");
            entity.HasIndex(e => new { e.CompanyCode, e.DeptCode })
                .IsUnique()
                .HasDatabaseName("IX_PayrollDepartmentShortNames_CompanyCode_DeptCode");
        });

        // Positions configuration
        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable("Positions");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.PositionCode)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.PositionName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Type)
                .HasColumnType("char(1)");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => new { e.CompanyCode, e.PositionCode })
                .IsUnique()
                .HasDatabaseName("IX_Positions_CompanyCode_PositionCode");
            entity.HasIndex(e => e.PositionCode).HasDatabaseName("IX_Positions_PositionCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Positions_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_Positions_IsDeleted");
        });

        // PhysicalLocations configuration
        modelBuilder.Entity<PhysicalLocation>(entity =>
        {
            entity.ToTable("PhysicalLocations");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.LocationCode).IsRequired();
            entity.Property(e => e.LocationName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => e.LocationCode)
                .IsUnique()
                .HasDatabaseName("IX_PhysicalLocations_LocationCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_PhysicalLocations_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_PhysicalLocations_IsDeleted");
        });

        // FunctionalDepartments configuration
        modelBuilder.Entity<FunctionalDepartment>(entity =>
        {
            entity.ToTable("FunctionalDepartments");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FunctionalDeptCode).IsRequired();
            entity.Property(e => e.FunctionalDeptName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => e.FunctionalDeptCode)
                .IsUnique()
                .HasDatabaseName("IX_FunctionalDepartments_FunctionalDeptCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_FunctionalDepartment_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_FunctionalDepartment_IsDeleted");
        });

        // UnionCrafts configuration
        modelBuilder.Entity<UnionCraft>(entity =>
        {
            entity.ToTable("UnionCrafts");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.CraftCode)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(30);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_UnionCrafts_CompanyCode");
            entity.HasIndex(e => e.CraftCode).HasDatabaseName("IX_UnionCrafts_CraftCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_UnionCrafts_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_UnionCrafts_IsDeleted");
        });

        // EmployeeLicenseClasses configuration
        modelBuilder.Entity<EmployeeLicenseClass>(entity =>
        {
            entity.ToTable("EmployeeLicenseClasses");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.LicenseClass)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(70);
            entity.Property(e => e.IsUnion)
                .IsRequired()
                .HasDefaultValue(false);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => e.LicenseClass).HasDatabaseName("IX_EmployeeLicenseClasses_LicenseClass");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_EmployeeLicenseClasses_IsDeleted");
        });

        // EmploymentStatuses configuration
        modelBuilder.Entity<EmploymentStatus>(entity =>
        {
            entity.ToTable("EmploymentStatuses");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Notes)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.CodeType)
                .HasMaxLength(1)
                .IsFixedLength(false);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_EmploymentStatuses_CompanyCode");
            entity.HasIndex(e => e.Notes).HasDatabaseName("IX_EmploymentStatuses_Notes");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_EmploymentStatuses_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_EmploymentStatuses_IsDeleted");
        });

        // EmploymentStatusMapper configuration
        modelBuilder.Entity<EmploymentStatusMapper>(entity =>
        {
            entity.ToTable("EmploymentStatusMapper");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ActiveStatus)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.LayOffStatus)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.ReturnToWorkStatus)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.TerminationStatus)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.IsUnion)
                .IsRequired()
                .HasDefaultValue(false);
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes
            entity.HasIndex(e => e.ActiveStatus).HasDatabaseName("IX_EmploymentStatusMapper_ActiveStatus");
            entity.HasIndex(e => e.IsUnion).HasDatabaseName("IX_EmploymentStatusMapper_IsUnion");
        });

        // EmployeeSalaryTypes configuration
        modelBuilder.Entity<EmployeeSalaryType>(entity =>
        {
            entity.ToTable("EmployeeSalaryTypes");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.SalaryCode)
                .IsRequired(false);
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_EmployeeSalaryTypes_CompanyCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_EmployeeSalaryTypes_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_EmployeeSalaryTypes_IsDeleted");
        });

        // ApprenticePercentages configuration
        modelBuilder.Entity<ApprenticePercentage>(entity =>
        {
            entity.ToTable("ApprenticePercentages");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AppPercentage)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.AppDescription)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            // Indexes
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ApprenticePercentages_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ApprenticePercentages_IsDeleted");
        });

        // TabletProfiles configuration
        modelBuilder.Entity<TabletProfile>(entity =>
        {
            entity.ToTable("TabletProfiles");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.LocationType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.ProfileName)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            // Indexes
            entity.HasIndex(e => e.LocationType).HasDatabaseName("IX_TabletProfiles_LocationType");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_TabletProfiles_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_TabletProfiles_IsDeleted");
        });

        // ComputerRequirements configuration
        modelBuilder.Entity<ComputerRequirement>(entity =>
        {
            entity.ToTable("ComputerRequirements");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            // Indexes
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ComputerRequirements_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_ComputerRequirements_IsDeleted");
        });

        // BuildingAccessRequirements configuration
        modelBuilder.Entity<BuildingAccessRequirement>(entity =>
        {
            entity.ToTable("BuildingAccessRequirements");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.LocationType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            // Indexes
            entity.HasIndex(e => e.LocationType).HasDatabaseName("IX_BuildingAccessRequirements_LocationType");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_BuildingAccessRequirements_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_BuildingAccessRequirements_IsDeleted");
        });

        // NewHireBuildingAccessRequirements configuration
        modelBuilder.Entity<NewHireBuildingAccessRequirement>(entity =>
        {
            entity.ToTable("NewHireBuildingAccessRequirements");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.NewHireRequestId).IsRequired();
            entity.Property(e => e.AccessId).IsRequired();
            entity.Property(e => e.AccessDescription)
                .IsRequired()
                .HasMaxLength(100);
            
            // Foreign Keys
            entity.HasOne(e => e.NewHireRequest)
                .WithMany(nh => nh.BuildingAccessRequirements)
                .HasForeignKey(e => e.NewHireRequestId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.BuildingAccessRequirement)
                .WithMany(bar => bar.NewHireBuildingAccessRequirements)
                .HasForeignKey(e => e.AccessId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.NewHireRequestId).HasDatabaseName("IX_NewHireBuildingAccessRequirements_NewHireRequestId");
            entity.HasIndex(e => e.AccessId).HasDatabaseName("IX_NewHireBuildingAccessRequirements_AccessId");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_NewHireBuildingAccessRequirements_IsDeleted");
        });

        // CompanyTypeLocation configuration
        modelBuilder.Entity<CompanyTypeLocation>(entity =>
        {
            entity.ToTable("CompanyTypeLocation");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.LocationType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.IsUnion)
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_CompanyTypeLocation_CompanyCode");
            entity.HasIndex(e => e.LocationType).HasDatabaseName("IX_CompanyTypeLocation_LocationType");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_CompanyTypeLocation_IsDeleted");
        });

        // CompanyDL configuration
        modelBuilder.Entity<CompanyDL>(entity =>
        {
            entity.ToTable("CompanyDL");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.SiteDL)
                .HasMaxLength(255);
            entity.Property(e => e.SecurityDL)
                .HasMaxLength(255);
            entity.Property(e => e.CreditCardDL)
                .HasMaxLength(255);
            entity.Property(e => e.FleetDL)
                .HasMaxLength(255);
            entity.Property(e => e.ComplianceDL)
                .HasMaxLength(255);
            entity.Property(e => e.SafetyDL)
                .HasMaxLength(255);
            entity.Property(e => e.FuelFobDL)
                .HasMaxLength(255);
            entity.Property(e => e.HRDL)
                .HasMaxLength(255);
            entity.Property(e => e.ITDL)
                .HasMaxLength(255);
            entity.Property(e => e.PAYROLLDL)
                .HasMaxLength(255);

            // Indexes
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_CompanyDL_CompanyCode");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_CompanyDL_IsDeleted");
        });
    }

    private void ConfigureUserManagementTables(ModelBuilder modelBuilder)
    {
        // Employees configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.EmployeeNumber).IsRequired();
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(30);
            entity.Property(e => e.MiddleName).HasMaxLength(15);
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(30);
            entity.Property(e => e.PersonalEmail).HasMaxLength(255);
            entity.Property(e => e.WorkEmail).HasMaxLength(255);
            entity.Property(e => e.NetworkId).HasMaxLength(255);
            entity.Property(e => e.PositionCode).HasMaxLength(10);
            entity.Property(e => e.SupervisorId);
            entity.Property(e => e.TerminationDate).HasColumnType("datetime2");
            entity.Property(e => e.TerminationReasonCode).HasMaxLength(20);
            entity.Property(e => e.ReturnToWorkDate).HasColumnType("datetime2");
            entity.Property(e => e.EmploymentStatus).HasMaxLength(10);
            entity.Property(e => e.WorkPhoneNumber).HasMaxLength(50);
            entity.Property(e => e.WorkExtension).HasMaxLength(50);
            entity.Property(e => e.WorkCell).HasMaxLength(50);
            entity.Property(e => e.ViewpointSyncDate).HasColumnType("datetime2");

            // Indexes
            entity.HasIndex(e => new { e.CompanyCode, e.EmployeeNumber })
                .IsUnique()
                .HasDatabaseName("IX_Employees_CompanyCode_EmployeeNumber");
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_Employees_CompanyCode");
            entity.HasIndex(e => e.EmployeeNumber).HasDatabaseName("IX_Employees_EmployeeNumber");
            entity.HasIndex(e => e.FirstName).HasDatabaseName("IX_Employees_FirstName");
            entity.HasIndex(e => e.LastName).HasDatabaseName("IX_Employees_LastName");
            entity.HasIndex(e => e.NetworkId).HasDatabaseName("IX_Employees_NetworkId");
            entity.HasIndex(e => e.SupervisorId).HasDatabaseName("IX_Employees_SupervisorId");
            entity.HasIndex(e => e.EmploymentStatus).HasDatabaseName("IX_Employees_EmploymentStatus");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_Employees_IsDeleted");
            
            // Global filter for soft delete only
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // UserCompanyAccess configuration
        modelBuilder.Entity<UserCompanyAccess>(entity =>
        {
            entity.ToTable("UserCompanyAccess");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.CompanyCode).IsRequired();
            entity.Property(e => e.CanSubmitRequests)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.LastSyncDate).HasColumnType("datetime2");
            
            // Indexes
            entity.HasIndex(e => new { e.UserId, e.CompanyCode })
                .IsUnique()
                .HasDatabaseName("IX_UserCompanyAccess_UserId_CompanyCode");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_UserCompanyAccess_UserId");
            entity.HasIndex(e => e.CompanyCode).HasDatabaseName("IX_UserCompanyAccess_CompanyCode");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_UserCompanyAccess_IsDeleted");
        });

        // TerminationReasons configuration
        modelBuilder.Entity<TerminationReason>(entity =>
        {
            entity.ToTable("TerminationReasons");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ReasonCode)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.ReasonDescription)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            // Indexes
            entity.HasIndex(e => e.ReasonCode)
                .HasDatabaseName("IX_TerminationReasons_ReasonCode");
            entity.HasIndex(e => new { e.CompanyCode, e.ReasonCode })
                .HasDatabaseName("IX_TerminationReasons_CompanyCode_ReasonCode");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_TerminationReasons_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_TerminationReasons_IsDeleted");
        });
    }

    private void ConfigureNotificationTables(ModelBuilder modelBuilder)
    {
        // EmailTemplates configuration
        modelBuilder.Entity<EmailTemplate>(entity =>
        {
            entity.ToTable("EmailTemplates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TemplateName)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.RequestType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.EmailType)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Recipients)
                .IsRequired()
                .HasMaxLength(1000);
            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.Body)
                .IsRequired()
                .HasColumnType("varchar(max)");
            entity.Property(e => e.TriggerType)
                .IsRequired()
                .HasMaxLength(10);
            entity.Property(e => e.SubmissionFreq)
                .HasDefaultValue(0);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Indexes
            entity.HasIndex(e => e.RequestType).HasDatabaseName("IX_EmailTemplates_RequestType");
            entity.HasIndex(e => e.EmailType).HasDatabaseName("IX_EmailTemplates_EmailType");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_EmailTemplates_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_EmailTemplates_IsDeleted");
        });

        // NotificationQueue configuration
        modelBuilder.Entity<NotificationQueue>(entity =>
        {
            entity.ToTable("NotificationQueue");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RequestId).IsRequired();
            entity.Property(e => e.TemplateId).IsRequired();
            entity.Property(e => e.ToEmail)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.CcEmail).HasMaxLength(500);
            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.Body)
                .IsRequired()
                .HasColumnType("varchar(max)");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.AttemptCount)
                .IsRequired()
                .HasDefaultValue(0);
            entity.Property(e => e.LastAttempt).HasColumnType("datetime2");
            entity.Property(e => e.ErrorMessage).HasColumnType("varchar(max)");
            entity.Property(e => e.SentDate).HasColumnType("datetime2");
            
            // Foreign Keys
            entity.HasOne(e => e.HRRequest)
                .WithMany(r => r.Notifications)
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.EmailTemplate)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => e.RequestId).HasDatabaseName("IX_NotificationQueue_RequestId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_NotificationQueue_Status");
            entity.HasIndex(e => e.CreatedDate).HasDatabaseName("IX_NotificationQueue_CreatedDate");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_NotificationQueue_IsDeleted");
        });
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var systemUserId = 1;

        // Seed RequestTypes
        modelBuilder.Entity<RequestType>().HasData(
            new RequestType { Id = 1, RequestTypeName = "Promotion", RequestTypeDescription = "Employee promotion or transfer request", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new RequestType { Id = 2, RequestTypeName = "Layoff", RequestTypeDescription = "Employee layoff request", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new RequestType { Id = 3, RequestTypeName = "Termination", RequestTypeDescription = "Employee termination request", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new RequestType { Id = 4, RequestTypeName = "ReturnToWork", RequestTypeDescription = "Return to work request for laid-off employees", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new RequestType { Id = 5, RequestTypeName = "NewHire", RequestTypeDescription = "New hire request (future implementation)", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate }
        );

        // Seed RequestStatuses
        modelBuilder.Entity<RequestStatus>().HasData(
            new RequestStatus { Id = 1, RequestStatusName = "Pending", RequestStatusDescription = "Request submitted and awaiting processing", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new RequestStatus { Id = 2, RequestStatusName = "Processing", RequestStatusDescription = "Request is currently being processed", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new RequestStatus { Id = 3, RequestStatusName = "Completed", RequestStatusDescription = "Request has been completed successfully", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new RequestStatus { Id = 4, RequestStatusName = "Failed", RequestStatusDescription = "Request processing failed", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new RequestStatus { Id = 5, RequestStatusName = "Cancelled", RequestStatusDescription = "Request was cancelled", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate }
        );

        // Seed default termination reasons from schema
        var terminationReasons = new[]
        {
            new TerminationReason { Id = 1, ReasonCode = "VT SCHOOL", ReasonDescription = "VT SCHOOL", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 2, ReasonCode = "VT SALARY", ReasonDescription = "VT SALARY", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 3, ReasonCode = "VT RETIRE", ReasonDescription = "VT RETIRE", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 4, ReasonCode = "VT PERSONL", ReasonDescription = "VT PERSONL", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 5, ReasonCode = "VT NOSHOW", ReasonDescription = "VT NOSHOW", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 6, ReasonCode = "VT NOAVAIL", ReasonDescription = "VT NOAVAIL", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 7, ReasonCode = "VT NO WORK", ReasonDescription = "VT NO WORK", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 8, ReasonCode = "VT NO FIT", ReasonDescription = "VT NO FIT", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 9, ReasonCode = "VT MOVE", ReasonDescription = "VT MOVE", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 10, ReasonCode = "VT FAMILY", ReasonDescription = "VT FAMILY", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 11, ReasonCode = "VT EVERIFY", ReasonDescription = "VT EVERIFY", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 12, ReasonCode = "VT DIF JOB", ReasonDescription = "VT DIF JOB", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 13, ReasonCode = "VT DEGREE", ReasonDescription = "VT DEGREE", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 14, ReasonCode = "VOLUNTARY", ReasonDescription = "VOLUNTARY", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 15, ReasonCode = "UR", ReasonDescription = "UR", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 16, ReasonCode = "TRANSFER", ReasonDescription = "TRANSFER", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 17, ReasonCode = "RETIRED", ReasonDescription = "RETIRED", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 18, ReasonCode = "MERIT", ReasonDescription = "MERIT", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 19, ReasonCode = "IT SAFETY", ReasonDescription = "IT SAFETY", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 20, ReasonCode = "IT PERF", ReasonDescription = "IT PERF", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 21, ReasonCode = "IT DA POL", ReasonDescription = "IT DA POL", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 22, ReasonCode = "IT BEHAVR", ReasonDescription = "IT BEHAVR", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 23, ReasonCode = "IT ATTEND", ReasonDescription = "IT ATTEND", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 24, ReasonCode = "INVOLUNTAR", ReasonDescription = "INVOLUNTAR", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 25, ReasonCode = "DISABLED", ReasonDescription = "DISABLED", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate },
            new TerminationReason { Id = 26, ReasonCode = "DECEASED", ReasonDescription = "DECEASED", IsActive = true, CreatedBy = systemUserId, CreatedDate = seedDate }
        };
        modelBuilder.Entity<TerminationReason>().HasData(terminationReasons);

        // Note: EmailTemplate data is managed manually in the database, not seeded here
    }

    private void ConfigureSystemConfigurationTables(ModelBuilder modelBuilder)
    {
        // SyncScheduleConfigs configuration
        modelBuilder.Entity<SyncScheduleConfig>(entity =>
        {
            entity.ToTable("SyncScheduleConfigs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.SyncType)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Schedule)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.CronExpression).HasMaxLength(100);
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.LastExecuted).HasColumnType("datetime2");
            entity.Property(e => e.LastExecutionResult).HasMaxLength(500);
            
            // Indexes
            entity.HasIndex(e => e.SyncType)
                .IsUnique()
                .HasDatabaseName("IX_SyncScheduleConfigs_SyncType");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_SyncScheduleConfigs_IsActive");
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_SyncScheduleConfigs_IsDeleted");
        });
    }

}