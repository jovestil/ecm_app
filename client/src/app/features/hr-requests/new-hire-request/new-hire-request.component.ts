import { Component, OnInit, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { AppHeaderComponent } from '../../../shared/app-header/app-header.component';
import { BackToHomepageButtonComponent } from '../../../shared/back-to-homepage-button/back-to-homepage-button.component';
import { CancelRequestButtonComponent } from '../../../shared/cancel-request-button/cancel-request-button.component';
import { ConfirmationDialogComponent, ConfirmationDialogConfig } from '../../../shared/confirmation-dialog/confirmation-dialog.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { ReferenceDataService, CompanyDto, CompanyTypeLocationDto, PhysicalLocationDto, EmploymentStatusDto, UnionCraftDto, EmployeeSalaryTypeDto, ApprenticePercentageDto, PositionDto, PayrollDepartmentDto, SupervisorDto, BuildingAccessRequirementDto, TabletProfileDto, ApplicationDto, EmployeeLicenseClassDto, ComputerRequirementDto } from '../../../core/services/reference-data.service';
import { SearchableDropdownComponent, SearchableDropdownConfig } from '../../../shared/searchable-dropdown';
import { HRRequestService } from '../../../core/services/hr-request.service';
import { CreateNewHireRequest, NewHireRequestViewDto } from '../../../models/new-hire-request.model';

interface CompanyOption {
  value: string;
  label: string;
}

interface ReferenceDataOptions {
  companies: CompanyOption[];
  physicalLocations: any[];
  employmentStatuses: any[];
  payrollCodes: any[];
  supervisors: any[];
  unionCrafts: any[];
  driverClassifications: any[];
  applications: any[];
}

@Component({
  selector: 'app-new-hire-request',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, AppHeaderComponent, BackToHomepageButtonComponent, CancelRequestButtonComponent, ConfirmationDialogComponent, SearchableDropdownComponent],
  templateUrl: './new-hire-request.component.html',
  styleUrls: ['./new-hire-request.component.css', '../../../shared/styles/common.css']
})
export class NewHireRequestComponent implements OnInit, OnDestroy {
  // State properties for edit mode
  isEditMode: boolean = false;
  isDraftRequest: boolean = false;
  isFormEditable: boolean = false;
  parentId: number | null = null;
  requestDetailId: number | null = null;

  // Cancel request state
  isCancelledRequest: boolean = false;
  requestStatusId: number | null = null;

  // Cancellation banner and dialog
  showCancellationBanner: boolean = false;
  cancellationBannerMessage: string = '';
  showCancelConfirmDialog: boolean = false;
  confirmCancelDialogConfig: ConfirmationDialogConfig = {
    title: 'Cancel New Hire Request',
    message: 'Are you sure you want to cancel this new hire request? A cancellation notification email will be sent immediately. This action cannot be undone.',
    confirmButtonText: 'Yes, Cancel Request',
    cancelButtonText: 'Keep Request',
    confirmButtonClass: 'btn-danger',
    cancelButtonClass: 'btn-secondary',
    showIcon: true,
    iconType: 'warning'
  };
  pendingCancelRequest: NewHireRequestViewDto | null = null;

  // Draft validation state
  private originalValidators = new Map<string, any>();

  // View data for edit mode
  viewData: NewHireRequestViewDto | null = null;
  errorMessage = '';

  // Destroy subject for cleanup
  private destroy$ = new Subject<void>();
  // Subject to cancel building access valueChanges subscription when FormArray is recreated
  private buildingAccessDestroy$ = new Subject<void>();

  newHireForm!: FormGroup;
  activeTab: 'personal' | 'it' = 'personal';
  isLoading = false;
  isDraftSaving = false;
  availableRoles: Array<{value: string, label: string}> = [];
  availableBuildingAccess: string[] = [];
  buildingAccessRequirements: BuildingAccessRequirementDto[] = [];
  tabletProfiles: TabletProfileDto[] = [];

  // Admin role flag
  isEcmAdmin = false;
  isUpdatingPhoneInfo = false;

  // Date constraint for First Day of Employment
  maxEmploymentDate: string = '';

  // Track last warned date to prevent duplicate warnings
  private lastWarnedEmploymentDate: string = '';

  // Computer Requirements data
  computerRequirements: ComputerRequirementDto[] = [];
  parentComputerRequirements: ComputerRequirementDto[] = [];
  childComputerRequirements: Map<number, ComputerRequirementDto[]> = new Map();
  selectedChildRequirements: { [key: string]: boolean } = {};

  // Company dropdown data and configuration
  companies: CompanyDto[] = [];
  companiesForDropdown: Array<CompanyDto & { displayText: string }> = [];
  companyDropdownConfig: SearchableDropdownConfig<CompanyDto & { displayText: string }> = {
    placeholder: 'Type to search companies...',
    displayProperty: 'displayText',
    valueProperty: 'companyCode',
    noResultsText: 'No matching companies found',
    minSearchLength: 1
  };

  // Physical locations dropdown data and configuration
  physicalLocations: PhysicalLocationDto[] = [];
  physicalLocationsForDropdown: Array<PhysicalLocationDto & { displayText: string }> = [];
  physicalLocationDropdownConfig: SearchableDropdownConfig<PhysicalLocationDto & { displayText: string }> = {
    placeholder: 'Type to search physical locations...',
    displayProperty: 'displayText',
    valueProperty: 'locationCode',
    noResultsText: 'No matching physical locations found',
    minSearchLength: 1
  };

  // Employment status dropdown data and configuration
  employmentStatuses: EmploymentStatusDto[] = [];
  employmentStatusesForDropdown: Array<EmploymentStatusDto & { displayText: string }> = [];
  employmentStatusDropdownConfig: SearchableDropdownConfig<EmploymentStatusDto & { displayText: string }> = {
    placeholder: 'Type to search employment statuses...',
    displayProperty: 'displayText',
    valueProperty: 'id',
    noResultsText: 'No matching employment statuses found',
    minSearchLength: 1
  };

  // Applications dropdown data and configuration
  applications: ApplicationDto[] = [];
  applicationsForDropdown: Array<ApplicationDto & { displayText: string }> = [];
  applicationDropdownConfig: SearchableDropdownConfig<ApplicationDto & { displayText: string }> = {
    placeholder: 'Type to search applications...',
    displayProperty: 'displayText',
    valueProperty: 'id',
    noResultsText: 'No matching applications found',
    minSearchLength: 1
  };

  // Union craft dropdown data and configuration
  unionCrafts: UnionCraftDto[] = [];
  unionCraftsForDropdown: Array<UnionCraftDto & { displayText: string }> = [];
  unionCraftDropdownConfig: SearchableDropdownConfig<UnionCraftDto & { displayText: string }> = {
    placeholder: 'Type to search union crafts...',
    displayProperty: 'displayText',
    valueProperty: 'id',
    noResultsText: 'No matching union crafts found',
    minSearchLength: 1
  };

  // Employee salary types dropdown data and configuration
  employeeSalaryTypes: EmployeeSalaryTypeDto[] = [];
  employeeSalaryTypesForDropdown: Array<EmployeeSalaryTypeDto & { displayText: string }> = [];
  employeeSalaryTypeDropdownConfig: SearchableDropdownConfig<EmployeeSalaryTypeDto & { displayText: string }> = {
    placeholder: 'Type to search salary types...',
    displayProperty: 'displayText',
    valueProperty: 'id',
    noResultsText: 'No matching salary types found',
    minSearchLength: 1
  };

  // Apprentice percentages dropdown data and configuration
  apprenticePercentages: ApprenticePercentageDto[] = [];
  apprenticePercentagesForDropdown: Array<ApprenticePercentageDto & { displayText: string }> = [];
  apprenticePercentageDropdownConfig: SearchableDropdownConfig<ApprenticePercentageDto & { displayText: string }> = {
    placeholder: 'Select apprentice percentage...',
    displayProperty: 'displayText',
    valueProperty: 'id',
    noResultsText: 'No matching apprentice percentages found',
    minSearchLength: 0
  };

  // Positions dropdown data and configuration
  positions: PositionDto[] = [];
  positionsForDropdown: Array<PositionDto & { displayText: string }> = [];
  positionDropdownConfig: SearchableDropdownConfig<PositionDto & { displayText: string }> = {
    placeholder: 'Type to search positions...',
    displayProperty: 'displayText',
    valueProperty: 'id',
    noResultsText: 'No matching positions found',
    minSearchLength: 1
  };

  // Payroll departments dropdown data and configuration
  payrollDepartments: PayrollDepartmentDto[] = [];
  payrollDepartmentsForDropdown: Array<PayrollDepartmentDto & { displayText: string }> = [];
  payrollDepartmentDropdownConfig: SearchableDropdownConfig<PayrollDepartmentDto & { displayText: string }> = {
    placeholder: 'Type to search payroll departments...',
    displayProperty: 'displayText',
    valueProperty: 'id',
    noResultsText: 'No matching payroll departments found',
    minSearchLength: 1
  };

  // Supervisors dropdown data and configuration
  supervisors: SupervisorDto[] = [];
  supervisorsForDropdown: Array<SupervisorDto & { displayText: string }> = [];

  // Special "NOT FOUND" supervisor entry for when no supervisors are available
  private readonly NOT_FOUND_SUPERVISOR: SupervisorDto & { displayText: string } = {
    id: -1,
    employeeNumber: -1,
    firstName: 'NOT FOUND',
    lastName: '',
    fullName: 'NOT FOUND, Will contact HR',
    companyCode: 0,
    displayText: 'NOT FOUND, Will contact HR'
  };

  get supervisorDropdownConfig(): SearchableDropdownConfig<SupervisorDto & { displayText: string }> {
    return {
      placeholder: 'Type to search supervisors...',
      displayProperty: 'displayText',
      valueProperty: 'id',
      noResultsText: 'No matching supervisors found',
      minSearchLength: 1
    };
  }

  // Employee license classes dropdown data and configuration
  employeeLicenseClasses: EmployeeLicenseClassDto[] = [];
  employeeLicenseClassesForDropdown: Array<EmployeeLicenseClassDto & { displayText: string }> = [];
  employeeLicenseClassDropdownConfig: SearchableDropdownConfig<EmployeeLicenseClassDto & { displayText: string }> = {
    placeholder: 'Type to search driver classifications...',
    displayProperty: 'displayText',
    valueProperty: 'id',
    noResultsText: 'No matching driver classifications found',
    minSearchLength: 1
  };


  // Union status tracking
  private isUnionCompany = false;

  // Form update guards to prevent infinite loops
  private isUpdatingForm = false;

  // Guard to prevent data clearing during dropdown selection
  private isSelectingFromDropdown = false;

  // Track if payroll department has been selected for supervisor placeholder logic
  private hasPayrollDepartmentSelected = false;
  // Track selected payroll department's deptCode and email domain for email generation
  private selectedPayrollDeptCode: number | null = null;
  private selectedEmailDomain: string | null = null;

  // Computed properties for conditional Other Info section
  get isSalariedEmployee(): boolean {
    const salaryTypeId = this.newHireForm.get('positionInfo.salaryCode')?.value;
    if (!salaryTypeId) return false;
    
    // Find the selected salary type from cached data
    const selectedSalaryType = this.employeeSalaryTypes.find(st => st.id.toString() === salaryTypeId);
    if (!selectedSalaryType) return false;
    
    const description = selectedSalaryType.description?.toLowerCase() || '';
    return description.includes('salary') || description.includes('salaried');
  }

  get isEmailRequired(): boolean {
    return this.newHireForm.get('itInfo.emailRequired')?.value === 'yes';
  }

  get showOtherInfoSection(): boolean {
    return this.isSalariedEmployee;
  }

  get showMicrosoftLicenseSection(): boolean {
    return this.isEmailRequired;
  }

  get isDeskPhoneSelected(): boolean {
    const phoneTypes = this.newHireForm.get('itInfo.phoneTypes') as FormArray;
    return phoneTypes?.at(0)?.value === true;
  }

  // Reference data
  referenceData: ReferenceDataOptions = {
    companies: [
      { value: '78', label: '78 - Petro Energy, LLC' },
      { value: '19', label: '19 - Mathy Construction Company' },
      { value: '88', label: '88 - Pavement Materials LLC' }
    ],
    physicalLocations: [],
    employmentStatuses: [],
    payrollCodes: [],
    supervisors: [],
    unionCrafts: [],
    driverClassifications: [],
    applications: []
  };

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private route: ActivatedRoute,
    private location: Location,
    private authService: AuthService,
    private toasterService: ToasterService,
    private referenceDataService: ReferenceDataService,
    private cdr: ChangeDetectorRef,
    private hrRequestService: HRRequestService
  ) {
    this.initializeForm();
  }

  async ngOnInit() {
    // Initialize route parameters first
    this.initializeRouteParameters();

    // Check user authorization
    await this.checkUserAuthorization();

    // Load reference data
    this.loadReferenceData();
    this.setupFormSubscriptions();

    if (this.isEditMode && this.parentId) {
      // Load existing request data for edit mode
      await this.loadExistingRequest();

      // Focus on a specific field if requested via query param (retry until DOM renders)
      if (this.focusField) {
        this.focusFieldWithRetry(this.focusField, 10);
      }
    } else {
      // Initialize create mode
      this.initializeCreateMode();
    }
  }

  private initializeForm() {
    this.newHireForm = this.fb.group({
      // Personal Information
      personalInfo: this.fb.group({
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        middleInitial: [''],
        suffix: [''],
        preferredName: [''],
        userId: [''],
        firstDay: ['', Validators.required],
        referredBy: [''],
        rehire: ['no', Validators.required]
      }),
      
      // Position Information
      positionInfo: this.fb.group({
        company: ['', Validators.required],
        physicalLocation: ['', Validators.required],
        union: ['no'],
        unionCraft: [''],
        employmentStatus: ['', Validators.required],
        apprentice: ['no'],
        unionWage: ['no'],
        apprenticeDropdown: [''],
        salaryCode: ['', Validators.required],
        position: ['', Validators.required],
        payrollCode: ['', Validators.required],
        payrollDisplayName: [''], // For view mode display
        supervisor: ['', Validators.required],
        supervisorDisplayName: [''] // For view mode display
      }),

      // Credit Card Information
      creditCardInfo: this.fb.group({
        kwikTripCard: ['no'],
        companyExpenseCard: ['no'],
        creditExpenseType: [''],
        weeklyLimit: [''],
        fuelCardlockAccess: ['no'],
        cardlockShipAddress: ['']
      }),

      // Vehicle Information
      vehicleInfo: this.fb.group({
        approvedVehicle: ['no'],
        driverClassification: [''],
        drugAlcoholProfile: [''],
        companyCarNeeded: [''],
        applicationPart2: ['']
      }),

      // Building Access (will be created dynamically based on company)
      buildingAccess: this.fb.array([]),
      useExistingKeyFob: this.fb.control(false),

      // IT Information
      itInfo: this.fb.group({
        emailRequired: ['no'], // Default to 'no', no validators needed
        microsoftLicense: [''],
        emailAddress: [''], // Auto-generated email address
        phoneTypes: this.fb.array([
          this.fb.control(false), // Desk Phone
          this.fb.control(false), // Company Cell Phone
          this.fb.control(false)  // BYOD Cell Phone
        ]),
        workPhoneNumber: [''],
        workExtension: [''],
        reusingPhone: ['no'],
        computerEquipment: ['none'], // Single FormControl for radio button selection, default to None
        rolesRequiredNewHires: ['', Validators.required],
        cargasAppRole: [''],
        dataCollectionRole: [''],
        milestoneRole: [''],
        xrsRole: [''],
        solarConnectionRole: [''],
        toddsRediMixRole: [''],
        deliveryNote: ['']
      }),

      // Dynamic Arrays
      applicationSoftware: this.fb.array([
        this.createApplicationSoftwareGroup()
      ]),

      folderSharepoint: this.fb.array([
        this.createFolderSharepointGroup()
      ]),

      // Notes field (shared across tabs)
      notes: ['']
    });
  }


  private createApplicationSoftwareGroup(): FormGroup {
    return this.fb.group({
      applicationSoftware: [''],
      applicationAccessNote: ['']
    });
  }

  private createFolderSharepointGroup(): FormGroup {
    return this.fb.group({
      type: [''],
      folderSharepointMailbox: ['']
    });
  }

  private loadReferenceData() {
    this.isLoading = true;
    let companiesLoaded = false;
    let locationsLoaded = false;
    let apprenticePercentagesLoaded = false;
    let employeeLicenseClassesLoaded = false;
    let computerRequirementsLoaded = false;

    const checkIfAllLoaded = () => {
      if (companiesLoaded && locationsLoaded && apprenticePercentagesLoaded && employeeLicenseClassesLoaded && computerRequirementsLoaded) {
        this.isLoading = false;
      }
    };
    
    // Load companies from API
    this.referenceDataService.getCompaniesWithCache().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.companies = response.data;
          // Create formatted display data for dropdown
          this.companiesForDropdown = this.companies.map(company => ({
            ...company,
            displayText: `${company.companyCode} - ${company.companyName}`
          }));
        } else {
          console.error('Failed to load companies:', response.errors);
          console.error('Full response:', response);
          this.toasterService.showError('Failed to load company data', 'Error');
        }
        companiesLoaded = true;
        checkIfAllLoaded();
      },
      error: (error) => {
        console.error('Error loading companies:', error);
        this.toasterService.showError('Failed to load company data', 'Error');
        companiesLoaded = true;
        checkIfAllLoaded();
      }
    });

    // Load physical locations from API
    this.referenceDataService.getPhysicalLocationsWithCache().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.physicalLocations = response.data;
          // Create formatted display data for dropdown
          this.physicalLocationsForDropdown = this.physicalLocations.map(location => ({
            ...location,
            displayText: `${location.locationCode} - ${location.locationName}`
          }));
        } else {
          console.error('Failed to load physical locations:', response.errors);
          this.toasterService.showError('Failed to load physical location data', 'Error');
        }
        locationsLoaded = true;
        checkIfAllLoaded();
      },
      error: (error) => {
        console.error('Error loading physical locations:', error);
        this.toasterService.showError('Failed to load physical location data', 'Error');
        locationsLoaded = true;
        checkIfAllLoaded();
      }
    });


    // Load apprentice percentages from API
    this.referenceDataService.getApprenticePercentagesWithCache().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.apprenticePercentages = response.data;
          // Create formatted display data for dropdown
          this.apprenticePercentagesForDropdown = this.apprenticePercentages.map(apprenticePercentage => ({
            ...apprenticePercentage,
            displayText: `${apprenticePercentage.appPercentage} - ${apprenticePercentage.appDescription}`
          }));
        } else {
          console.error('Failed to load apprentice percentages:', response.errors);
          this.toasterService.showError('Failed to load apprentice percentage data', 'Error');
        }
        apprenticePercentagesLoaded = true;
        checkIfAllLoaded();
      },
      error: (error) => {
        console.error('Error loading apprentice percentages:', error);
        this.toasterService.showError('Failed to load apprentice percentage data', 'Error');
        apprenticePercentagesLoaded = true;
        checkIfAllLoaded();
      }
    });

    // Load employee license classes from API
    this.referenceDataService.getEmployeeLicenseClassesWithCache().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.employeeLicenseClasses = response.data;
          // Create formatted display data for dropdown
          this.employeeLicenseClassesForDropdown = this.employeeLicenseClasses.map(licenseClass => ({
            ...licenseClass,
            displayText: `${licenseClass.licenseClass}${licenseClass.description ? ' - ' + licenseClass.description : ''}`
          }));
        } else {
          console.error('Failed to load employee license classes:', response.errors);
          this.toasterService.showError('Failed to load driver classification data', 'Error');
        }
        employeeLicenseClassesLoaded = true;
        checkIfAllLoaded();
      },
      error: (error) => {
        console.error('Error loading employee license classes:', error);
        this.toasterService.showError('Failed to load driver classification data', 'Error');
        employeeLicenseClassesLoaded = true;
        checkIfAllLoaded();
      }
    });

    // Load computer requirements from API
    this.referenceDataService.getComputerRequirements().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.computerRequirements = response.data;
          this.organizeComputerRequirements();
          this.initializeComputerEquipmentControl();

          // Default to 'None' in create mode (already set in form initialization)
        } else {
          console.error('Failed to load computer requirements:', response.errors);
          this.toasterService.showError('Failed to load computer requirements data', 'Error');
          
          // Fallback: Use sample data for testing when API fails
          this.useSampleComputerRequirementsData();
        }
        computerRequirementsLoaded = true;
        checkIfAllLoaded();
      },
      error: (error) => {
        console.error('Error loading computer requirements:', error);
        
        // Use sample data when API is unavailable
        this.useSampleComputerRequirementsData();
        
        computerRequirementsLoaded = true;
        checkIfAllLoaded();
      }
    });
  }

  private setupFormSubscriptions() {
    // Company selection conditional logic
    this.newHireForm.get('positionInfo.company')?.valueChanges.subscribe(company => {
      if (!this.isUpdatingForm) {
        this.handleCompanyChange(company);
      }
    });

    // Apprentice radio button conditional logic
    this.newHireForm.get('positionInfo.apprentice')?.valueChanges.subscribe(value => {
      if (!this.isUpdatingForm) {
        this.updateUnionValidation();
      }
    });

    // Company Expense Card conditional logic
    this.newHireForm.get('creditCardInfo.companyExpenseCard')?.valueChanges.subscribe(value => {
      this.handleCompanyExpenseCardChange(value);
    });

    // Fuel Cardlock Access conditional logic
    this.newHireForm.get('creditCardInfo.fuelCardlockAccess')?.valueChanges.subscribe(value => {
      this.handleFuelCardlockChange(value);
    });

    // Vehicle approval conditional logic
    this.newHireForm.get('vehicleInfo.approvedVehicle')?.valueChanges.subscribe(value => {
      this.handleVehicleApprovalChange(value);
    });

    // Hourly/Salaried selection conditional logic for Other Info section
    this.newHireForm.get('positionInfo.salaryCode')?.valueChanges.subscribe(value => {
      this.handleSalaryTypeChange(value);
    });

    // Email required conditional logic for Microsoft Office License section
    this.newHireForm.get('itInfo.emailRequired')?.valueChanges.subscribe(value => {
      this.handleEmailRequiredChange(value);
    });

    // Roles required conditional logic
    this.newHireForm.get('itInfo.rolesRequiredNewHires')?.valueChanges.subscribe(value => {
      this.handleRolesRequiredChange(value);
    });

    // Phone types mutual exclusion logic (Company Cell Phone and BYOD Cell Phone)
    const phoneTypesArray = this.newHireForm.get('itInfo.phoneTypes') as FormArray;
    if (phoneTypesArray) {
      // Desk Phone (index 0) - clear related fields when unchecked, default reusingPhone to 'no' when checked
      phoneTypesArray.at(0)?.valueChanges.subscribe(isChecked => {
        if (isChecked) {
          this.newHireForm.get('itInfo.reusingPhone')?.setValue('no');
        } else {
          this.newHireForm.get('itInfo.workPhoneNumber')?.setValue('');
          this.newHireForm.get('itInfo.workExtension')?.setValue('');
          this.newHireForm.get('itInfo.reusingPhone')?.setValue('');
        }
      });

      // Company Cell Phone (index 1) subscription
      phoneTypesArray.at(1)?.valueChanges.subscribe(isChecked => {
        if (isChecked) {
          this.handlePhoneTypeChange(1, isChecked);
        }
      });

      // BYOD Cell Phone (index 2) subscription
      phoneTypesArray.at(2)?.valueChanges.subscribe(isChecked => {
        if (isChecked) {
          this.handlePhoneTypeChange(2, isChecked);
        }
      });
    }

    // Subscribe to union radio button changes to filter employment statuses
    this.newHireForm.get('positionInfo.union')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.filterEmploymentStatusesByUnion();
      });

    // Subscribe to First Name changes to auto-generate User ID
    // Skip when populating form (isUpdatingForm) or viewing a non-editable request
    // The debounceTime(500) can fire after isUpdatingForm resets, so also check isEditMode + isFormEditable
    this.newHireForm.get('personalInfo.firstName')?.valueChanges
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(500),
        distinctUntilChanged()
      )
      .subscribe(() => {
        if (!this.isUpdatingForm && (!this.isEditMode || this.isFormEditable)) {
          this.generateUserId();
        }
      });

    // Subscribe to Preferred First Name changes to auto-generate User ID
    this.newHireForm.get('personalInfo.preferredName')?.valueChanges
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(500),
        distinctUntilChanged()
      )
      .subscribe(() => {
        if (!this.isUpdatingForm && (!this.isEditMode || this.isFormEditable)) {
          this.generateUserId();
        }
      });

    // Subscribe to Last Name changes to re-generate email address
    this.newHireForm.get('personalInfo.lastName')?.valueChanges
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(500),
        distinctUntilChanged()
      )
      .subscribe(() => {
        if (!this.isUpdatingForm && (!this.isEditMode || this.isFormEditable)) {
          this.generateEmailAddress();
        }
      });

  }

  // Check if an employment status is union-related
  private isUnionStatus(status: EmploymentStatusDto): boolean {
    return status.status === 'U-ACTIVE' ||
           status.status === 'U-MANAGER' ||
           (status.description?.toLowerCase().includes('union') ?? false);
  }

  // Filter employment statuses based on union selection
  private filterEmploymentStatusesByUnion(): void {
    if (!this.employmentStatuses || this.employmentStatuses.length === 0) {
      return;
    }

    const isUnion = this.newHireForm.get('positionInfo.union')?.value === 'yes';

    let filteredStatuses: EmploymentStatusDto[];

    if (isUnion) {
      // Union: Show only union-related statuses
      filteredStatuses = this.employmentStatuses.filter(status => this.isUnionStatus(status));
    } else {
      // Non-union: Show all except union-related statuses
      filteredStatuses = this.employmentStatuses.filter(status => !this.isUnionStatus(status));
    }

    this.employmentStatusesForDropdown = filteredStatuses.map(status => ({
      ...status,
      displayText: `${status.status} - ${status.description}`
    }));

    // Clear employment status selection if current value is no longer in the filtered list
    const currentValue = this.newHireForm.get('positionInfo.employmentStatus')?.value;
    if (currentValue) {
      const stillExists = this.employmentStatusesForDropdown.some(
        status => status.id.toString() === currentValue
      );
      if (!stillExists) {
        this.newHireForm.get('positionInfo.employmentStatus')?.setValue('');
      }
    }
  }

  // Generate User ID when First Name or Preferred First Name changes
  private generateUserId(): void {
    const firstName = this.newHireForm.get('personalInfo.firstName')?.value?.trim() || '';
    const preferredName = this.newHireForm.get('personalInfo.preferredName')?.value?.trim() || '';

    // Need at least first name to generate user ID
    if (!firstName) {
      return;
    }

    this.referenceDataService.generateUsername(firstName, preferredName || undefined)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.newHireForm.get('personalInfo.userId')?.setValue(response.data);
            // Also generate email address after User ID is generated
            this.generateEmailAddress();
          }
        },
        error: (error) => {
          console.error('Error generating username:', error);
        }
      });
  }

  // Generate Email Address locally based on employee name, email required selection, and payroll department's email domain
  private generateEmailAddress(): void {
    const firstName = this.newHireForm.get('personalInfo.firstName')?.value?.trim() || '';
    const lastName = this.newHireForm.get('personalInfo.lastName')?.value?.trim() || '';
    const preferredName = this.newHireForm.get('personalInfo.preferredName')?.value?.trim() || '';
    const userId = this.newHireForm.get('personalInfo.userId')?.value?.trim() || '';
    const emailRequired = this.newHireForm.get('itInfo.emailRequired')?.value === 'yes';

    // Need first name, last name, and email domain to generate email
    if (!firstName || !lastName || !this.selectedEmailDomain) {
      return;
    }

    let emailAddress: string;
    if (emailRequired) {
      // Email required: firstname.lastname@domain or preferred.lastname@domain
      // Remove spaces to handle names like "Mary Ann" -> "maryann"
      const namePrefix = preferredName ? preferredName.toLowerCase().replace(/\s/g, '') : firstName.toLowerCase().replace(/\s/g, '');
      emailAddress = `${namePrefix}.${lastName.toLowerCase().replace(/\s/g, '')}@${this.selectedEmailDomain}`;
    } else {
      // No email required: userId@domain
      if (!userId) {
        return;
      }
      emailAddress = `${userId.toLowerCase()}@${this.selectedEmailDomain}`;
    }

    this.newHireForm.get('itInfo.emailAddress')?.setValue(emailAddress);
  }

  // Handle company selection from searchable dropdown
  onCompanySelected(selectedCompany: (CompanyDto & { displayText: string }) | null): void {
    // Set flag to indicate we're in the middle of a dropdown selection
    this.isSelectingFromDropdown = true;
    
    if (selectedCompany) {
      const companyCode = selectedCompany.companyCode.toString();
      
      // Set flag to prevent infinite loops during programmatic updates
      this.isUpdatingForm = true;
      this.newHireForm.get('positionInfo.company')?.setValue(companyCode, { emitEvent: false });
      this.isUpdatingForm = false;
      
      
      // Load company union status and employment statuses dynamically
      this.loadCompanyUnionStatus(selectedCompany.companyCode);
      this.loadCompanyEmploymentStatuses(selectedCompany.companyCode);

      // Set union default based on company
      this.setUnionDefault(selectedCompany.companyCode);

      // Manually trigger company change handling since we're not emitting the form event
      this.handleCompanyChange(companyCode);
    } else {
      
      // Set flag to prevent infinite loops during programmatic updates
      this.isUpdatingForm = true;
      this.newHireForm.get('positionInfo.company')?.setValue('', { emitEvent: false });
      this.isUpdatingForm = false;
      
      this.isUnionCompany = false;
      this.availableBuildingAccess = [];
      this.buildingAccessRequirements = [];
      this.employmentStatusesForDropdown = [];
      this.newHireForm.get('positionInfo.employmentStatus')?.setValue('');
      this.newHireForm.get('positionInfo.union')?.setValue('');
      this.recreateBuildingAccessFormArray();

      // Manually trigger company change handling since we're not emitting the form event
      this.handleCompanyChange('');
    }
    
    // Clear the selection flag after a brief delay to allow UI to stabilize
    setTimeout(() => {
      this.isSelectingFromDropdown = false;
    }, 200);
    
  }

  // Handle physical location selection from searchable dropdown
  onPhysicalLocationSelected(selectedLocation: (PhysicalLocationDto & { displayText: string }) | null): void {
    if (selectedLocation) {
      const locationCode = selectedLocation.locationCode.toString();
      this.newHireForm.get('positionInfo.physicalLocation')?.setValue(locationCode);
    } else {
      this.newHireForm.get('positionInfo.physicalLocation')?.setValue('');
    }
  }

  // Handle employment status selection from searchable dropdown
  onEmploymentStatusSelected(selectedStatus: (EmploymentStatusDto & { displayText: string }) | null): void {
    const control = this.newHireForm.get('positionInfo.employmentStatus');
    if (selectedStatus) {
      this.safeSetControlValue(control, selectedStatus.id.toString());
    } else {
      this.safeSetControlValue(control, '');
    }
  }

  // Handle union craft selection from searchable dropdown
  onUnionCraftSelected(selectedUnionCraft: (UnionCraftDto & { displayText: string }) | null): void {
    if (selectedUnionCraft) {
      this.newHireForm.get('positionInfo.unionCraft')?.setValue(selectedUnionCraft.id.toString());
    } else {
      this.newHireForm.get('positionInfo.unionCraft')?.setValue('');
    }
  }

  // Handle employee salary type selection from searchable dropdown
  onEmployeeSalaryTypeSelected(selectedSalaryType: (EmployeeSalaryTypeDto & { displayText: string }) | null): void {
    if (selectedSalaryType) {
      this.newHireForm.get('positionInfo.salaryCode')?.setValue(selectedSalaryType.id.toString());
    } else {
      this.newHireForm.get('positionInfo.salaryCode')?.setValue('');
    }
  }

  // Handle apprentice percentage selection from searchable dropdown
  onApprenticePercentageSelected(selectedApprenticePercentage: (ApprenticePercentageDto & { displayText: string }) | null): void {
    if (selectedApprenticePercentage) {
      this.newHireForm.get('positionInfo.apprenticeDropdown')?.setValue(selectedApprenticePercentage.id.toString());
    } else {
      this.newHireForm.get('positionInfo.apprenticeDropdown')?.setValue('');
    }
  }

  // Handle position selection from searchable dropdown
  onPositionSelected(selectedPosition: (PositionDto & { displayText: string }) | null): void {
    if (selectedPosition) {
      this.newHireForm.get('positionInfo.position')?.setValue(selectedPosition.id.toString());
    } else {
      this.newHireForm.get('positionInfo.position')?.setValue('');
    }
  }

  // Handle payroll department selection from searchable dropdown
  onPayrollDepartmentSelected(selectedPayrollDepartment: (PayrollDepartmentDto & { displayText: string }) | null): void {
    if (selectedPayrollDepartment) {
      this.newHireForm.get('positionInfo.payrollCode')?.setValue(selectedPayrollDepartment.id.toString());

      // Track that payroll department has been selected
      this.hasPayrollDepartmentSelected = true;
      this.selectedPayrollDeptCode = selectedPayrollDepartment.deptCode;
      this.selectedEmailDomain = selectedPayrollDepartment.emailDomain || null;

      // Load supervisors when payroll department changes
      const companyValue = this.newHireForm.get('positionInfo.company')?.value;
      if (companyValue) {
        this.loadSupervisors(parseInt(companyValue), selectedPayrollDepartment.deptCode);
      }

      // Generate email address when payroll department is selected
      this.generateEmailAddress();
    } else {
      this.newHireForm.get('positionInfo.payrollCode')?.setValue('');

      // Track that payroll department has been deselected
      this.hasPayrollDepartmentSelected = false;
      this.selectedPayrollDeptCode = null;
      this.selectedEmailDomain = null;

      // Clear supervisors when payroll department is cleared
      this.supervisors = [];
      this.supervisorsForDropdown = [];
      this.newHireForm.get('positionInfo.supervisor')?.setValue('');

      // Clear email address when payroll department is cleared
      this.newHireForm.get('itInfo.emailAddress')?.setValue('');
    }
  }

  // Handle supervisor selection from searchable dropdown
  onSupervisorSelected(selectedSupervisor: (SupervisorDto & { displayText: string }) | null): void {
    const control = this.newHireForm.get('positionInfo.supervisor');
    if (selectedSupervisor) {
      this.safeSetControlValue(control, selectedSupervisor.id.toString());
    } else {
      this.safeSetControlValue(control, '');
    }
  }

  // Load supervisors for specific company and payroll department
  private loadSupervisors(companyCode: number, payrollDeptCode: number): void {
    this.referenceDataService.getSupervisorsWithCache(companyCode, payrollDeptCode).subscribe({
      next: (response) => {
        if (response.success && response.data && response.data.length > 0) {
          this.supervisors = response.data;
          this.supervisorsForDropdown = [
            ...this.supervisors.map(supervisor => ({
              ...supervisor,
              displayText: `${supervisor.firstName} ${supervisor.lastName} (${supervisor.employeeNumber})`
            })),
            this.NOT_FOUND_SUPERVISOR
          ];
          // Clear previous selection (e.g. NOT FOUND) so user picks from new list
          this.newHireForm.get('positionInfo.supervisor')?.setValue('');
        } else {
          // No supervisors found - show NOT FOUND option and auto-select it
          console.warn('No supervisors found for company:', companyCode, 'and payroll dept:', payrollDeptCode);
          this.supervisors = [];
          this.supervisorsForDropdown = [this.NOT_FOUND_SUPERVISOR];
          // Auto-select NOT FOUND after change detection processes new items
          setTimeout(() => {
            this.newHireForm.get('positionInfo.supervisor')?.setValue(this.NOT_FOUND_SUPERVISOR.id.toString());
          }, 0);
        }
      },
      error: (error) => {
        console.error('Error loading supervisors for company:', companyCode, 'and payroll dept:', payrollDeptCode, error);
        // On error - show NOT FOUND option and auto-select it
        this.supervisors = [];
        this.supervisorsForDropdown = [this.NOT_FOUND_SUPERVISOR];
        // Auto-select NOT FOUND after change detection processes new items
        setTimeout(() => {
          this.newHireForm.get('positionInfo.supervisor')?.setValue(this.NOT_FOUND_SUPERVISOR.id.toString());
        }, 0);
      }
    });
  }

  // Load company-specific data (employment statuses and union crafts)
  private async loadCompanySpecificData(companyCode: number): Promise<void> {

    try {
      // Load critical data first (employment statuses and company union status)
      await Promise.all([
        this.loadEmploymentStatuses(companyCode),
        this.loadCompanyUnionStatusAsync(companyCode)
      ]);

      // Then load other dropdown data with small delays to prevent UI blocking
      await this.loadEmployeeSalaryTypes(companyCode);
      
      // Small delay to allow UI to breathe
      await this.delay(50);
      
      await this.loadPositions(companyCode);
      
      // Small delay to allow UI to breathe
      await this.delay(50);
      
      await this.loadPayrollDepartments(companyCode);
      
      // Small delay to allow UI to breathe
      await this.delay(50);
      
      await this.loadTabletProfiles(companyCode);
      
      // Small delay to allow UI to breathe
      await this.delay(50);
      
      await this.loadApplications(companyCode);
      
      // Small delay to allow UI to breathe
      await this.delay(50);
      
      await this.loadUnionCrafts(companyCode);

    } catch (error) {
      console.error('Error loading company-specific data:', error);
    }
  }

  private delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  private loadEmploymentStatuses(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.referenceDataService.getEmploymentStatusesWithCache(companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.employmentStatuses = response.data;
            // Filter employment statuses based on union selection
            this.filterEmploymentStatusesByUnion();
          } else {
            console.error('Failed to load employment statuses for company:', companyCode, response.errors);
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading employment statuses for company:', companyCode, error);
          reject(error);
        }
      });
    });
  }

  private loadEmployeeSalaryTypes(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.referenceDataService.getEmployeeSalaryTypesWithCache(companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.employeeSalaryTypes = response.data;
            this.employeeSalaryTypesForDropdown = this.employeeSalaryTypes.map(salaryType => ({
              ...salaryType,
              displayText: salaryType.description
            }));

            // Auto-select if only one item and no value is currently set
            const currentValue = this.newHireForm.get('positionInfo.salaryCode')?.value;
            if (this.employeeSalaryTypesForDropdown.length === 1 && !currentValue) {
              this.newHireForm.get('positionInfo.salaryCode')?.setValue(this.employeeSalaryTypesForDropdown[0].id.toString());
            }
          } else {
            console.error('Failed to load employee salary types for company:', companyCode, response.errors);
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading employee salary types for company:', companyCode, error);
          reject(error);
        }
      });
    });
  }

  private loadPositions(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.referenceDataService.getPositionsWithCache(companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.positions = response.data;
            this.filterPositionsBySalaryType();
          } else {
            console.error('Failed to load positions for company:', companyCode, response.errors);
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading positions for company:', companyCode, error);
          reject(error);
        }
      });
    });
  }

  private loadPayrollDepartments(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.referenceDataService.getPayrollDepartmentsByCompanyWithCache(companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.payrollDepartments = response.data;
            this.payrollDepartmentsForDropdown = this.payrollDepartments.map(dept => ({
              ...dept,
              displayText: `${dept.deptCode} - ${dept.deptName}`
            }));
          } else {
            console.error('Failed to load payroll departments for company:', companyCode, response.errors);
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading payroll departments for company:', companyCode, error);
          reject(error);
        }
      });
    });
  }

  private loadTabletProfiles(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.referenceDataService.getTabletProfilesWithCache(companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.tabletProfiles = response.data;
            // Update available roles with tablet profile names
            this.updateAvailableRolesFromTabletProfiles();
          } else {
            console.error('Failed to load tablet profiles for company:', companyCode, response.errors);
            // Fallback to empty roles if tablet profiles fail to load
            this.availableRoles = [];
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading tablet profiles for company:', companyCode, error);
          // Fallback to empty roles if tablet profiles fail to load
          this.availableRoles = [];
          reject(error);
        }
      });
    });
  }

  private loadApplications(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.referenceDataService.getApplicationsWithCache(companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.applications = response.data;
            this.applicationsForDropdown = response.data.map(app => ({
              ...app,
              displayText: app.name
            }));
          } else {
            console.error('Failed to load applications for company:', companyCode, response.errors);
            this.applications = [];
            this.applicationsForDropdown = [];
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading applications for company:', companyCode, error);
          this.applications = [];
          this.applicationsForDropdown = [];
          reject(error);
        }
      });
    });
  }

  private loadUnionCrafts(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      this.referenceDataService.getUnionCraftsWithCache(companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.unionCrafts = response.data;
            this.unionCraftsForDropdown = response.data.map(unionCraft => ({
              ...unionCraft,
              displayText: `${unionCraft.craftCode} - ${unionCraft.description}`
            }));
          } else {
            console.error('Failed to load union crafts for company:', companyCode, response.errors);
            this.unionCrafts = [];
            this.unionCraftsForDropdown = [];
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading union crafts for company:', companyCode, error);
          this.unionCrafts = [];
          this.unionCraftsForDropdown = [];
          reject(error);
        }
      });
    });
  }

  // Load company union status and building access from APIs
  private loadCompanyUnionStatus(companyCode: number): void {
    // Load company type locations for union status
    this.referenceDataService.getCompanyTypeLocations(undefined, companyCode).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // Check if any location type for this company has IsUnion = true
          this.isUnionCompany = response.data.some(ctl => ctl.isUnion);
          
          // Update form validation based on union status
          this.updateUnionValidation();
        } else {
          console.error('Failed to load company type locations:', response.errors);
          this.isUnionCompany = false;
        }
      },
      error: (error) => {
        console.error('Error loading company type locations:', error);
        this.isUnionCompany = false;
      }
    });

    // Load building access requirements from API
    this.referenceDataService.getBuildingAccessRequirementsWithCache(companyCode).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.buildingAccessRequirements = response.data;
          this.availableBuildingAccess = response.data
            .map(req => req.description)
            .sort((a, b) => a.localeCompare(b));
          this.recreateBuildingAccessFormArray();
        } else {
          console.error('Failed to load building access requirements:', response.errors);
          this.buildingAccessRequirements = [];
          this.availableBuildingAccess = [];
          this.recreateBuildingAccessFormArray();
        }
      },
      error: (error) => {
        console.error('Error loading building access requirements:', error);
        this.buildingAccessRequirements = [];
        this.availableBuildingAccess = [];
        this.recreateBuildingAccessFormArray();
      }
    });
  }

  // Async version of loadCompanyUnionStatus for sequential loading
  private loadCompanyUnionStatusAsync(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      let unionStatusLoaded = false;
      let buildingAccessLoaded = false;
      let hasError = false;

      const checkCompletion = () => {
        if (!hasError && unionStatusLoaded && buildingAccessLoaded) {
          resolve();
        }
      };

      // Load company type locations for union status
      this.referenceDataService.getCompanyTypeLocations(undefined, companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            // Check if any location type for this company has IsUnion = true
            this.isUnionCompany = response.data.some(ctl => ctl.isUnion);
            
            // Update form validation based on union status
            this.updateUnionValidation();
          } else {
            console.error('Failed to load company type locations:', response.errors);
            this.isUnionCompany = false;
          }
          unionStatusLoaded = true;
          checkCompletion();
        },
        error: (error) => {
          console.error('Error loading company type locations:', error);
          this.isUnionCompany = false;
          hasError = true;
          reject(error);
        }
      });

      // Load building access requirements from API
      this.referenceDataService.getBuildingAccessRequirementsWithCache(companyCode).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.buildingAccessRequirements = response.data;
            this.availableBuildingAccess = response.data
            .map(req => req.description)
            .sort((a, b) => a.localeCompare(b));
            this.recreateBuildingAccessFormArray();
          } else {
            console.error('Failed to load building access requirements:', response.errors);
            this.buildingAccessRequirements = [];
            this.availableBuildingAccess = [];
            this.recreateBuildingAccessFormArray();
          }
          buildingAccessLoaded = true;
          checkCompletion();
        },
        error: (error) => {
          console.error('Error loading building access requirements:', error);
          this.buildingAccessRequirements = [];
          this.availableBuildingAccess = [];
          this.recreateBuildingAccessFormArray();
          hasError = true;
          reject(error);
        }
      });
    });
  }



  // Load company employment statuses from API
  private loadCompanyEmploymentStatuses(companyCode: number): void {
    this.referenceDataService.getEmploymentStatusesWithCache(companyCode).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.employmentStatuses = response.data;
          // Filter employment statuses based on union selection
          this.filterEmploymentStatusesByUnion();
        } else {
          console.error('Failed to load employment statuses:', response.errors);
          this.employmentStatuses = [];
          this.employmentStatusesForDropdown = [];
        }
      },
      error: (error) => {
        console.error('Error loading employment statuses:', error);
        this.employmentStatuses = [];
        this.employmentStatusesForDropdown = [];
      }
    });
  }

  // Set union and apprentice defaults based on company selection
  private setUnionDefault(companyCode: number): void {
    // Don't override values in edit mode - data is already loaded for view
    if (this.isEditMode) {
      return;
    }

    // Set default union value based on company
    if (companyCode === 19) { // Mathy Construction Company
      this.newHireForm.get('positionInfo.union')?.setValue('yes');
    } else {
      this.newHireForm.get('positionInfo.union')?.setValue('no');
    }

    // Always set apprentice default to 'no' regardless of company
    this.newHireForm.get('positionInfo.apprentice')?.setValue('no');

    // Always set union wage default to 'no' regardless of company
    this.newHireForm.get('positionInfo.unionWage')?.setValue('no');
  }

  // Update validation based on union status
  private updateUnionValidation(): void {
    // Prevent infinite loops during form updates
    if (this.isUpdatingForm) {
      return;
    }
    
    // Set flag to prevent recursive calls
    this.isUpdatingForm = true;
    
    try {
      const positionInfo = this.newHireForm.get('positionInfo');
      
      // Apprentice should never be required, always clear its validators
      positionInfo?.get('apprentice')?.clearValidators();

      if (this.isUnionCompany) {
        positionInfo?.get('union')?.setValidators([Validators.required]);
      } else {
        positionInfo?.get('union')?.clearValidators();
        // Don't override union value - let setUnionDefault() handle union values
        positionInfo?.get('apprentice')?.setValue('no');
        positionInfo?.get('unionCraft')?.setValue('');
        positionInfo?.get('unionWage')?.setValue('no');
        positionInfo?.get('apprenticeDropdown')?.setValue('');
        positionInfo?.get('apprenticeDropdown')?.clearValidators();
      }
      
      // Update apprentice dropdown validation based on apprentice radio selection
      const apprenticeValue = positionInfo?.get('apprentice')?.value;
      // Apprentice dropdown is never required - always clear validators
      positionInfo?.get('apprenticeDropdown')?.clearValidators();
      if (apprenticeValue !== 'yes') {
        positionInfo?.get('apprenticeDropdown')?.setValue('');
      }
    } finally {
      // Always reset the flag, even if an error occurs
      this.isUpdatingForm = false;
    }
  }

  private handleCompanyChange(company: string) {
    // Prevent infinite loops during form updates
    if (this.isUpdatingForm) {
      return;
    }

    const itInfo = this.newHireForm.get('itInfo');

    // Update available roles from tablet profiles (loaded from database)
    // Only use hardcoded fallback if tablet profiles haven't been loaded yet
    if (this.tabletProfiles && this.tabletProfiles.length > 0) {
      this.updateAvailableRolesFromTabletProfiles();
    } else {
      // Tablet profiles will be loaded asynchronously, no need to set hardcoded values
      // The updateAvailableRolesFromTabletProfiles() will be called when loadTabletProfiles completes
    }

    // Note: Building access is now loaded dynamically via loadCompanyUnionStatus method
    // based on CompanyTypeLocation API response, so we don't call updateAvailableBuildingAccess here

    // Skip resetting role selection and text fields when loading existing data (edit mode)
    // This prevents overwriting saved tablet profile values
    if (this.isEditMode && this.viewData) {
      // In edit mode, still load company-specific data but don't reset form values
      if (company) {
        const companyCode = parseInt(company);
        this.setUnionDefault(companyCode);
        this.loadCompanySpecificData(companyCode);
      }
      return;
    }

    // Reset role selection and text fields when company changes (only for new requests)
    itInfo?.get('rolesRequiredNewHires')?.setValue('');
    // Clear all role-specific text fields
    itInfo?.get('cargasAppRole')?.setValue('');
    itInfo?.get('dataCollectionRole')?.setValue('');
    itInfo?.get('milestoneRole')?.setValue('');
    itInfo?.get('xrsRole')?.setValue('');
    itInfo?.get('solarConnectionRole')?.setValue('');
    itInfo?.get('toddsRediMixRole')?.setValue('');

    // Check if F3 license should be auto-selected based on new company selection
    if (this.shouldAutoSelectF3License()) {
      itInfo?.get('microsoftLicense')?.setValue('f3');
    } else if (company === '19') {
      // For Mathy Construction (19), don't auto-select F3, but keep existing selection if any
      // This allows manual selection for company 19
    }

    // Load employment statuses and union crafts for the selected company
    if (company) {
      // Clear dependent dropdowns before loading new company data
      this.clearCompanyDependentDropdowns();

      const companyCode = parseInt(company);
      // Set union default based on company
      this.setUnionDefault(companyCode);
      this.loadCompanySpecificData(companyCode);

      // Regenerate email address when company changes (if userId and payrollDept are available)
      this.generateEmailAddress();
    } else {
      // Clear company-specific data when no company is selected
      this.clearCompanyDependentDropdowns();

      // Clear email address when company is cleared
      this.newHireForm.get('itInfo.emailAddress')?.setValue('');
    }
  }

  private clearCompanyDependentDropdowns(): void {
    // Don't clear dropdown data if user is actively selecting from a dropdown
    if (this.isSelectingFromDropdown) {
      return;
    }

    // Don't clear dropdown data in edit mode - data is already loaded for view
    if (this.isEditMode) {
      return;
    }

    // Clear form values for company-dependent dropdowns
    this.newHireForm.get('positionInfo.salaryCode')?.setValue('');
    this.newHireForm.get('positionInfo.position')?.setValue('');
    this.newHireForm.get('positionInfo.payrollCode')?.setValue('');
    this.newHireForm.get('positionInfo.supervisor')?.setValue('');
    this.newHireForm.get('positionInfo.unionCraft')?.setValue('');
    this.newHireForm.get('positionInfo.apprenticeDropdown')?.setValue('');
    this.newHireForm.get('positionInfo.employmentStatus')?.setValue('');
    
    // Clear dropdown data arrays
    this.employeeSalaryTypes = [];
    this.employeeSalaryTypesForDropdown = [];
    this.positions = [];
    this.positionsForDropdown = [];
    this.payrollDepartments = [];
    this.payrollDepartmentsForDropdown = [];
    this.supervisors = [];
    this.supervisorsForDropdown = [];
    this.unionCrafts = [];
    this.unionCraftsForDropdown = [];
    this.employmentStatusesForDropdown = [];

    // Reset payroll department selection state
    this.hasPayrollDepartmentSelected = false;
    this.selectedPayrollDeptCode = null;
    this.selectedEmailDomain = null;

    // Clear email address since payroll department is cleared
    this.newHireForm.get('itInfo.emailAddress')?.setValue('');
  }

  private handleCompanyExpenseCardChange(value: string) {
    // Don't override values in edit mode - data is already loaded for view
    if (this.isEditMode) {
      return;
    }

    const creditCardInfo = this.newHireForm.get('creditCardInfo');

    if (value === 'yes') {
      creditCardInfo?.get('creditExpenseType')?.clearValidators();
      creditCardInfo?.get('creditExpenseType')?.setValue('');
    } else {
      creditCardInfo?.get('creditExpenseType')?.clearValidators();
      creditCardInfo?.get('creditExpenseType')?.setValue('');
      creditCardInfo?.get('weeklyLimit')?.setValue('');
    }

    creditCardInfo?.get('creditExpenseType')?.updateValueAndValidity();
  }

  private handleFuelCardlockChange(value: string) {
    const creditCardInfo = this.newHireForm.get('creditCardInfo');
    
    if (value !== 'yes') {
      creditCardInfo?.get('cardlockShipAddress')?.setValue('');
    }
  }

  private handleVehicleApprovalChange(value: string) {
    // Don't override values in edit mode - data is already loaded for view
    if (this.isEditMode) {
      return;
    }

    const vehicleInfo = this.newHireForm.get('vehicleInfo');

    if (value === 'yes') {
      // Clear validators for all vehicle fields
      vehicleInfo?.get('driverClassification')?.clearValidators();
      vehicleInfo?.get('drugAlcoholProfile')?.clearValidators();
      vehicleInfo?.get('companyCarNeeded')?.clearValidators();
      vehicleInfo?.get('applicationPart2')?.clearValidators();

      // Set default values for specific fields when vehicle approval is YES
      vehicleInfo?.get('companyCarNeeded')?.setValue('no');
      vehicleInfo?.get('applicationPart2')?.setValue('no');
      // Note: drugAlcoholProfile and driverClassification are left as-is
    } else {
      // Clear validators for all vehicle fields
      vehicleInfo?.get('driverClassification')?.clearValidators();
      vehicleInfo?.get('drugAlcoholProfile')?.clearValidators();
      vehicleInfo?.get('companyCarNeeded')?.clearValidators();
      vehicleInfo?.get('applicationPart2')?.clearValidators();

      // Set values to null/empty when vehicle approval is NO
      vehicleInfo?.get('driverClassification')?.setValue('');
      vehicleInfo?.get('drugAlcoholProfile')?.setValue(null);
      vehicleInfo?.get('companyCarNeeded')?.setValue(null);
      vehicleInfo?.get('applicationPart2')?.setValue(null);
    }

    vehicleInfo?.get('driverClassification')?.updateValueAndValidity();
    vehicleInfo?.get('drugAlcoholProfile')?.updateValueAndValidity();
    vehicleInfo?.get('companyCarNeeded')?.updateValueAndValidity();
    vehicleInfo?.get('applicationPart2')?.updateValueAndValidity();
  }

  private handleRolesRequiredChange(value: string) {
    // Don't override values in edit mode - data is already loaded for view
    if (this.isEditMode) {
      return;
    }

    const itInfo = this.newHireForm.get('itInfo');

    // Clear all role-specific fields first
    itInfo?.get('cargasAppRole')?.setValue('');
    itInfo?.get('dataCollectionRole')?.setValue('');
    itInfo?.get('milestoneRole')?.setValue('');
    itInfo?.get('xrsRole')?.setValue('');
    itInfo?.get('solarConnectionRole')?.setValue('');
    itInfo?.get('toddsRediMixRole')?.setValue('');
  }

  private handleSalaryTypeChange(value: string) {
    // Filter positions based on salary type selection (applies in both create and edit modes)
    this.filterPositionsBySalaryType();

    // Skip auto-setting IT info values when loading existing data (edit mode)
    // This prevents overwriting saved values with defaults
    if (this.isEditMode && this.viewData) {
      return;
    }

    const itInfo = this.newHireForm.get('itInfo');

    // Auto-set fields based on salary type selection (only in create mode)
    if (this.isSalariedEmployee) {
      // For salaried employees, automatically set email required to 'yes' and E5 license
      itInfo?.get('emailRequired')?.setValue('yes');
      itInfo?.get('microsoftLicense')?.setValue('e5');
    } else {
      // For non-salaried employees, set email required to 'no' and clear Microsoft License
      itInfo?.get('emailRequired')?.setValue('no');
      itInfo?.get('microsoftLicense')?.setValue('');
    }

    // EmailRequired is nullable in database schema, so no validators needed
    itInfo?.get('emailRequired')?.clearValidators();
    itInfo?.get('emailRequired')?.updateValueAndValidity();
  }

  private filterPositionsBySalaryType(): void {
    const salaryTypeId = this.newHireForm.get('positionInfo.salaryCode')?.value;
    let filteredPositions = this.positions;

    if (salaryTypeId) {
      const selectedSalaryType = this.employeeSalaryTypes.find(st => st.id.toString() === salaryTypeId);
      if (selectedSalaryType) {
        const description = selectedSalaryType.description?.toLowerCase() || '';
        if (description.includes('hourly') || description.includes('hour')) {
          filteredPositions = this.positions.filter(p => p.type === 'N' || p.type === 'U');
        } else if (description.includes('salaried') || description.includes('salary')) {
          filteredPositions = this.positions.filter(p => p.type === 'E');
        }
      }
    }

    this.positionsForDropdown = filteredPositions.map(position => ({
      ...position,
      displayText: `${position.positionCode} - ${position.positionName}`
    }));

    // Clear position selection since the filtered list may no longer contain the previously selected value
    // Skip clearing during form population (isUpdatingForm) - populatePositionInfo will set the correct value
    if (!this.isUpdatingForm) {
      this.newHireForm.get('positionInfo.position')?.setValue('');
    }
  }

  private handleEmailRequiredChange(value: string) {
    const itInfo = this.newHireForm.get('itInfo');

    // If email not required, clear Microsoft License field
    if (value !== 'yes') {
      itInfo?.get('microsoftLicense')?.setValue('');
    } else {
      // If email is required and company is not Mathy Construction (19), auto-select F3 license
      if (this.shouldAutoSelectF3License()) {
        itInfo?.get('microsoftLicense')?.setValue('f3');
      }
    }

    // Re-generate email address based on the new emailRequired selection
    this.generateEmailAddress();
  }

  private shouldAutoSelectF3License(): boolean {
    const companyCode = this.newHireForm.get('positionInfo.company')?.value;
    const emailRequired = this.newHireForm.get('itInfo.emailRequired')?.value;

    // Auto-select F3 license if company is NOT Mathy Construction (19) AND email is required
    return companyCode && companyCode !== '19' && emailRequired === 'yes';
  }

  private handlePhoneTypeChange(phoneTypeIndex: number, isChecked: boolean) {
    // Prevent infinite loops during form updates
    if (this.isUpdatingForm) {
      return;
    }

    const phoneTypesArray = this.newHireForm.get('itInfo.phoneTypes') as FormArray;

    if (!phoneTypesArray || !isChecked) {
      return; // No action needed if unchecking or array doesn't exist
    }

    // Set flag to prevent recursive calls
    this.isUpdatingForm = true;

    try {
      // Company Cell Phone (index 1) and BYOD Cell Phone (index 2) are mutually exclusive
      if (phoneTypeIndex === 1) {
        // Company Cell Phone checked → uncheck BYOD Cell Phone
        phoneTypesArray.at(2)?.setValue(false, { emitEvent: false });
      } else if (phoneTypeIndex === 2) {
        // BYOD Cell Phone checked → uncheck Company Cell Phone
        phoneTypesArray.at(1)?.setValue(false, { emitEvent: false });
      }
    } finally {
      // Always reset the flag, even if an error occurs
      this.isUpdatingForm = false;
    }
  }

  // Handler for First Day of Employment blur event
  onFirstDayBlur(): void {
    this.checkFirstDayOfEmploymentWarning();
  }

  // First Day of Employment warning - Show warning only for past dates (less than 7 days from today)
  private checkFirstDayOfEmploymentWarning(): void {
    const firstDayControl = this.newHireForm.get('personalInfo.firstDay');
    if (!firstDayControl || !firstDayControl.value) {
      return;
    }

    // Skip warnings when viewing/editing existing request (isEditMode = true)
    if (this.isEditMode) {
      return;
    }

    const dateValue = firstDayControl.value;

    // Prevent duplicate warnings for the same date value
    if (this.lastWarnedEmploymentDate === dateValue) {
      return;
    }

    // Parse the date string from the input
    const selectedDate = new Date(dateValue);

    // Validate the date is reasonable (not more than 10 years in the future)
    const maxReasonableDate = new Date();
    maxReasonableDate.setFullYear(maxReasonableDate.getFullYear() + 10);
    if (selectedDate > maxReasonableDate || isNaN(selectedDate.getTime())) {
      return; // Skip warning for invalid or unreasonable dates
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0); // Set to start of today for accurate comparison

    // Calculate days difference from today
    // Negative = past dates, 0 = today, Positive = future dates
    const daysDifference = Math.floor((selectedDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

    // Show "Hot Potato" warning for upcoming dates less than 3 days away (0 to 2 days in the future)
    if (daysDifference >= 0 && daysDifference < 3) {
      this.lastWarnedEmploymentDate = dateValue;
      this.toasterService.showWarning(
        'A New Hire is starting in less than three days.',
        '',
        10000 // Display warning for 10 seconds
      );
    }
    // Show warning only for past dates (daysDifference < 0)
    // Do not show warning for today (daysDifference = 0) or any future dates (daysDifference > 0)
    else if (daysDifference < 0) {
      this.lastWarnedEmploymentDate = dateValue;
      this.toasterService.showWarning(
        'The first day of employment is less than 7 days from today! Due to short notice, many onboarding tasks may not be completed before the new hire arrives!',
        'Short Notice Warning',
        10000 // Display warning for 10 seconds
      );
    }
  }

  // Tab navigation
  showTab(tabName: 'personal' | 'it') {
    this.activeTab = tabName;
    // Scroll to top of page when switching tabs
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // Dynamic form array methods
  get applicationSoftwareArray(): FormArray {
    return this.newHireForm.get('applicationSoftware') as FormArray;
  }

  get folderSharepointArray(): FormArray {
    return this.newHireForm.get('folderSharepoint') as FormArray;
  }

  addApplicationRow() {
    this.applicationSoftwareArray.push(this.createApplicationSoftwareGroup());
  }

  removeApplicationRow(index: number) {
    if (this.applicationSoftwareArray.length > 1) {
      this.applicationSoftwareArray.removeAt(index);
    }
  }

  addFolderSharepointRow() {
    this.folderSharepointArray.push(this.createFolderSharepointGroup());
  }

  removeFolderSharepointRow(index: number) {
    if (this.folderSharepointArray.length > 1) {
      this.folderSharepointArray.removeAt(index);
    }
  }

  // Getter methods for conditional display
  get showUnionFields(): boolean {
    return this.isUnionCompany;
  }

  get isUnionYes(): boolean {
    return this.newHireForm.get('positionInfo.union')?.value === 'yes';
  }

  private updateAvailableRoles(company: string) {
    
    if (company === '19') { // Mathy Construction Company
      this.availableRoles = [
        { value: 'None', label: 'None' },
        { value: 'Cargas App', label: 'Cargas App' },
        { value: 'Data Collection App', label: 'Data Collection App' },
        { value: 'Milestone Agg. Production App', label: 'Milestone Agg. Production App' },
        { value: 'XRS App', label: 'XRS App' }
      ];
    } else if (company === '88') { // Pavement Materials LLC
      this.availableRoles = [
        { value: 'None', label: 'None' },
        { value: 'Solar Connection', label: 'Solar Connection' },
        { value: "Todd's Redi-Mix", label: "Todd's Redi-Mix" },
        { value: 'XRS App', label: 'XRS App' }
      ];
    } else {
      // Default/fallback - show no roles or basic set
      this.availableRoles = [
        { value: 'None', label: 'None' }
      ];
    }
    
    
  }

  private updateAvailableRolesFromTabletProfiles() {

    if (this.tabletProfiles && this.tabletProfiles.length > 0) {
      // Start with empty array - backend should provide all options including 'None' if needed
      this.availableRoles = [];

      // Add tablet profile names as available roles
      this.tabletProfiles.forEach(profile => {
        if (profile.isActive) {
          this.availableRoles.push({
            value: profile.profileName,
            label: profile.profileName
          });
        }
      });

      // Sort: 'None' first, then alphabetically
      this.availableRoles.sort((a, b) => {
        // If 'a' is 'None', it should come first
        if (a.value === 'None') return -1;
        // If 'b' is 'None', it should come first
        if (b.value === 'None') return 1;
        // Otherwise, sort alphabetically
        return a.value.localeCompare(b.value);
      });

      // Set 'None' as default selection if it exists in available roles
      // BUT only for new requests, not when loading existing data (edit mode)
      if (!this.isEditMode || !this.viewData) {
        const hasNoneOption = this.availableRoles.some(role => role.value === 'None');
        if (hasNoneOption) {
          const itInfoGroup = this.newHireForm.get('itInfo') as FormGroup;
          if (itInfoGroup) {
            itInfoGroup.get('rolesRequiredNewHires')?.setValue('None');
          }
        }
      }
    } else {
      // No tablet profiles available
      this.availableRoles = [];
    }

  }


  private recreateBuildingAccessFormArray() {
    // Get current form array before replacement to preserve values in edit mode
    const currentBuildingAccessArray = this.newHireForm.get('buildingAccess') as FormArray;
    let currentValues: boolean[] = [];

    if (currentBuildingAccessArray) {
      currentValues = currentBuildingAccessArray.controls.map(c => c.value);
    }

    // Create new FormArray with controls for each building access option
    // In edit mode, try to preserve existing values if the array size matches
    const buildingAccessControls = this.availableBuildingAccess.map((_, index) => {
      // If we're in edit mode and have existing values, preserve them
      if (this.isEditMode && currentValues.length > 0 && index < currentValues.length) {
        return this.fb.control(currentValues[index]);
      }
      return this.fb.control(false);
    });

    const newFormArray = this.fb.array(buildingAccessControls);

    // Replace the existing FormArray
    this.newHireForm.setControl('buildingAccess', newFormArray);

    // Re-subscribe to valueChanges each time the FormArray is recreated.
    // Using valueChanges (fires after form control is updated) avoids the timing
    // conflict that (change) event has with CheckboxControlValueAccessor.
    this.buildingAccessDestroy$.next(); // cancel previous subscription
    newFormArray.valueChanges
      .pipe(takeUntil(this.buildingAccessDestroy$), takeUntil(this.destroy$))
      .subscribe((values: (boolean | null)[]) => {
        if (values.some(v => v === true) && this.newHireForm.get('useExistingKeyFob')?.value === true) {
          this.newHireForm.get('useExistingKeyFob')?.setValue(false, { emitEvent: false });
        }
      });
  }

  private useSampleComputerRequirementsData() {
    // Sample data for testing based on mockup
    this.computerRequirements = [
      // Parent requirements (IsChild = false/0)
      { 
        id: 1, 
        description: 'Desktop PC includes Monitor, Keyboard, Mouse, Webcam', 
        isChild: false, 
        parentId: undefined, 
        isActive: true, 
        createdBy: 1, 
        createdDate: new Date().toISOString(), 
        modifiedBy: undefined, 
        modifiedDate: undefined 
      },
      { 
        id: 2, 
        description: 'Laptop', 
        isChild: false, 
        parentId: undefined, 
        isActive: true, 
        createdBy: 1, 
        createdDate: new Date().toISOString(), 
        modifiedBy: undefined, 
        modifiedDate: undefined 
      },
      { 
        id: 3, 
        description: 'Will Repurpose Existing Computer Equipment', 
        isChild: false, 
        parentId: undefined, 
        isActive: true, 
        createdBy: 1, 
        createdDate: new Date().toISOString(), 
        modifiedBy: undefined, 
        modifiedDate: undefined 
      },
      // Child requirements for Laptop (IsChild = true/1, ParentId = 2)
      { 
        id: 4, 
        description: 'Docking Station, Monitor, Keyboard & Mouse ($600)', 
        isChild: true, 
        parentId: 2, 
        isActive: true, 
        createdBy: 1, 
        createdDate: new Date().toISOString(), 
        modifiedBy: undefined, 
        modifiedDate: undefined 
      },
      { 
        id: 5, 
        description: 'Additional Monitor ($200)', 
        isChild: true, 
        parentId: 2, 
        isActive: true, 
        createdBy: 1, 
        createdDate: new Date().toISOString(), 
        modifiedBy: undefined, 
        modifiedDate: undefined 
      },
      { 
        id: 6, 
        description: 'Web Camera ($100)', 
        isChild: true, 
        parentId: 2, 
        isActive: true, 
        createdBy: 1, 
        createdDate: new Date().toISOString(), 
        modifiedBy: undefined, 
        modifiedDate: undefined 
      },
      { 
        id: 7, 
        description: 'Laptop Bag ($30)', 
        isChild: true, 
        parentId: 2, 
        isActive: true, 
        createdBy: 1, 
        createdDate: new Date().toISOString(), 
        modifiedBy: undefined, 
        modifiedDate: undefined 
      },
      { 
        id: 8, 
        description: 'Mouse ($10)', 
        isChild: true, 
        parentId: 2, 
        isActive: true, 
        createdBy: 1, 
        createdDate: new Date().toISOString(), 
        modifiedBy: undefined, 
        modifiedDate: undefined 
      }
    ];
    
    this.organizeComputerRequirements();
    this.initializeComputerEquipmentControl();

    // Default to 'None' in create mode (already set in form initialization)
  }

  private organizeComputerRequirements() {
    // Separate parent and child requirements - handle both boolean and bit values
    this.parentComputerRequirements = this.computerRequirements.filter(req => 
      req.isChild === false || req.isChild === 0 || req.isChild == null);
    
    // Group child requirements by parent ID - handle both boolean and bit values
    this.childComputerRequirements.clear();
    const childRequirements = this.computerRequirements
      .filter(req => (req.isChild === true || req.isChild === 1) && req.parentId);
    
    childRequirements.forEach(child => {
      const parentId = child.parentId!;
      if (!this.childComputerRequirements.has(parentId)) {
        this.childComputerRequirements.set(parentId, []);
      }
      this.childComputerRequirements.get(parentId)!.push(child);
    });
  }

  private initializeComputerEquipmentControl() {
    // Initialize FormControl for radio button selection (no need to recreate)
    // The FormControl is already initialized in the form structure
  }

  // Computer Requirements helper methods
  onParentComputerRequirementChange(requirement: ComputerRequirementDto) {
    // Update FormControl value
    const computerEquipmentControl = this.newHireForm.get('itInfo.computerEquipment');
    if (computerEquipmentControl) {
      computerEquipmentControl.setValue(requirement.id);
    }

    // Clear all child requirements since we're switching parents
    this.clearAllChildRequirements();

    // Default check all child checkboxes for Desktop PC
    if (requirement.description === 'Desktop PC') {
      const children = this.getChildRequirements(requirement.id);
      children.forEach(child => {
        const key = `${requirement.id}_${child.id}`;
        this.selectedChildRequirements[key] = true;
      });
    }
  }

  onChildComputerRequirementChange(event: any, parentId: number, childId: number) {
    const isChecked = event.target.checked;
    const key = `${parentId}_${childId}`;
    this.selectedChildRequirements[key] = isChecked;
  }

  isComputerEquipmentNoneSelected(): boolean {
    const computerEquipmentControl = this.newHireForm.get('itInfo.computerEquipment');
    return computerEquipmentControl?.value === 'none';
  }

  isParentSelected(parentId: number): boolean {
    const computerEquipmentControl = this.newHireForm.get('itInfo.computerEquipment');
    const controlValue = computerEquipmentControl?.value;
    return controlValue == parentId; // Use == for type coercion (string/number comparison)
  }

  getChildRequirements(parentId: number): ComputerRequirementDto[] {
    return this.childComputerRequirements.get(parentId) || [];
  }

  getChildControlValue(parentId: number, childId: number): boolean {
    const key = `${parentId}_${childId}`;
    return this.selectedChildRequirements[key] || false;
  }

  private clearChildRequirements(parentId: number) {
    const childRequirements = this.getChildRequirements(parentId);
    childRequirements.forEach(child => {
      const key = `${parentId}_${child.id}`;
      this.selectedChildRequirements[key] = false;
    });
  }

  private clearAllChildRequirements() {
    // Clear all child requirements when switching parent selection
    this.selectedChildRequirements = {};
  }

  get showApprenticeDropdown(): boolean {
    return this.newHireForm.get('positionInfo.apprentice')?.value === 'yes';
  }

  get showExpenseCardFields(): boolean {
    return this.newHireForm.get('creditCardInfo.companyExpenseCard')?.value === 'yes';
  }

  get showCardlockShipAddress(): boolean {
    return this.newHireForm.get('creditCardInfo.fuelCardlockAccess')?.value === 'yes';
  }

  get showVehicleFields(): boolean {
    return this.newHireForm.get('vehicleInfo.approvedVehicle')?.value === 'yes';
  }


  shouldShowRoleTextbox(roleValue: string): boolean {
    if (roleValue === 'None') {
      return false;
    }
    
    const selectedRole = this.newHireForm.get('itInfo.rolesRequiredNewHires')?.value;
    const shouldShow = selectedRole === roleValue;
    
    return shouldShow;
  }



  getRoleTextboxControlName(roleValue: string): string {
    const controlMap: {[key: string]: string} = {
      'Cargas App': 'cargasAppRole',
      'Data Collection App': 'dataCollectionRole', 
      'Milestone Agg. Production App': 'milestoneRole',
      'XRS App': 'xrsRole',
      'Solar Connection': 'solarConnectionRole',
      "Todd's Redi-Mix": 'toddsRediMixRole'
    };
    return controlMap[roleValue] || '';
  }

  getRoleId(roleValue: string): string {
    return 'role_' + roleValue.replace(/[\s']/g, '');
  }

  get selectedPhysicalLocation(): string {
    const locationValue = this.newHireForm.get('positionInfo.physicalLocation')?.value;

    // If no location is selected, return '-'
    if (!locationValue) {
      return '-';
    }

    // If location is selected, try to find the display name
    if (this.physicalLocationsForDropdown.length > 0) {
      const location = this.physicalLocationsForDropdown.find(loc =>
        loc.locationCode.toString() === locationValue.toString()
      );
      return location ? location.locationName : '-';
    }

    return '-';
  }

  // Building Access Column Distribution
  get buildingAccessColumns(): string[][] {
    // In draft edit mode, show all available items (like create mode)
    if (this.isEditMode && this.isDraftRequest && this.isFormEditable) {
      const totalItems = this.availableBuildingAccess.length;
      const itemsPerColumn = Math.ceil(totalItems / 3);

      const columns: string[][] = [[], [], []];

      for (let i = 0; i < totalItems; i++) {
        const columnIndex = Math.floor(i / itemsPerColumn);
        if (columnIndex < 3) {
          columns[columnIndex].push(this.availableBuildingAccess[i]);
        }
      }

      return columns;
    }

    // In regular view mode, show only selected items
    if (this.isEditMode) {
      const selectedItems = this.getSelectedBuildingAccessItems();
      const totalItems = selectedItems.length;
      const itemsPerColumn = Math.ceil(totalItems / 3);

      const columns: string[][] = [[], [], []];

      for (let i = 0; i < totalItems; i++) {
        const columnIndex = Math.floor(i / itemsPerColumn);
        if (columnIndex < 3) {
          columns[columnIndex].push(selectedItems[i]);
        }
      }

      return columns;
    }

    // In create mode, show all available items
    const totalItems = this.availableBuildingAccess.length;
    const itemsPerColumn = Math.ceil(totalItems / 3);

    const columns: string[][] = [[], [], []];

    for (let i = 0; i < totalItems; i++) {
      const columnIndex = Math.floor(i / itemsPerColumn);
      if (columnIndex < 3) {
        columns[columnIndex].push(this.availableBuildingAccess[i]);
      }
    }

    return columns;
  }

  // Get only the selected building access items for view mode
  private getSelectedBuildingAccessItems(): string[] {
    const buildingAccessArray = this.newHireForm.get('buildingAccess') as FormArray;
    if (!buildingAccessArray) {
      return [];
    }

    const selectedItems: string[] = [];
    buildingAccessArray.controls.forEach((control, index) => {
      if (control.value === true && this.availableBuildingAccess[index]) {
        selectedItems.push(this.availableBuildingAccess[index]);
      }
    });

    return selectedItems;
  }

  getItemFormIndex(columnIndex: number, itemIndex: number): number {
    // In draft edit mode, use create mode calculation
    if (this.isEditMode && this.isDraftRequest && this.isFormEditable) {
      const totalItems = this.availableBuildingAccess.length;
      const itemsPerColumn = Math.ceil(totalItems / 3);

      return (columnIndex * itemsPerColumn) + itemIndex;
    }

    // In regular view mode, map the filtered display back to the original form array index
    if (this.isEditMode) {
      const selectedItems = this.getSelectedBuildingAccessItems();
      const selectedItemsPerColumn = Math.ceil(selectedItems.length / 3);
      const displayIndex = (columnIndex * selectedItemsPerColumn) + itemIndex;

      if (displayIndex < selectedItems.length) {
        const itemName = selectedItems[displayIndex];
        // Find the original index in availableBuildingAccess
        return this.availableBuildingAccess.indexOf(itemName);
      }
      return 0;
    }

    // In create mode, use the normal calculation
    const totalItems = this.availableBuildingAccess.length;
    const itemsPerColumn = Math.ceil(totalItems / 3);

    return (columnIndex * itemsPerColumn) + itemIndex;
  }

  trackByIndex(index: number): number {
    return index;
  }

  trackByBuildingAccessItem(index: number, item: string): string {
    return item;
  }

  // Method to track building access checkbox changes (kept for any future use)
  onBuildingAccessChange(columnIndex: number, itemIndex: number, event: any): void {
  }

  onUseExistingKeyFobChange(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    if (checkbox.checked) {
      const buildingAccessArray = this.newHireForm.get('buildingAccess') as FormArray;
      if (buildingAccessArray) {
        buildingAccessArray.controls.forEach(control => control.setValue(false, { emitEvent: false }));
      }
    }
  }

  private getItemNameByColumnAndIndex(columnIndex: number, itemIndex: number): string {
    // Get the item name based on display mode
    if (this.isEditMode && this.isDraftRequest && this.isFormEditable) {
      // In draft edit mode, use create mode calculation
      const totalItems = this.availableBuildingAccess.length;
      const itemsPerColumn = Math.ceil(totalItems / 3);
      const actualIndex = (columnIndex * itemsPerColumn) + itemIndex;

      return this.availableBuildingAccess[actualIndex] || 'Unknown';
    }

    if (this.isEditMode) {
      // In view mode, get from selected items
      const selectedItems = this.getSelectedBuildingAccessItems();
      const selectedItemsPerColumn = Math.ceil(selectedItems.length / 3);
      const displayIndex = (columnIndex * selectedItemsPerColumn) + itemIndex;

      return selectedItems[displayIndex] || 'Unknown';
    }

    // In create mode
    const totalItems = this.availableBuildingAccess.length;
    const itemsPerColumn = Math.ceil(totalItems / 3);
    const actualIndex = (columnIndex * itemsPerColumn) + itemIndex;

    return this.availableBuildingAccess[actualIndex] || 'Unknown';
  }

  // Helper method for timezone-safe date parsing
  private parseFormDateSafely(dateString: string): Date {
    if (!dateString) return new Date();

    // Parse YYYY-MM-DD format safely without timezone conversion
    const parts = dateString.split('-');
    if (parts.length === 3) {
      const year = parseInt(parts[0], 10);
      const month = parseInt(parts[1], 10) - 1; // Month is 0-indexed in JavaScript Date
      const day = parseInt(parts[2], 10);
      return new Date(year, month, day);
    }

    // Fallback to original behavior if format is unexpected
    return new Date(dateString);
  }

  // Helper method for timezone-safe date-to-string conversion for form inputs
  private formatDateForInput(dateValue: any): string {
    if (!dateValue) return '';

    // If it's a Date object, format it safely
    if (dateValue instanceof Date) {
      const year = dateValue.getFullYear();
      const month = (dateValue.getMonth() + 1).toString().padStart(2, '0');
      const day = dateValue.getDate().toString().padStart(2, '0');
      return `${year}-${month}-${day}`;
    }

    // If it's a string, try to extract date part safely
    const dateStr = dateValue.toString();
    if (dateStr.includes('T')) {
      return dateStr.split('T')[0];
    }

    return dateStr;
  }

  // Helper method to convert Date to timezone-neutral Date object for API submission
  private toTimezoneNeutralDate(date: Date): Date {
    // Create a new Date object using UTC constructor to avoid timezone shifts
    const year = date.getFullYear();
    const month = date.getMonth(); // Month is 0-indexed, keep as-is for Date constructor
    const day = date.getDate();

    // Create Date in UTC to ensure no timezone conversion during JSON serialization
    return new Date(Date.UTC(year, month, day, 0, 0, 0, 0));
  }

  // Form submission
  onSubmit() {

    if (this.newHireForm.valid) {
      this.isLoading = true;

      try {
        const newHireRequest = this.transformFormToNewHireRequest();

        // Determine which service method to call based on mode
        const serviceCall = this.isDraftRequest && this.parentId
          ? this.hrRequestService.updateNewHireRequest(this.parentId, newHireRequest)
          : this.hrRequestService.createNewHireRequest(newHireRequest);

        serviceCall.subscribe({
          next: (response) => {
            if (response.success) {
              const message = this.isDraftRequest
                ? 'New hire request submitted successfully!'
                : 'New hire request submitted successfully!';
              this.toasterService.showSuccess(message);
              this.isLoading = false;
              this.goBack();
            } else {
              this.toasterService.showError(response.message || 'Failed to submit new hire request');
              this.isLoading = false;
            }
          },
          error: (error) => {
            console.error('Error submitting new hire request:', error);
            console.error('Error response body:', error.error);
            console.error('Error status:', error.status);
            console.error('Error message:', error.message);

            if (error.error && error.error.errors) {
              console.error('Validation errors:', error.error.errors);
              Object.keys(error.error.errors).forEach(key => {
                console.error(`Validation error for ${key}:`, error.error.errors[key]);
              });
            }

            let errorMessage = 'An error occurred while submitting the request';
            if (error.error && error.error.errors) {
              // Format validation errors for user display
              const validationErrors: string[] = [];
              Object.keys(error.error.errors).forEach(key => {
                const errors = error.error.errors[key];
                if (Array.isArray(errors)) {
                  validationErrors.push(`${key}: ${errors.join(', ')}`);
                } else {
                  validationErrors.push(`${key}: ${errors}`);
                }
              });
              errorMessage = `Validation errors:\n${validationErrors.join('\n')}`;
            } else if (error.error && error.error.message) {
              errorMessage = error.error.message;
            } else if (error.error && typeof error.error === 'string') {
              errorMessage = error.error;
            }

            this.toasterService.showError(errorMessage);
            this.isLoading = false;
          }
        });
      } catch (error) {
        console.error('Error transforming form data:', error);
        this.toasterService.showError('An error occurred while processing the form data');
        this.isLoading = false;
      }
    } else {
      this.markFormGroupTouched(this.newHireForm);
      this.toasterService.showError('Please fill in all required fields.');
    }
  }

  private transformFormToNewHireRequest(): CreateNewHireRequest {
    const formValue = this.newHireForm.value;

    // Debug logging for form values
    // console.log('Form personalInfo:', formValue.personalInfo);
    // console.log('Form positionInfo:', formValue.positionInfo);

    const newHireRequest: CreateNewHireRequest = {
      personalInfo: {
        employeeId: null,
        firstName: formValue.personalInfo.firstName || '',
        lastName: formValue.personalInfo.lastName || '',
        middleInitial: formValue.personalInfo.middleInitial || null,
        suffix: formValue.personalInfo.suffix || null,
        preferredFirstName: formValue.personalInfo.preferredName || null,
        userId: formValue.personalInfo.userId || null,
        firstDayEmployment: formValue.personalInfo.firstDay ?
          this.toTimezoneNeutralDate(this.parseFormDateSafely(formValue.personalInfo.firstDay)) :
          this.toTimezoneNeutralDate(new Date()),
        referredBy: formValue.personalInfo.referredBy || null,
        rehire: formValue.personalInfo.rehire === 'yes'
      },
      positionInfo: {
        companyCode: parseInt(formValue.positionInfo.company),
        locationCode: parseInt(formValue.positionInfo.physicalLocation),
        employmentStatus: formValue.positionInfo.employmentStatus || '',
        isUnion: formValue.positionInfo.union === 'yes' ? true : formValue.positionInfo.union === 'no' ? false : null,
        unionCraftId: formValue.positionInfo.unionCraft ? parseInt(formValue.positionInfo.unionCraft) : null,
        isApprentice: formValue.positionInfo.apprentice === 'yes' ? true : formValue.positionInfo.apprentice === 'no' ? false : null,
        isUnionWage: formValue.positionInfo.unionWage === 'yes' ? true : formValue.positionInfo.unionWage === 'no' ? false : null,
        salaryCode: this.getEmployeeSalaryCode(formValue.positionInfo.salaryCode),
        positionCode: this.getPositionCode(formValue.positionInfo.position) || '',
        payrollDeptCode: this.getPayrollDeptCode(formValue.positionInfo.payrollCode),
        supervisorId: this.getSupervisorEmployeeNumber(formValue.positionInfo.supervisor),
        appPercentage: this.getApprenticePercentage(formValue.positionInfo.apprenticeDropdown) || ''
      }
    };

    // Add IT Information - always include since Other Info section is always visible
    newHireRequest.itInfo = {
      emailRequired: formValue.itInfo.emailRequired === 'yes',
      alternateDeliveryLocation: formValue.itInfo.deliveryNote || null,
      msOfficeLicenseE5: false,
      msOfficeLicenseF3: false,
      emailAddress: formValue.itInfo.emailAddress?.trim() || null
    };

    // Add Microsoft License information if email is required
    if (this.showMicrosoftLicenseSection && newHireRequest.itInfo) {
      if (formValue.itInfo.microsoftLicense === 'e5') {
        newHireRequest.itInfo.msOfficeLicenseE5 = true;
      } else if (formValue.itInfo.microsoftLicense === 'f3') {
        newHireRequest.itInfo.msOfficeLicenseF3 = true;
      }
    }

    // Add Credit Card Information - always include this section
    newHireRequest.creditCardInfo = {
      kwikTripCard: formValue.creditCardInfo?.kwikTripCard === 'yes',
      companyExpenseCard: formValue.creditCardInfo?.companyExpenseCard === 'yes',
      creditExpenseType: formValue.creditCardInfo?.creditExpenseType || null,
      weeklyLimit: formValue.creditCardInfo?.weeklyLimit ? parseFloat(formValue.creditCardInfo.weeklyLimit) : null,
      fuelCardlockAccess: formValue.creditCardInfo?.fuelCardlockAccess === 'yes',
      fuelCardlockAddress: formValue.creditCardInfo?.cardlockShipAddress || null
    };

    // Add Vehicle Information - always include this section
    newHireRequest.vehicleInfo = {
      isApprovedToOperate: formValue.vehicleInfo?.approvedVehicle === 'yes',
      driverClassification: this.getDriverClassificationText(formValue.vehicleInfo?.driverClassification),
      drugAndAlcoholProfile: formValue.vehicleInfo?.drugAlcoholProfile || null,
      needCompanyCar: formValue.vehicleInfo?.companyCarNeeded === 'yes',
      isApplicationPart2Complete: formValue.vehicleInfo?.applicationPart2 === 'yes'
    };

    // Add Phone Information - always include this section
    const phoneTypes = formValue.itInfo?.phoneTypes || [];
    newHireRequest.phoneInfo = {
      deskPhone: phoneTypes[0] || false,
      companyCellphone: phoneTypes[1] || false,
      byodCellphone: phoneTypes[2] || false,
      workPhoneNumber: formValue.itInfo?.workPhoneNumber || null,
      workExtension: formValue.itInfo?.workExtension || null,
      reusingExistingPhone: formValue.itInfo?.reusingPhone === 'yes'
    };

    // Add Applications - always include as empty array if none
    const applicationArray = formValue.applicationSoftware || [];
    newHireRequest.applications = applicationArray
      .filter((app: any) => app.applicationSoftware)
      .map((app: any) => ({
        applicationId: parseInt(app.applicationSoftware),
        accessNotes: app.applicationAccessNote || null
      })) || [];

    // Add Folders - always include as empty array if none
    const folderArray = formValue.folderSharepoint || [];
    newHireRequest.folders = folderArray
      .filter((folder: any) => folder.type || folder.folderSharepointMailbox)
      .map((folder: any) => ({
        folderType: folder.type || 'Folder',
        folderName: folder.folderSharepointMailbox
      })) || [];

    // Add Tablet Profiles - always include as empty array
    const rolesRequired = formValue.itInfo?.rolesRequiredNewHires;
    newHireRequest.tabletProfiles = [];
    if (rolesRequired) {
      const selectedTabletProfile = this.tabletProfiles.find(profile => profile.profileName === rolesRequired);
      if (selectedTabletProfile) {
        newHireRequest.tabletProfiles = [{
          tabletProfileId: selectedTabletProfile.id,
          tabletProfileName: selectedTabletProfile.profileName,
          rolesRequiredForNewHire: this.getRoleTextValue(rolesRequired, formValue) || ''
        }];
      }
    }

    // Add Computer Requirements - always include as empty array
    const computerEquipmentId = formValue.itInfo?.computerEquipment;
    newHireRequest.computerRequirements = [];
    if (computerEquipmentId && computerEquipmentId !== 'none') {
      newHireRequest.computerRequirements = [{
        computerRequirementsId: parseInt(computerEquipmentId),
        isChild: false,
        parentId: null
      }];

      // Add selected child requirements
      Object.keys(this.selectedChildRequirements).forEach(key => {
        if (this.selectedChildRequirements[key]) {
          const [parentId, childId] = key.split('_').map(id => parseInt(id));
          if (parentId === parseInt(computerEquipmentId)) {
            newHireRequest.computerRequirements!.push({
              computerRequirementsId: childId,
              isChild: true,
              parentId: parentId
            });
          }
        }
      });
    }

    // Add Building Access - always include as empty array
    const buildingAccessArray = formValue.buildingAccess || [];

    // Check if buildingAccessArray is a boolean array (form controls) or already object array
    if (Array.isArray(buildingAccessArray) && buildingAccessArray.length > 0) {
      if (typeof buildingAccessArray[0] === 'boolean') {
        // Process as boolean array (form controls)
        const buildingAccessResults: { accessId: number; accessDescription: string }[] = [];

        buildingAccessArray.forEach((isSelected: boolean, index: number) => {
          if (isSelected) {
            // First, try to get the sorted description if available
            if (this.availableBuildingAccess && this.availableBuildingAccess[index]) {
              const sortedDescription = this.availableBuildingAccess[index];
              const requirement = this.buildingAccessRequirements.find(req =>
                req.description === sortedDescription
              );

              if (requirement) {
                buildingAccessResults.push({
                  accessId: requirement.id,
                  accessDescription: requirement.description
                });
                return;
              }
            }

            // Fallback: try to find by original array index if sorted matching fails
            if (this.buildingAccessRequirements && this.buildingAccessRequirements[index]) {
              const fallbackRequirement = this.buildingAccessRequirements[index];
              buildingAccessResults.push({
                accessId: fallbackRequirement.id,
                accessDescription: fallbackRequirement.description
              });
              return;
            }

          }
        });

        newHireRequest.buildingAccess = buildingAccessResults;
      } else {
        // Already in object format, just ensure accessId is present
        const objectAccessResults: { accessId: number; accessDescription: string }[] = [];

        buildingAccessArray.forEach((access: any) => {
          if (access.accessId) {
            objectAccessResults.push({
              accessId: access.accessId,
              accessDescription: access.accessDescription || ''
            });
          } else if (access.accessDescription) {
            // Find the accessId by description
            const requirement = this.buildingAccessRequirements.find(req =>
              req.description === access.accessDescription
            );
            if (requirement) {
              objectAccessResults.push({
                accessId: requirement.id,
                accessDescription: requirement.description
              });
            } else {
            }
          }
        });

        newHireRequest.buildingAccess = objectAccessResults;
      }
    } else {
      newHireRequest.buildingAccess = [];
    }

    // Emergency fallback if our logic fails
    if (newHireRequest.buildingAccess && newHireRequest.buildingAccess.some(access => !access.accessId)) {

      newHireRequest.buildingAccess = newHireRequest.buildingAccess.map((access, index) => {
        if (!access.accessId && access.accessDescription) {
          // Try to find accessId by description
          const requirement = this.buildingAccessRequirements.find(req =>
            req.description === access.accessDescription
          );
          if (requirement) {
            return {
              accessId: requirement.id,
              accessDescription: requirement.description
            };
          }
        }
        return access;
      }); // Keep all items - the backend fix should provide accessId now
    }

    // Add useExistingKeyFob flag
    newHireRequest.useExistingKeyFob = formValue.useExistingKeyFob || false;

    // Add notes (combine various text fields) - always include notes field
    const notes = this.buildNotesField(formValue);
    newHireRequest.notes = notes || '';

    return newHireRequest;
  }

  private validateBuildingAccessBeforeSubmission(request: CreateNewHireRequest): boolean {
    if (request.buildingAccess && request.buildingAccess.length > 0) {
      for (let i = 0; i < request.buildingAccess.length; i++) {
        const access = request.buildingAccess[i];
        if (!access.accessId) {
          this.toasterService.showError('Building access data is incomplete. Please try again.');
          return false;
        }
      }
    }

    return true;
  }


  private getRoleTextValue(roleValue: string, formValue: any): string | null {
    const controlName = this.getRoleTextboxControlName(roleValue);
    if (controlName && formValue.itInfo[controlName]) {
      return formValue.itInfo[controlName];
    }
    return null;
  }

  private buildNotesField(formValue: any): string | null {
    // Return the notes field value directly from the form
    return formValue.notes || null;
  }

  private getDriverClassificationText(selectedId: number | string | null): string | null {
    if (!selectedId) return null;

    const selectedLicenseClass = this.employeeLicenseClassesForDropdown.find(
      licenseClass => licenseClass.id == selectedId
    );

    return selectedLicenseClass ? selectedLicenseClass.licenseClass : null;
  }

  private getEmployeeSalaryCode(selectedId: number | string | null): number | null {
    if (!selectedId) return null;

    const selectedSalaryType = this.employeeSalaryTypesForDropdown.find(
      salaryType => salaryType.id == selectedId
    );

    return selectedSalaryType && selectedSalaryType.salaryCode != null
      ? parseInt(selectedSalaryType.salaryCode)
      : null;
  }

  private getPositionCode(selectedId: number | string | null): string | null {
    if (!selectedId) return null;

    const selectedPosition = this.positionsForDropdown.find(
      position => position.id == selectedId
    );

    return selectedPosition ? selectedPosition.positionCode : null;
  }

  private getPayrollDeptCode(selectedId: number | string | null): number | null {
    if (!selectedId) return null;

    const selectedDept = this.payrollDepartmentsForDropdown.find(
      dept => dept.id == selectedId
    );

    return selectedDept ? selectedDept.deptCode : null;
  }

  private getSupervisorEmployeeNumber(selectedId: number | string | null): number | null {
    if (!selectedId) return null;

    // Return null if NOT FOUND option is selected
    if (selectedId == this.NOT_FOUND_SUPERVISOR.id || selectedId == this.NOT_FOUND_SUPERVISOR.id.toString()) {
      return null;
    }

    const selectedSupervisor = this.supervisorsForDropdown.find(
      supervisor => supervisor.id == selectedId
    );

    return selectedSupervisor ? selectedSupervisor.employeeNumber : null;
  }

  private getApprenticePercentage(selectedId: number | string | null): string | null {
    if (!selectedId) return null;

    const selectedApprenticePercentage = this.apprenticePercentagesForDropdown.find(
      apprenticePercentage => apprenticePercentage.id == selectedId
    );

    return selectedApprenticePercentage ? selectedApprenticePercentage.appPercentage : null;
  }

  private loadApprenticePercentagesForView(): Promise<void> {
    return new Promise((resolve, reject) => {
      // If already loaded, resolve immediately
      if (this.apprenticePercentagesForDropdown.length > 0) {
        resolve();
        return;
      }

      this.referenceDataService.getApprenticePercentagesWithCache().subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.apprenticePercentages = response.data;
            this.apprenticePercentagesForDropdown = this.apprenticePercentages.map(apprenticePercentage => ({
              ...apprenticePercentage,
              displayText: `${apprenticePercentage.appPercentage} - ${apprenticePercentage.appDescription}`
            }));
          } else {
            console.error('Failed to load apprentice percentages for view:', response.errors);
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading apprentice percentages for view:', error);
          resolve(); // Don't reject to prevent blocking the view load
        }
      });
    });
  }

  private loadEmployeeLicenseClassesForView(): Promise<void> {
    return new Promise((resolve, reject) => {
      // If already loaded, resolve immediately
      if (this.employeeLicenseClassesForDropdown.length > 0) {
        resolve();
        return;
      }

      this.referenceDataService.getEmployeeLicenseClassesWithCache().subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.employeeLicenseClasses = response.data;
            this.employeeLicenseClassesForDropdown = this.employeeLicenseClasses.map(licenseClass => ({
              ...licenseClass,
              displayText: `${licenseClass.licenseClass}${licenseClass.description ? ' - ' + licenseClass.description : ''}`
            }));
          } else {
            console.error('Failed to load employee license classes for view:', response.errors);
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading employee license classes for view:', error);
          resolve(); // Don't reject to prevent blocking the view load
        }
      });
    });
  }

  private getTabletProfileName(selectedId: number | string | null): string | null {
    if (!selectedId) return null;

    const selectedProfile = this.tabletProfiles.find(
      profile => profile.id == selectedId
    );

    return selectedProfile ? selectedProfile.profileName : null;
  }

  private markFormGroupTouched(formGroup: FormGroup) {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  /**
   * Draft-specific validation - only firstName and lastName are required
   */
  private validateForDraft(): boolean {
    const personalInfo = this.newHireForm.get('personalInfo');
    const firstName = personalInfo?.get('firstName')?.value?.trim();
    const lastName = personalInfo?.get('lastName')?.value?.trim();

    if (!firstName || !lastName) {
      this.toasterService.showError('First name and last name are required for draft saves');
      // Mark only required fields as touched for error display
      personalInfo?.get('firstName')?.markAsTouched();
      personalInfo?.get('lastName')?.markAsTouched();
      return false;
    }

    return true;
  }

  /**
   * Draft-specific form transformation - handles nullable fields properly for draft saves
   * Now includes all child records (applications, folders, tablet profiles, computer requirements, building access)
   */
  private transformFormToNewHireRequestForDraft(): CreateNewHireRequest {
    const formValue = this.newHireForm.value;

    const newHireRequest: CreateNewHireRequest = {
      personalInfo: {
        employeeId: null,
        firstName: formValue.personalInfo.firstName?.trim() || '',
        lastName: formValue.personalInfo.lastName?.trim() || '',
        middleInitial: formValue.personalInfo.middleInitial?.trim() || null,
        suffix: formValue.personalInfo.suffix?.trim() || null,
        preferredFirstName: formValue.personalInfo.preferredName?.trim() || null,
        userId: formValue.personalInfo.userId?.trim() || null,
        // For drafts, only set firstDayEmployment if actually provided
        firstDayEmployment: formValue.personalInfo.firstDay ?
          this.toTimezoneNeutralDate(this.parseFormDateSafely(formValue.personalInfo.firstDay)) :
          null,
        referredBy: formValue.personalInfo.referredBy?.trim() || null,
        rehire: formValue.personalInfo.rehire === 'yes'
      },
      positionInfo: {
        // For drafts, only set numeric fields if they have valid values
        companyCode: formValue.positionInfo.company ? parseInt(formValue.positionInfo.company) : null,
        locationCode: formValue.positionInfo.physicalLocation ? parseInt(formValue.positionInfo.physicalLocation) : null,
        employmentStatus: formValue.positionInfo.employmentStatus?.trim() || null,
        isUnion: formValue.positionInfo.union === 'yes' ? true : formValue.positionInfo.union === 'no' ? false : null,
        unionCraftId: formValue.positionInfo.unionCraft ? parseInt(formValue.positionInfo.unionCraft) : null,
        isApprentice: formValue.positionInfo.apprentice === 'yes' ? true : formValue.positionInfo.apprentice === 'no' ? false : null,
        isUnionWage: formValue.positionInfo.unionWage === 'yes' ? true : formValue.positionInfo.unionWage === 'no' ? false : null,
        salaryCode: formValue.positionInfo.salaryCode ? this.getEmployeeSalaryCode(formValue.positionInfo.salaryCode) : null,
        positionCode: formValue.positionInfo.position ? this.getPositionCode(formValue.positionInfo.position) : null,
        payrollDeptCode: this.getPayrollDeptCode(formValue.positionInfo.payrollCode),
        supervisorId: this.getSupervisorEmployeeNumber(formValue.positionInfo.supervisor),
        appPercentage: formValue.positionInfo.apprenticeDropdown || null
      },
      itInfo: {
        emailRequired: formValue.itInfo.emailRequired === 'yes',
        alternateDeliveryLocation: formValue.itInfo.deliveryNote || null,
        msOfficeLicenseE5: formValue.itInfo.microsoftLicense === 'e5',
        msOfficeLicenseF3: formValue.itInfo.microsoftLicense === 'f3',
        emailAddress: formValue.itInfo.emailAddress?.trim() || null
      },
      creditCardInfo: {
        kwikTripCard: formValue.creditCardInfo.kwikTripCard === 'yes',
        companyExpenseCard: formValue.creditCardInfo.companyExpenseCard === 'yes',
        creditExpenseType: formValue.creditCardInfo.creditExpenseType || null,
        weeklyLimit: formValue.creditCardInfo.weeklyLimit ? parseFloat(formValue.creditCardInfo.weeklyLimit) : null,
        fuelCardlockAccess: formValue.creditCardInfo.fuelCardlockAccess === 'yes',
        fuelCardlockAddress: formValue.creditCardInfo.cardlockShipAddress?.trim() || null
      },
      vehicleInfo: {
        isApprovedToOperate: formValue.vehicleInfo.approvedVehicle === 'yes',
        driverClassification: this.getDriverClassificationText(formValue.vehicleInfo?.driverClassification),
        drugAndAlcoholProfile: formValue.vehicleInfo.drugAlcoholProfile?.trim() || null,
        needCompanyCar: formValue.vehicleInfo.companyCarNeeded === 'yes',
        isApplicationPart2Complete: formValue.vehicleInfo.applicationPart2 === 'yes'
      },
      phoneInfo: {
        deskPhone: formValue.itInfo.phoneTypes?.[0] === true,
        companyCellphone: formValue.itInfo.phoneTypes?.[1] === true,
        byodCellphone: formValue.itInfo.phoneTypes?.[2] === true,
        workPhoneNumber: formValue.itInfo.workPhoneNumber?.trim() || null,
        workExtension: formValue.itInfo.workExtension?.trim() || null,
        reusingExistingPhone: formValue.itInfo.reusingPhone === 'yes'
      },
      applications: [],
      folders: [],
      tabletProfiles: [],
      computerRequirements: [],
      buildingAccess: [],
      notes: formValue.notes?.trim() || null
    };

    // Add Applications
    const applicationArray = formValue.applicationSoftware || [];
    newHireRequest.applications = applicationArray
      .filter((app: any) => app.applicationSoftware)
      .map((app: any) => ({
        applicationId: parseInt(app.applicationSoftware),
        accessNotes: app.applicationAccessNote || null
      })) || [];

    // Add Folders
    const folderArray = formValue.folderSharepoint || [];
    newHireRequest.folders = folderArray
      .filter((folder: any) => folder.type || folder.folderSharepointMailbox)
      .map((folder: any) => ({
        folderType: folder.type || 'Folder',
        folderName: folder.folderSharepointMailbox
      })) || [];

    // Add Tablet Profiles
    const rolesRequired = formValue.itInfo?.rolesRequiredNewHires;
    newHireRequest.tabletProfiles = [];
    if (rolesRequired) {
      const selectedTabletProfile = this.tabletProfiles.find(profile => profile.profileName === rolesRequired);
      if (selectedTabletProfile) {
        newHireRequest.tabletProfiles = [{
          tabletProfileId: selectedTabletProfile.id,
          tabletProfileName: selectedTabletProfile.profileName,
          rolesRequiredForNewHire: this.getRoleTextValue(rolesRequired, formValue) || ''
        }];
      }
    }

    // Add Computer Requirements
    const computerEquipmentId = formValue.itInfo?.computerEquipment;
    newHireRequest.computerRequirements = [];
    if (computerEquipmentId && computerEquipmentId !== 'none') {
      newHireRequest.computerRequirements = [{
        computerRequirementsId: parseInt(computerEquipmentId),
        isChild: false,
        parentId: null
      }];

      // Add selected child requirements
      Object.keys(this.selectedChildRequirements).forEach(key => {
        if (this.selectedChildRequirements[key]) {
          const [parentId, childId] = key.split('_').map(id => parseInt(id));
          if (parentId === parseInt(computerEquipmentId)) {
            newHireRequest.computerRequirements!.push({
              computerRequirementsId: childId,
              isChild: true,
              parentId: parentId
            });
          }
        }
      });
    }

    // Add Building Access
    const buildingAccessArray = formValue.buildingAccess || [];
    newHireRequest.buildingAccess = this.processBuildingAccessForSave(buildingAccessArray);

    // Add useExistingKeyFob flag
    newHireRequest.useExistingKeyFob = formValue.useExistingKeyFob || false;

    return newHireRequest;
  }

  /**
   * Helper method to process building access array for saving (used by both submit and draft)
   */
  private processBuildingAccessForSave(buildingAccessArray: any[]): { accessId: number; accessDescription: string }[] {
    if (!Array.isArray(buildingAccessArray) || buildingAccessArray.length === 0) {
      return [];
    }

    if (typeof buildingAccessArray[0] === 'boolean') {
      // Process as boolean array (form controls)
      const buildingAccessResults: { accessId: number; accessDescription: string }[] = [];

      buildingAccessArray.forEach((isSelected: boolean, index: number) => {
        if (isSelected) {
          // First, try to get the sorted description if available
          if (this.availableBuildingAccess && this.availableBuildingAccess[index]) {
            const sortedDescription = this.availableBuildingAccess[index];
            const requirement = this.buildingAccessRequirements.find(req =>
              req.description === sortedDescription
            );

            if (requirement) {
              buildingAccessResults.push({
                accessId: requirement.id,
                accessDescription: requirement.description
              });
              return;
            }
          }

          // Fallback: try to find by original array index if sorted matching fails
          if (this.buildingAccessRequirements && this.buildingAccessRequirements[index]) {
            const fallbackRequirement = this.buildingAccessRequirements[index];
            buildingAccessResults.push({
              accessId: fallbackRequirement.id,
              accessDescription: fallbackRequirement.description
            });
          }
        }
      });

      return buildingAccessResults;
    } else {
      // Already in object format, just ensure accessId is present
      const objectAccessResults: { accessId: number; accessDescription: string }[] = [];

      buildingAccessArray.forEach((access: any) => {
        if (access.accessId) {
          objectAccessResults.push({
            accessId: access.accessId,
            accessDescription: access.accessDescription || ''
          });
        } else if (access.accessDescription) {
          // Find the accessId by description
          const requirement = this.buildingAccessRequirements.find(req =>
            req.description === access.accessDescription
          );
          if (requirement) {
            objectAccessResults.push({
              accessId: requirement.id,
              accessDescription: requirement.description
            });
          }
        }
      });

      return objectAccessResults;
    }
  }

  saveAsDraft() {
    if (this.isDraftSaving) {
      return;
    }

    // Use draft-specific validation - only firstName and lastName are required
    if (this.validateForDraft()) {
      this.isDraftSaving = true;

      try {
        const newHireRequest = this.transformFormToNewHireRequestForDraft();

        // Add validation before API call
        if (!this.validateBuildingAccessBeforeSubmission(newHireRequest)) {
          this.isDraftSaving = false;
          return; // Block submission
        }

        // Determine which service method to call based on mode
        const isUpdate = this.isDraftRequest && this.parentId;

        const serviceCall = isUpdate
          ? this.hrRequestService.updateNewHireRequestAsDraft(this.parentId!, newHireRequest)
          : this.hrRequestService.saveNewHireRequestAsDraft(newHireRequest);

        serviceCall.subscribe({
          next: (response) => {
            if (response.success) {
              const message = this.isDraftRequest
                ? 'New hire request updated as draft successfully!'
                : 'New hire request saved as draft successfully!';
              this.toasterService.showSuccess(message);
              this.isDraftSaving = false;
              this.goBack();
            } else {
              this.toasterService.showError(response.message || 'Failed to save new hire request as draft');
              this.isDraftSaving = false;
            }
          },
          error: (error) => {
            console.error('Error saving draft:', error);

            let errorMessage = 'Failed to save new hire request as draft. Please try again.';
            if (error.error && error.error.message) {
              errorMessage = error.error.message;
            } else if (error.status === 400) {
              errorMessage = 'Invalid data provided. Please check all required fields.';
            } else if (error.status === 500) {
              errorMessage = 'Server error occurred while saving draft. Please try again later.';
            }

            this.toasterService.showError(errorMessage);
            this.isDraftSaving = false;
          }
        });
      } catch (error) {
        console.error('Error processing draft form:', error);
        this.toasterService.showError('Error processing form data. Please check building access selections.');
        this.isDraftSaving = false;
      }
    }
  }


  // Navigation
  goBack() {
    this.location.back();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Route parameter initialization
  private focusField: string | null = null;

  private focusFieldWithRetry(fieldId: string, retriesLeft: number): void {
    // Switch to IT tab first since workPhoneNumber is on that tab
    this.showTab('it');

    setTimeout(() => {
      const element = document.getElementById(fieldId);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        element.focus();
      } else if (retriesLeft > 0) {
        this.focusFieldWithRetry(fieldId, retriesLeft - 1);
      }
    }, 300);
  }

  private initializeRouteParameters(): void {
    // Check for focus query parameter (e.g., from ECM_ADMIN edit action)
    this.focusField = this.route.snapshot.queryParamMap.get('focus');

    // Check for parentId parameter from route
    const parentIdParam = this.route.snapshot.paramMap.get('parentId');
    if (parentIdParam) {
      this.parentId = parseInt(parentIdParam, 10);
      this.isEditMode = true;
      console.log('New hire component loaded in edit mode with parentId:', this.parentId);
      return;
    }

    // Also check query parameters as an alternative
    const parentIdQuery = this.route.snapshot.queryParamMap.get('parentId');
    if (parentIdQuery) {
      this.parentId = parseInt(parentIdQuery, 10);
      this.isEditMode = true;
      console.log('New hire component loaded with parentId from query params:', this.parentId);
      return;
    }

    // Check for requestDetailId parameter for direct editing
    const requestDetailIdParam = this.route.snapshot.paramMap.get('requestDetailId');
    if (requestDetailIdParam) {
      this.requestDetailId = parseInt(requestDetailIdParam, 10);
      this.isEditMode = true;
      console.log('New hire component loaded in edit mode with requestDetailId:', this.requestDetailId);
      return;
    }

    // If no parentId or requestDetailId found, we're in create mode
    console.log('New hire component loaded in create mode');
    this.isEditMode = false;
    this.parentId = null;
    this.requestDetailId = null;
  }

  private async checkUserAuthorization(): Promise<void> {
    const isAuthorized = await this.authService.checkUserAuthorization();
    if (!isAuthorized) {
      console.log('User not authorized for new hire request');
      this.router.navigate(['/unauthorized']);
    }

    const userRoles = await this.authService.getUserRoles();
    this.isEcmAdmin = userRoles.some(role => role.toLowerCase() === 'ecm_admin');
  }

  private initializeCreateMode(): void {
    // Set form as editable for create mode
    this.isFormEditable = true;

    // Initialize available roles based on default company (if any)
    // Building access is now loaded dynamically when company is selected via CompanyTypeLocation API
    const initialCompany = this.newHireForm.get('positionInfo.company')?.value;
    if (initialCompany) {
      // Tablet profiles will be loaded asynchronously when company is selected
      // updateAvailableRolesFromTabletProfiles() will be called when loadTabletProfiles completes
      // Note: Building access will be loaded when company is selected via onCompanySelected
    }

    // Set date constraints for First Day of Employment
    const today = new Date();

    // Set maximum date to 2 years from today to prevent invalid far-future dates
    const maxDateObj = new Date(today);
    maxDateObj.setFullYear(maxDateObj.getFullYear() + 2);
    this.maxEmploymentDate = maxDateObj.toISOString().split('T')[0];
  }

  async loadExistingRequest(): Promise<void> {
    if (!this.parentId) {
      console.error('No parentId available for loading existing request');
      return;
    }

    try {
      this.isLoading = true;
      this.errorMessage = '';

      const response = await this.hrRequestService.getNewHireRequestDetails(this.parentId)
        .pipe(takeUntil(this.destroy$))
        .toPromise();

      if (!response?.success || !response.data) {
        throw new Error(response?.message || 'Failed to load new hire request data');
      }

      this.viewData = response.data;

      // Set the request detail ID for cancel operations
      this.requestDetailId = this.viewData.requestDetailId;

      // Check if the request is cancelled
      this.isCancelledRequest = this.viewData.requestStatusName?.toLowerCase().includes('cancelled') || false;

      // Check if the request is a draft
      this.isDraftRequest = this.viewData.requestStatusName?.toLowerCase().includes('draft') || false;

      // Set form editability: editable if draft, readonly if not
      this.isFormEditable = this.isDraftRequest;

      console.log('Request status:', this.viewData.requestStatusName, 'isDraftRequest:', this.isDraftRequest, 'isFormEditable:', this.isFormEditable);

      // Wait for company-specific data to load before populating form
      await this.loadCompanySpecificDataForView(this.viewData.companyCode);

      // Add a small delay to ensure dropdown components are ready
      await new Promise(resolve => setTimeout(resolve, 100));

      // Populate form with existing data
      this.populateFormWithExistingData(this.viewData);

      this.isLoading = false;

    } catch (error) {
      console.error('Error loading existing new hire request:', error);
      this.errorMessage = 'Failed to load new hire request data';
      this.toasterService.showError('Failed to load new hire request data', 'Error');

      // Fallback to create mode
      this.isEditMode = false;
      this.parentId = null;
      this.requestDetailId = null;
      this.initializeCreateMode();

      this.isLoading = false;
    }
  }

  private populateFormWithExistingData(data: NewHireRequestViewDto): void {

    // Prevent form subscriptions (company change, userId generation, etc.) from firing
    // during programmatic form population. Without this, setting the company value triggers
    // handleCompanyChange → loadCompanySpecificData which asynchronously reloads positions
    // and clears the position value after it was already set by populatePositionInfo.
    this.isUpdatingForm = true;

    try {
      // Populate personal information safely
      this.populatePersonalInfo(data);

      // Populate position information safely
      this.populatePositionInfo(data);

      // Populate other sections safely
      this.populateCreditCardInfo(data);
      this.populateVehicleInfo(data);
      this.populateITInfo(data);
      this.populatePhoneInfo(data);

      // Populate complex structures
      this.populateApplicationSoftware(data.applications);
      this.populateFolders(data.folders);
      this.populateComputerRequirements(data.computerRequirements);

      // Populate notes field
      this.safeSetFormValue('notes', data.notes || '');

    } catch (error) {
      console.error('Error during form population:', error);
      // Continue even if some sections fail to populate
    } finally {
      this.isUpdatingForm = false;
    }

    // Conditionally enable/disable form controls based on editability
    if (this.isFormEditable) {
      this.enableFormControls(); // Enable for drafts
    } else {
      this.disableFormInEditMode(); // Disable for non-drafts
    }

    // Populate tablet profiles and building access AFTER disabling to prevent values from being cleared
    try {
      this.populateTabletProfiles(data.tabletProfiles);
      this.populateBuildingAccess(data.buildingAccess);
    } catch (error) {
      console.error('Error populating tablet profiles or building access after disable:', error);
    }

    // Check if First Day of Employment is in the warning range
    this.checkFirstDayOfEmploymentWarning();

  }

  private populatePersonalInfo(data: NewHireRequestViewDto): void {
    try {
      const personalInfoGroup = this.newHireForm.get('personalInfo');
      if (!personalInfoGroup) {
        console.warn('personalInfo form group not found');
        return;
      }

      this.safeSetFormValue('personalInfo.firstName', data.firstName || '');
      this.safeSetFormValue('personalInfo.lastName', data.lastName || '');
      this.safeSetFormValue('personalInfo.middleInitial', data.middleInitial || '');
      this.safeSetFormValue('personalInfo.suffix', data.suffix || '');
      this.safeSetFormValue('personalInfo.preferredName', data.preferredFirstName || '');
      this.safeSetFormValue('personalInfo.userId', data.userId || '');
      this.safeSetFormValue('personalInfo.firstDay',
        data.firstDayEmployment ? this.formatDateForInput(data.firstDayEmployment) : '');
      this.originalEffectiveDate = data.firstDayEmployment ? this.formatDateForInput(data.firstDayEmployment) : '';
      this.safeSetFormValue('personalInfo.referredBy', data.referredBy || '');
      this.safeSetFormValue('personalInfo.rehire', data.rehire ? 'yes' : 'no');
    } catch (error) {
      console.error('Error populating personal info:', error);
    }
  }

  private populatePositionInfo(data: NewHireRequestViewDto): void {
    try {
      const positionInfoGroup = this.newHireForm.get('positionInfo');
      if (!positionInfoGroup) {
        console.warn('positionInfo form group not found');
        return;
      }


      this.safeSetFormValue('positionInfo.company', data.companyCode ? data.companyCode.toString() : '');
      this.safeSetFormValue('positionInfo.physicalLocation', data.locationCode ? data.locationCode.toString() : '');

      // Employment status - try to match by ID first, then by status string if needed
      let matchingEmploymentStatus = this.employmentStatusesForDropdown.find(status =>
        status.id.toString() === data.employmentStatus
      );

      // If not found by ID, try matching by status string (fallback for different data formats)
      if (!matchingEmploymentStatus) {
        matchingEmploymentStatus = this.employmentStatusesForDropdown.find(status =>
          status.status === data.employmentStatus
        );
      }

      if (matchingEmploymentStatus) {
        const employmentStatusValue = matchingEmploymentStatus.id.toString();
        this.safeSetFormValue('positionInfo.employmentStatus', employmentStatusValue);
      } else {
        this.safeSetFormValue('positionInfo.employmentStatus', '');
      }

      this.safeSetFormValue('positionInfo.union', data.isUnion === true ? 'yes' : data.isUnion === false ? 'no' : '');

      // Union craft - use the unionCraftId directly
      const unionCraftValue = data.unionCraftId ? data.unionCraftId.toString() : '';
      this.safeSetFormValue('positionInfo.unionCraft', unionCraftValue);

      // Handle different data types for IsApprentice (boolean, number, string)
      let apprenticeValue = '';
      const apprenticeAny = data.isApprentice as any;

      if (data.isApprentice === true || apprenticeAny === 1 || apprenticeAny === '1' || apprenticeAny === 'true') {
        apprenticeValue = 'yes';
      } else if (data.isApprentice === false || apprenticeAny === 0 || apprenticeAny === '0' || apprenticeAny === 'false') {
        apprenticeValue = 'no';
      }

      this.safeSetFormValue('positionInfo.apprentice', apprenticeValue);
      this.safeSetFormValue('positionInfo.unionWage', data.isUnionWage === true ? 'yes' : data.isUnionWage === false ? 'no' : '');

      // Apprentice percentage dropdown - find matching item by appPercentage string
      if (data.appPercentage) {
        const matchingApprenticePercentage = this.apprenticePercentagesForDropdown.find(
          apprenticePercentage => apprenticePercentage.appPercentage === data.appPercentage
        );
        if (matchingApprenticePercentage) {
          this.safeSetFormValue('positionInfo.apprenticeDropdown', matchingApprenticePercentage.id.toString());
        } else {
          this.safeSetFormValue('positionInfo.apprenticeDropdown', '');
        }
      } else {
        this.safeSetFormValue('positionInfo.apprenticeDropdown', '');
      }

      // Hourly/Salaried - try multiple matching strategies since backend format is unclear
      let matchingEmployeeSalaryType = this.employeeSalaryTypesForDropdown.find(salaryType => salaryType.id.toString() === data.salaryCode?.toString());

      // If not found by ID, try matching by salaryCode or other properties
      if (!matchingEmployeeSalaryType) {
        matchingEmployeeSalaryType = this.employeeSalaryTypesForDropdown.find(salaryType =>
          salaryType.salaryCode === data.salaryCode?.toString() ||
          parseInt(salaryType.salaryCode) === data.salaryCode
        );
      }

      // Set salary code without triggering handleSalaryTypeChange subscription to avoid
      // filterPositionsBySalaryType rebuilding positionsForDropdown before the dropdown's
      // items input has been synced via change detection
      const salaryCodeValue = matchingEmployeeSalaryType ? matchingEmployeeSalaryType.id.toString() : '';
      const salaryCodeControl = this.newHireForm.get('positionInfo.salaryCode');
      if (salaryCodeControl) {
        salaryCodeControl.setValue(salaryCodeValue, { emitEvent: false });
      }

      // Manually rebuild positionsForDropdown with the correct salary type filter
      this.filterPositionsBySalaryType();

      // Position - find matching item in dropdown by position code
      const matchingPosition = this.positionsForDropdown.find(position => position.positionCode === data.positionCode);
      const positionValue = matchingPosition ? matchingPosition.id.toString() : '';
      this.safeSetFormValue('positionInfo.position', positionValue);

      // Re-set position after change detection so the dropdown's items input is synced
      // with the rebuilt positionsForDropdown before writeValue searches for the item
      if (positionValue) {
        setTimeout(() => {
          this.safeSetFormValue('positionInfo.position', positionValue);
        }, 0);
      }

      // Payroll department - find matching item in dropdown by deptCode
      const matchingPayrollDept = this.payrollDepartmentsForDropdown.find(dept => dept.deptCode === data.payrollDeptCode);

      if (matchingPayrollDept) {
        this.safeSetFormValue('positionInfo.payrollCode', matchingPayrollDept.id.toString());
        // Set display name for view mode
        const payrollDisplayName = `${matchingPayrollDept.deptCode} - ${matchingPayrollDept.deptName}`;
        this.safeSetFormValue('positionInfo.payrollDisplayName', payrollDisplayName);
        // Track payroll department for email generation in edit mode
        this.selectedPayrollDeptCode = matchingPayrollDept.deptCode;
        this.selectedEmailDomain = matchingPayrollDept.emailDomain || null;
        this.hasPayrollDepartmentSelected = true;
      } else {
        this.safeSetFormValue('positionInfo.payrollCode', '');
        // Use the payroll dept data from the DTO if no matching dept found in dropdown
        const displayName = data.payrollDeptName ? `${data.payrollDeptCode} - ${data.payrollDeptName}` : (data.payrollDeptCode ? data.payrollDeptCode.toString() : '');
        this.safeSetFormValue('positionInfo.payrollDisplayName', displayName);
      }

      // Supervisor - find matching item in dropdown by supervisor ID (stored as employeeNumber)
      // If supervisorId is null, select "NOT FOUND, Will contact HR"
      if (!data.supervisorId) {
        this.safeSetFormValue('positionInfo.supervisor', this.NOT_FOUND_SUPERVISOR.id.toString());
        this.safeSetFormValue('positionInfo.supervisorDisplayName', 'NOT FOUND, Will contact HR');
      } else {
        const matchingSupervisor = this.supervisorsForDropdown.find(supervisor => supervisor.employeeNumber === data.supervisorId);

        if (matchingSupervisor) {
          const supervisorValueAsString = matchingSupervisor.id.toString();
          this.safeSetFormValue('positionInfo.supervisor', supervisorValueAsString);

          // Set display name for view mode
          const supervisorDisplayName = `${matchingSupervisor.employeeNumber} - ${matchingSupervisor.firstName} ${matchingSupervisor.lastName}`;
          this.safeSetFormValue('positionInfo.supervisorDisplayName', supervisorDisplayName);
        } else {
          this.safeSetFormValue('positionInfo.supervisor', '');

          // Use the supervisor data from the DTO if no matching supervisor found in dropdown
          const displayName = data.supervisorName ? `${data.supervisorId} - ${data.supervisorName}` : '';
          this.safeSetFormValue('positionInfo.supervisorDisplayName', displayName);
        }
      }
    } catch (error) {
      console.error('Error populating position info:', error);
    }
  }

  private populateCreditCardInfo(data: NewHireRequestViewDto): void {
    try {
      if (!data.creditCardInfo) {
        console.log('No credit card info to populate');
        return;
      }

      const creditCardGroup = this.newHireForm.get('creditCardInfo');
      if (!creditCardGroup) {
        console.warn('creditCardInfo form group not found');
        return;
      }

      this.safeSetFormValue('creditCardInfo.kwikTripCard', data.creditCardInfo.kwikTripCard ? 'yes' : 'no');
      this.safeSetFormValue('creditCardInfo.companyExpenseCard', data.creditCardInfo.companyExpenseCard ? 'yes' : 'no');
      this.safeSetFormValue('creditCardInfo.creditExpenseType', data.creditCardInfo.creditExpenseType || '');
      this.safeSetFormValue('creditCardInfo.weeklyLimit', data.creditCardInfo.weeklyLimit ? data.creditCardInfo.weeklyLimit.toString() : '');
      this.safeSetFormValue('creditCardInfo.fuelCardlockAccess', data.creditCardInfo.fuelCardlockAccess ? 'yes' : 'no');
      this.safeSetFormValue('creditCardInfo.cardlockShipAddress', data.creditCardInfo.fuelCardlockAddress || '');
    } catch (error) {
      console.error('Error populating credit card info:', error);
    }
  }

  private populateVehicleInfo(data: NewHireRequestViewDto): void {
    try {
      if (!data.vehicleInfo) {
        console.log('No vehicle info to populate');
        return;
      }

      const vehicleGroup = this.newHireForm.get('vehicleInfo');
      if (!vehicleGroup) {
        console.warn('vehicleInfo form group not found');
        return;
      }

      this.safeSetFormValue('vehicleInfo.approvedVehicle', data.vehicleInfo.isApprovedToOperate ? 'yes' : 'no');

      // Driver classification - find matching item by licenseClass string
      if (data.vehicleInfo.driverClassification) {
        const matchingLicenseClass = this.employeeLicenseClassesForDropdown.find(
          licenseClass => licenseClass.licenseClass === data.vehicleInfo?.driverClassification
        );
        if (matchingLicenseClass) {
          this.safeSetFormValue('vehicleInfo.driverClassification', matchingLicenseClass.id.toString());
        } else {
          this.safeSetFormValue('vehicleInfo.driverClassification', '');
        }
      } else {
        this.safeSetFormValue('vehicleInfo.driverClassification', '');
      }

      this.safeSetFormValue('vehicleInfo.drugAlcoholProfile', data.vehicleInfo.drugAndAlcoholProfile || '');
      this.safeSetFormValue('vehicleInfo.companyCarNeeded', data.vehicleInfo.needCompanyCar ? 'yes' : 'no');
      this.safeSetFormValue('vehicleInfo.applicationPart2', data.vehicleInfo.isApplicationPart2Complete ? 'yes' : 'no');
    } catch (error) {
      console.error('Error populating vehicle info:', error);
    }
  }

  private populateITInfo(data: NewHireRequestViewDto): void {
    try {
      console.log('[DEBUG] populateITInfo called with data.itInfo:', data.itInfo);

      if (!data.itInfo) {
        console.log('No IT info to populate');
        return;
      }

      const itGroup = this.newHireForm.get('itInfo');
      if (!itGroup) {
        console.warn('itInfo form group not found');
        return;
      }

      const emailRequiredValue = data.itInfo.emailRequired ? 'yes' : 'no';
      const microsoftLicenseValue = data.itInfo.msOfficeLicenseE5 ? 'e5' : data.itInfo.msOfficeLicenseF3 ? 'f3' : '';

      console.log('[DEBUG] Setting emailRequired to:', emailRequiredValue);
      console.log('[DEBUG] Setting microsoftLicense to:', microsoftLicenseValue);
      console.log('[DEBUG] data.itInfo.msOfficeLicenseE5:', data.itInfo.msOfficeLicenseE5);
      console.log('[DEBUG] data.itInfo.msOfficeLicenseF3:', data.itInfo.msOfficeLicenseF3);

      this.safeSetFormValue('itInfo.emailRequired', emailRequiredValue);
      this.safeSetFormValue('itInfo.microsoftLicense', microsoftLicenseValue);
      this.safeSetFormValue('itInfo.emailAddress', data.itInfo.emailAddress || '');
      this.safeSetFormValue('itInfo.deliveryNote', data.itInfo.alternateDeliveryLocation || '');

      // Log the actual form values after setting
      console.log('[DEBUG] Form emailRequired value after set:', this.newHireForm.get('itInfo.emailRequired')?.value);
      console.log('[DEBUG] Form microsoftLicense value after set:', this.newHireForm.get('itInfo.microsoftLicense')?.value);
      console.log('[DEBUG] isEmailRequired getter:', this.isEmailRequired);
      console.log('[DEBUG] showMicrosoftLicenseSection getter:', this.showMicrosoftLicenseSection);
    } catch (error) {
      console.error('Error populating IT info:', error);
    }
  }

  private populatePhoneInfo(data: NewHireRequestViewDto): void {
    try {
      if (!data.phoneInfo) {
        console.log('No phone info to populate');
        return;
      }

      // Handle phone types array
      const phoneTypesArray = this.newHireForm.get('itInfo.phoneTypes') as FormArray;
      if (phoneTypesArray) {
        try {
          phoneTypesArray.patchValue([
            data.phoneInfo.deskPhone || false,
            data.phoneInfo.companyCellphone || false,
            data.phoneInfo.byodCellphone || false
          ]);
        } catch (error) {
          console.warn('Error setting phone types array:', error);
        }
      }

      // Handle other phone fields
      this.safeSetFormValue('itInfo.workPhoneNumber', data.phoneInfo.workPhoneNumber || '');
      this.safeSetFormValue('itInfo.workExtension', data.phoneInfo.workExtension || '');
      this.safeSetFormValue('itInfo.reusingPhone', data.phoneInfo.reusingExistingPhone ? 'yes' : 'no');
    } catch (error) {
      console.error('Error populating phone info:', error);
    }
  }

  private safeSetFormValue(controlPath: string, value: any): void {
    try {
      const control = this.newHireForm.get(controlPath);
      if (control) {
        // Remember if the control was disabled before setting the value
        const wasDisabled = control.disabled;

        control.setValue(value);

        // Only re-disable if form should not be editable and it was previously disabled
        if (!this.isFormEditable && wasDisabled) {
          control.disable();
        }
      } else {
        console.warn(`Form control not found: ${controlPath}`);
      }
    } catch (error) {
      console.error(`Error setting form value for ${controlPath}:`, error);
    }
  }

  private safeSetControlValue(control: any, value: any): void {
    if (!control) return;

    // Remember if the control was disabled before setting the value
    const wasDisabled = control.disabled;

    control.setValue(value);

    // Only re-disable if form should not be editable and it was previously disabled
    if (!this.isFormEditable && wasDisabled) {
      control.disable();
    }
  }

  private async loadCompanySpecificDataForView(companyCode: number): Promise<void> {
    if (!companyCode) {
      return;
    }

    console.log('Loading company-specific data for view mode, company:', companyCode);

    try {
      // Load all company-specific data in parallel
      await Promise.all([
        this.loadEmploymentStatuses(companyCode),
        this.loadEmployeeSalaryTypes(companyCode),
        this.loadPositions(companyCode),
        this.loadPayrollDepartments(companyCode),
        this.loadTabletProfiles(companyCode),
        this.loadApplications(companyCode),
        this.loadUnionCrafts(companyCode),
        this.loadCompanyUnionStatusAsync(companyCode),
        this.loadApprenticePercentagesForView(),
        this.loadEmployeeLicenseClassesForView()
      ]);

      // Load supervisors if we have payroll department data
      if (this.viewData?.payrollDeptCode) {
        await this.loadSupervisorsForView(companyCode, this.viewData.payrollDeptCode);
      }

    } catch (error) {
      console.error('Error loading company-specific data for view mode:', error);
      // Continue even if some data fails to load
    }
  }

  private async loadSupervisorsForView(companyCode: number, payrollDeptCode: number): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      this.referenceDataService.getSupervisorsWithCache(companyCode, payrollDeptCode).subscribe({
        next: (response) => {
          if (response.success && response.data && response.data.length > 0) {
            this.supervisors = response.data;
            this.supervisorsForDropdown = [
              ...this.supervisors.map(supervisor => ({
                ...supervisor,
                displayText: `${supervisor.firstName} ${supervisor.lastName} (${supervisor.employeeNumber})`
              })),
              this.NOT_FOUND_SUPERVISOR
            ];
          } else {
            console.warn('No supervisors found for view mode');
            this.supervisors = [];
            this.supervisorsForDropdown = [this.NOT_FOUND_SUPERVISOR];
          }
          resolve();
        },
        error: (error) => {
          console.error('Error loading supervisors for view mode:', error);
          this.supervisors = [];
          this.supervisorsForDropdown = [this.NOT_FOUND_SUPERVISOR];
          // Don't reject - continue even if supervisor loading fails
          resolve();
        }
      });
    });
  }

  private disableFormInEditMode(): void {
    if (this.isFormEditable) {
      return; // Don't disable if form should be editable
    }

    // Disable all form controls recursively
    this.disableFormGroup(this.newHireForm);

    // Re-enable date field when request is still in process
    if (this.canUpdateDate) {
      this.newHireForm.get('personalInfo.firstDay')?.enable({ emitEvent: false });
    }

    // Re-enable Work Phone Number and Work Extension for ECM_ADMIN when status is Pending or Processing
    this.enablePhoneFieldsForAdmin();
  }

  private enablePhoneFieldsForAdmin(): void {
    if (!this.isEcmAdmin) {
      return;
    }

    const status = this.viewData?.requestStatusName?.toLowerCase() || '';
    const isPendingOrProcessing = status.includes('pending') || status.includes('processing');

    if (isPendingOrProcessing) {
      this.newHireForm.get('itInfo.workPhoneNumber')?.enable({ emitEvent: false });
      this.newHireForm.get('itInfo.workExtension')?.enable({ emitEvent: false });
    }
  }

  get canUpdateDate(): boolean {
    if (!this.isEditMode || !this.viewData) return false;
    const status = this.viewData.requestStatusName?.toLowerCase() || '';
    if (!status.includes('pending')) return false;
    // Disable editing if effective date is today or in the past
    // Parse YYYY-MM-DD as local date (not UTC) by splitting the string
    if (this.originalEffectiveDate) {
      const parts = this.originalEffectiveDate.split('-');
      const effective = new Date(+parts[0], +parts[1] - 1, +parts[2]);
      const today = new Date();
      effective.setHours(0, 0, 0, 0);
      today.setHours(0, 0, 0, 0);
      if (effective.getTime() <= today.getTime()) return false;
    }
    return true;
  }

  isUpdatingDate: boolean = false;
  originalEffectiveDate: string = '';

  updateEffectiveDate(): void {
    if (!this.parentId || this.isUpdatingDate) return;

    const firstDay = this.newHireForm.get('personalInfo.firstDay')?.value;
    if (!firstDay) {
      this.toasterService.showError('Please select a date');
      return;
    }

    this.isUpdatingDate = true;
    this.hrRequestService.updateEffectiveDate(this.parentId, firstDay).subscribe({
      next: (response) => {
        this.isUpdatingDate = false;
        if (response.success) {
          this.toasterService.showSuccess('Effective date updated successfully!');
          this.goBack();
        } else {
          this.toasterService.showError(response.message || 'Failed to update effective date');
        }
      },
      error: (error) => {
        this.isUpdatingDate = false;
        this.toasterService.showError('Failed to update effective date. Please try again.');
        console.error('Error updating effective date:', error);
      }
    });
  }

  get canUpdatePhoneInfo(): boolean {
    if (!this.isEditMode || !this.isEcmAdmin || !this.viewData || !this.isDeskPhoneSelected) {
      return false;
    }
    const status = this.viewData.requestStatusName?.toLowerCase() || '';
    return status.includes('pending') || status.includes('processing');
  }

  updatePhoneInfo(): void {
    if (!this.parentId || this.isUpdatingPhoneInfo) {
      return;
    }

    this.isUpdatingPhoneInfo = true;
    const phoneInfo = {
      workPhoneNumber: this.newHireForm.get('itInfo.workPhoneNumber')?.value?.trim() || null,
      workExtension: this.newHireForm.get('itInfo.workExtension')?.value?.trim() || null
    };

    // Check if effective date also needs updating
    const firstDay = this.newHireForm.get('personalInfo.firstDay')?.value;
    const dateChanged = this.canUpdateDate && firstDay && firstDay !== this.originalEffectiveDate;

    this.hrRequestService.updateNewHirePhoneInfo(this.parentId, phoneInfo).subscribe({
      next: (response) => {
        if (response.success && dateChanged) {
          // Also update the effective date if it was changed
          this.hrRequestService.updateEffectiveDate(this.parentId!, firstDay).subscribe({
            next: (dateResponse) => {
              this.isUpdatingPhoneInfo = false;
              if (dateResponse.success) {
                this.toasterService.showSuccess('New hire request updated successfully!');
              } else {
                this.toasterService.showError('Phone info updated, but failed to update effective date: ' + (dateResponse.message || ''));
              }
              this.goBack();
            },
            error: (dateError) => {
              this.isUpdatingPhoneInfo = false;
              this.toasterService.showError('Phone info updated, but failed to update effective date. Please try again.');
              console.error('Error updating effective date:', dateError);
              this.goBack();
            }
          });
        } else {
          this.isUpdatingPhoneInfo = false;
          if (response.success) {
            this.toasterService.showSuccess('New hire request updated successfully!');
            this.goBack();
          } else {
            this.toasterService.showError(response.message || 'Failed to update new hire request');
          }
        }
      },
      error: (error) => {
        this.isUpdatingPhoneInfo = false;
        this.toasterService.showError('Failed to update new hire request. Please try again.');
        console.error('Error updating phone info:', error);
      }
    });
  }

  private disableFormGroup(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      if (control instanceof FormGroup) {
        this.disableFormGroup(control);
      } else if (control instanceof FormArray) {
        this.disableFormArray(control);
      } else {
        control?.disable({ emitEvent: false });
      }
    });
  }

  private disableFormArray(formArray: FormArray): void {
    formArray.controls.forEach(control => {
      if (control instanceof FormGroup) {
        this.disableFormGroup(control);
      } else {
        control.disable({ emitEvent: false });
      }
    });
  }

  private enableFormControls(): void {
    this.enableFormGroup(this.newHireForm);
  }

  private enableFormGroup(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      if (control instanceof FormGroup) {
        this.enableFormGroup(control);
      } else if (control instanceof FormArray) {
        this.enableFormArray(control);
      } else {
        control?.enable({ emitEvent: false });
      }
    });
  }

  private enableFormArray(formArray: FormArray): void {
    formArray.controls.forEach(control => {
      if (control instanceof FormGroup) {
        this.enableFormGroup(control);
      } else {
        control.enable({ emitEvent: false });
      }
    });
  }

  private populateApplicationSoftware(applications: any[]): void {
    if (!applications || applications.length === 0) {
      return;
    }

    const applicationArray = this.newHireForm.get('applicationSoftware') as FormArray;
    applicationArray.clear();

    applications.forEach(app => {
      const appGroup = this.createApplicationSoftwareGroup();
      appGroup.patchValue({
        applicationSoftware: app.applicationId ? app.applicationId.toString() : '',
        applicationAccessNote: app.accessNotes || ''
      });
      applicationArray.push(appGroup);
    });
  }

  private populateFolders(folders: any[]): void {
    if (!folders || folders.length === 0) {
      return;
    }

    const folderArray = this.newHireForm.get('folderSharepoint') as FormArray;
    folderArray.clear();

    folders.forEach(folder => {
      const folderGroup = this.createFolderSharepointGroup();
      folderGroup.patchValue({
        type: folder.folderType || 'Folder',
        folderSharepointMailbox: folder.folderName || ''
      });
      folderArray.push(folderGroup);
    });
  }

  private populateTabletProfiles(tabletProfiles: any[]): void {
    console.log('[DEBUG] populateTabletProfiles called with:', tabletProfiles);
    console.log('[DEBUG] availableRoles:', this.availableRoles);
    console.log('[DEBUG] availableRoles values:', this.availableRoles.map(r => r.value));

    if (!tabletProfiles || tabletProfiles.length === 0) {
      console.log('[DEBUG] No tablet profiles to populate');
      return;
    }

    try {
      // Set the first tablet profile as the selected role
      const firstProfile = tabletProfiles[0];
      console.log('[DEBUG] firstProfile:', firstProfile);

      if (firstProfile) {
        const profileName = firstProfile.tabletProfileName || '';
        console.log('[DEBUG] Setting rolesRequiredNewHires to:', profileName);
        console.log('[DEBUG] Looking for profileName in availableRoles. ProfileName:', `"${profileName}"`);

        this.safeSetFormValue('itInfo.rolesRequiredNewHires', profileName);

        // Log the actual form value after setting
        console.log('[DEBUG] Form rolesRequiredNewHires value after set:', this.newHireForm.get('itInfo.rolesRequiredNewHires')?.value);

        // Check if the profile exists in availableRoles
        const matchingRole = this.availableRoles.find(r => r.value === profileName);
        console.log('[DEBUG] Matching role in availableRoles:', matchingRole);

        if (!matchingRole) {
          console.log('[DEBUG] NO MATCH FOUND! Available values are:', this.availableRoles.map(r => `"${r.value}"`).join(', '));
        }

        // Set role-specific text if available
        const roleControlName = this.getRoleTextboxControlName(firstProfile.tabletProfileName);
        if (roleControlName && firstProfile.rolesRequiredForNewHire) {
          this.safeSetFormValue(`itInfo.${roleControlName}`, firstProfile.rolesRequiredForNewHire);
        }
      }
    } catch (error) {
      console.error('Error populating tablet profiles:', error);
    }
  }

  private populateComputerRequirements(computerRequirements: any[]): void {
    if (!computerRequirements || computerRequirements.length === 0) {
      return;
    }

    // Find parent computer requirement
    const parentRequirement = computerRequirements.find(req => !req.isChild);
    if (parentRequirement) {
      this.newHireForm.patchValue({
        itInfo: {
          computerEquipment: parentRequirement.computerRequirementsId
        }
      });

      // Find and set child requirements
      const childRequirements = computerRequirements.filter(req => req.isChild && req.parentId === parentRequirement.computerRequirementsId);
      childRequirements.forEach(child => {
        const key = `${child.parentId}_${child.computerRequirementsId}`;
        this.selectedChildRequirements[key] = true;
      });
    }
  }

  private populateBuildingAccess(buildingAccess: any[]): void {
    if (!buildingAccess || buildingAccess.length === 0 || !this.buildingAccessRequirements) {
      // Restore useExistingKeyFob flag if no building access items were saved
      this.newHireForm.get('useExistingKeyFob')?.setValue(this.viewData?.useExistingKeyFob ?? false, { emitEvent: false });
      return;
    }

    const buildingAccessArray = this.newHireForm.get('buildingAccess') as FormArray;
    if (buildingAccessArray) {
      // Create a boolean array based on the SORTED availableBuildingAccess order
      const accessValues = this.availableBuildingAccess.map(sortedDescription => {
        // Find the requirement that matches this sorted description
        const matchingRequirement = this.buildingAccessRequirements.find(req =>
          req.description === sortedDescription
        );

        if (!matchingRequirement) return false;

        // Check if this requirement was saved in the draft
        return buildingAccess.some(access =>
          access.accessId === matchingRequirement.id ||
          access.accessDescription === matchingRequirement.description
        );
      });

      buildingAccessArray.patchValue(accessValues);
    }
  }

  onRequestCancelled(): void {
    // Handle any post-cancellation logic if needed
    console.log('Request has been cancelled');
    // Optionally reload data or update UI state
    if (this.parentId) {
      this.loadExistingRequest();
    }
  }

  // Utility methods
  isFieldInvalid(fieldPath: string): boolean {
    const field = this.newHireForm.get(fieldPath);
    return field ? (field.invalid && (field.dirty || field.touched)) : false;
  }

  getFieldError(fieldPath: string): string {
    const field = this.newHireForm.get(fieldPath);
    if (field?.errors) {
      if (field.errors['required']) {
        return 'This field is required.';
      }
    }
    return '';
  }

  /**
   * Check if cancellation banner should be displayed
   * Shows when: FirstDayOfEmployment is today or within past 7 days AND status is Pending/Processing/Failed/Draft
   */
  shouldDisplayCancellationBanner(): boolean {
    if (!this.isEditMode || !this.viewData || this.isCancelledRequest) {
      return false;
    }

    // Check if status is Completed (should not show banner)
    if (this.viewData.requestStatusName?.toLowerCase().includes('completed')) {
      return false;
    }

    // Check if status is one of the allowed statuses: Pending, Processing, Failed, Draft
    const allowedStatuses = ['pending', 'processing', 'failed', 'draft'];
    const isAllowedStatus = allowedStatuses.some(status =>
      this.viewData?.requestStatusName?.toLowerCase().includes(status)
    );

    if (!isAllowedStatus) {
      return false;
    }

    // Check if FirstDayOfEmployment is today or within past 7 days
    if (!this.viewData.firstDayEmployment) {
      return false;
    }

    const firstDayOfEmployment = new Date(this.viewData.firstDayEmployment);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    firstDayOfEmployment.setHours(0, 0, 0, 0);

    const daysDifference = Math.floor((today.getTime() - firstDayOfEmployment.getTime()) / (1000 * 60 * 60 * 24));

    // Return true if today or within past 7 days (0 to 7 days)
    return daysDifference >= 0 && daysDifference <= 7;
  }

  /**
   * Generate dynamic message for cancellation banner
   */
  getCancellationBannerMessage(): string {
    if (!this.viewData?.firstDayEmployment) {
      return '';
    }

    const firstDayOfEmployment = new Date(this.viewData.firstDayEmployment);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    firstDayOfEmployment.setHours(0, 0, 0, 0);

    const daysDifference = Math.floor((today.getTime() - firstDayOfEmployment.getTime()) / (1000 * 60 * 60 * 24));

    if (daysDifference === 0) {
      return `FirstDayOfEmployment is today`;
    } else if (daysDifference > 0 && daysDifference <= 7) {
      return `FirstDayOfEmployment was ${daysDifference} day${daysDifference > 1 ? 's' : ''} ago`;
    }

    return '';
  }

  /**
   * Initiate cancel request - Shows confirmation dialog
   */
  initiateCancelRequest(): void {
    if (!this.viewData) {
      console.error('No request data found');
      return;
    }

    this.pendingCancelRequest = this.viewData;
    this.showCancelConfirmDialog = true;
  }

  /**
   * Handle cancel confirmation - Execute the cancel request
   */
  async onCancelConfirmed(): Promise<void> {
    if (!this.requestDetailId) {
      console.error('No request detail ID found');
      return;
    }

    this.showCancelConfirmDialog = false;

    try {
      this.isLoading = true;
      const response = await this.hrRequestService.cancelHRRequestDetail(this.requestDetailId).toPromise();

      if (response?.success) {
        console.log('New Hire request cancelled successfully');
        this.toasterService.showSuccess('New Hire request cancelled successfully', 'Request Cancelled');

        // Redirect to HR dashboard after successful cancellation
        setTimeout(() => {
          this.router.navigate(['/']);
        }, 1500);
      } else {
        console.error('Failed to cancel request:', response?.message);
        this.toasterService.showError(`Failed to cancel request: ${response?.message}`, 'Cancel Failed');
      }
    } catch (error: any) {
      console.error('Error cancelling request:', error);
      const errorMessage = error?.error?.message || error?.message || 'Unknown error occurred';
      this.toasterService.showError(`Error cancelling request: ${errorMessage}`, 'Cancel Error');
    } finally {
      this.isLoading = false;
      this.pendingCancelRequest = null;
    }
  }

  /**
   * Handle cancel dialog closed or cancelled
   */
  onCancelDialogClosed(): void {
    this.showCancelConfirmDialog = false;
    this.pendingCancelRequest = null;
  }
}