import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormControl, FormArray } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { Subject, firstValueFrom } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { trigger, transition, style, animate } from '@angular/animations';
import { AppHeaderComponent } from '../../../shared/app-header/app-header.component';
import { SearchableDropdownComponent, SearchableDropdownConfig } from '../../../shared/searchable-dropdown/searchable-dropdown.component';
import { CancelRequestButtonComponent } from '../../../shared/cancel-request-button/cancel-request-button.component';
import { BackToHomepageButtonComponent } from '../../../shared/back-to-homepage-button/back-to-homepage-button.component';
import { ConfirmationDialogComponent, ConfirmationDialogConfig } from '../../../shared/confirmation-dialog/confirmation-dialog.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { HRRequestService } from '../../../core/services/hr-request.service';
import { EmployeeService, EmployeeDto } from '../../../core/services/employee.service';
import { ReferenceDataService } from '../../../core/services/reference-data.service';
import { CreateHRRequestDto } from '../../../models/api-hr-request.model';
import { CreatePromotionRequestDto, PromotionTabletProfileDto, PromotionVehicleInfoDto } from '../../../models/promotion-request.model';

interface PromotionFormData {
  selectedEmployeeId?: string;
  selectedEmployeeName?: string;
  currentPayrollCompany?: string | number;
  currentPayrollGroup?: string | number;
  currentPayrollDept?: string | number;
  currentPositionId?: string | number;
  currentSupervisor?: string | number;
  currentPhysicalLocation?: string | number;
  currentStatus?: string | number;
  currentSalaryCode?: string | number;
  currentPayRate?: string | number;
  newPayrollCompany?: string | number;
  newPayrollGroup?: string | number;
  newPayrollDept?: string | number;
  newPosition?: string | number;
  newSupervisor?: string | number;
  newHRrep?: string;
  newPhysicalLocation?: string | number;
  newStatus?: string | number;
  newSalaryCode?: string | number;
  newpayRate?: string | number;
  effectiveDate?: string;
  notes?: string;
  kwikTripCard?: string;
  companyExpenseCard?: string;
  creditExpenseType?: string;
  weeklyLimit?: number;
  fuelCardlockAccess?: string;
  cardlockShipAddress?: string;
  companyVehicleApproved?: string;
  driverClassification?: string;
  drugAlcoholProfile?: string;
  companyCarNeeded?: string;
  applicationPart2?: string;
}

@Component({
  selector: 'app-promotion-request',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, AppHeaderComponent, SearchableDropdownComponent, CancelRequestButtonComponent, BackToHomepageButtonComponent, ConfirmationDialogComponent],
  templateUrl: './promotion-request.component.html',
  styleUrls: ['./promotion-request.component.css'],
  animations: [
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate('300ms ease-in', style({ opacity: 1, transform: 'translateY(0)' }))
      ])
    ])
  ]
})
export class PromotionRequestComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // View mode properties
  isEditMode: boolean = false;
  parentId: number | null = null;
  viewData: any | null = null;
  errorMessage = '';
  requestDetailId: number | null = null;
  requestStatusId: number | null = null;
  isCancelledRequest: boolean = false;
  isDraftRequest: boolean = false;
  isFormEditable: boolean = false;

  // Search and employee selection
  searchInput: string = '';
  searchResults: EmployeeDto[] = [];
  showSearchResults: boolean = false;
  selectedEmployee: EmployeeDto | null = null;
  showPositionComparison: boolean = false;
  showConditionalSections: boolean = false;

  // Form data
  formData: PromotionFormData = {};
  originalEffectiveDate: string = '';
  activeTab: string = 'personal';

  // Track if IT Related Information tab has been visited
  hasVisitedITTab: boolean = false;

  // Admin role flag
  isEcmAdmin = false;

  // IT Tab Review confirmation dialog
  showITTabReviewDialog: boolean = false;
  itTabReviewDialogConfig: ConfirmationDialogConfig = {
    title: 'IT Related Information Review',
    message: 'You have not reviewed the IT Related Information tab. Please review it before submitting.',
    confirmButtonText: 'Review IT Info',
    cancelButtonText: 'Submit Anyway',
    confirmButtonClass: 'btn-primary',
    cancelButtonClass: 'btn-secondary',
    showIcon: true,
    iconType: 'warning'
  };

  // Computer Requirements confirmation dialog
  showComputerRequirementsDialog: boolean = false;
  computerRequirementsDialogConfig: ConfirmationDialogConfig = {
    title: 'Computer Requirements Review',
    message: 'Please review the Computer Requirements section before proceeding.',
    confirmButtonText: 'Review Requirements',
    cancelButtonText: 'Skip for Now',
    confirmButtonClass: 'btn-primary',
    cancelButtonClass: 'btn-secondary',
    showIcon: true,
    iconType: 'question'
  };

  // Reactive form for conditional sections
  promotionForm!: FormGroup;

  // Show/hide vehicle fields based on company vehicle approval
  showVehicleFields: boolean = false;

  // Building Access
  availableBuildingAccess: string[] = [];
  buildingAccessRequirements: any[] = [];

  // Loading state
  isLoading: boolean = false;
  isSaving: boolean = false;

  // Form update flag to prevent infinite loops
  private isUpdatingForm = false;

  // Search subject for debouncing
  private searchSubject = new Subject<string>();

  // Reference data arrays for dropdowns
  payrollCompanies: any[] = [];
  payrollGroups: any[] = [];
  payrollDepts: any[] = [];
  positions: any[] = [];
  supervisors: any[] = [];
  physicalLocations: any[] = [];
  employmentStatuses: any[] = [];
  employeeSalaryTypes: any[] = [];

  // Data for searchable dropdowns (for current position - read-only)
  currentPayrollCompaniesForDropdown: any[] = [];
  currentPayrollGroupsForDropdown: any[] = [];
  currentPayrollDeptsForDropdown: any[] = [];
  currentPositionsForDropdown: any[] = [];
  currentSupervisorsForDropdown: any[] = [];
  currentPhysicalLocationsForDropdown: any[] = [];
  currentEmploymentStatusesForDropdown: any[] = [];
  currentEmployeeSalaryTypesForDropdown: any[] = [];

  // Data for searchable dropdowns (for new position)
  newPayrollCompaniesForDropdown: any[] = [];
  newPayrollGroupsForDropdown: any[] = [];
  newPayrollDeptsForDropdown: any[] = [];
  newPositionsForDropdown: any[] = [];
  newSupervisorsForDropdown: any[] = [];
  newPhysicalLocationsForDropdown: any[] = [];
  newEmploymentStatusesForDropdown: any[] = [];
  newEmployeeSalaryTypesForDropdown: any[] = [];

  // Flag to prevent subscription from firing during initial employee population
  private isPopulatingEmployeeData: boolean = false;

  // IT Related Information data
  computerRequirements: any[] = [];
  parentComputerRequirements: any[] = [];
  childComputerRequirements: Map<number, any[]> = new Map();
  selectedChildRequirements: { [key: string]: boolean } = {};

  // Tablet Profile data
  availableRoles: Array<{value: string, label: string}> = [];
  tabletProfiles: any[] = [];

  applicationsForDropdown: any[] = [];
  applicationDropdownConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select application/software...'
  };

  // Driver Classification data
  employeeLicenseClasses: any[] = [];
  employeeLicenseClassesForDropdown: any[] = [];
  employeeLicenseClassDropdownConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select license class...'
  };

  // Dropdown configurations
  currentPayrollCompanyConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select company...',
    disabled: true
  };
  currentPayrollGroupConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select group...',
    disabled: true
  };
  currentPayrollDeptConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select department...',
    disabled: true
  };
  currentPositionConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select position...',
    disabled: true
  };
  currentSupervisorConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select supervisor...',
    disabled: true
  };
  currentPhysicalLocationConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select location...',
    disabled: true
  };
  currentEmploymentStatusConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select status...',
    disabled: true
  };
  currentPayRateConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select pay rate...',
    disabled: true
  };

  // New position dropdown configs
  newPayrollCompanyConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select company...',
    required: true
  };
  newPayrollGroupConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select group...',
    required: true
  };
  newPayrollDeptConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select department...',
    required: true
  };
  newPositionConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select position...',
    required: true
  };
  newSupervisorConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select supervisor...',
    required: true
  };
  newPhysicalLocationConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select location...',
    required: true
  };
  newEmploymentStatusConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select status...',
    required: true
  };
  newPayRateConfig: SearchableDropdownConfig<any> = {
    displayProperty: 'displayText' as keyof any,
    valueProperty: 'id' as keyof any,
    placeholder: 'Select pay rate...',
    required: true
  };

  constructor(
    private employeeService: EmployeeService,
    private hrRequestService: HRRequestService,
    private authService: AuthService,
    private toasterService: ToasterService,
    private referenceDataService: ReferenceDataService,
    private router: Router,
    private route: ActivatedRoute,
    private location: Location,
    private formBuilder: FormBuilder
  ) {}

  async ngOnInit(): Promise<void> {
    console.log('[ngOnInit] Promotion request component initializing...');
    // Initialize route parameters first
    this.initializeRouteParameters();

    this.initializeForm();
    this.setupFormSubscriptions();
    this.setupSearchDebounce();

    // Only load companies on page load - other reference data will be loaded after employee selection
    this.loadCompaniesOnly();

    // Load employee license classes (global data, not company-specific)
    this.loadEmployeeLicenseClasses();

    // Check admin role
    const userRoles = await this.authService.getUserRoles();
    this.isEcmAdmin = userRoles.some(role => role.toLowerCase() === 'ecm_admin');

    if (this.isEditMode && this.parentId) {
      // Load existing request data for view mode
      await this.loadExistingRequest();
    }
  }

  private initializeForm(): void {
    this.promotionForm = this.formBuilder.group({
      // Position Info Section (New Position)
      positionInfo: this.formBuilder.group({
        newPayrollCompany: ['', Validators.required],
        newPayrollGroup: ['', Validators.required],
        newPayrollDept: ['', Validators.required],
        newPosition: ['', Validators.required],
        newSupervisor: [''],
        newPhysicalLocation: ['', Validators.required],
        newStatus: ['', Validators.required],
        newpayRate: ['']
      }),
      // Vehicle Info Section
      vehicleInfo: this.formBuilder.group({
        approvedVehicle: ['no'],
        driverClassification: [''],
        drugAlcoholProfile: [''],
        companyCarNeeded: ['no'],
        applicationPart2: ['no']
      }),
      // Credit Cards Section
      kwikTripCard: ['no', Validators.required],
      companyExpenseCard: ['no', Validators.required],
      creditExpenseType: [''],
      weeklyLimit: [''],
      fuelCardlockAccess: ['no', Validators.required],
      cardlockShipAddress: [''],
      // Building Access Section
      buildingAccess: this.formBuilder.array([]),
      useExistingKeyFob: [false],
      // IT Related Information Section
      itInfo: this.formBuilder.group({
        emailRequired: ['no'], // Default to 'no'
        microsoftLicense: [''],
        emailAddress: [''],
        phoneTypes: this.formBuilder.array([
          this.formBuilder.control(false),
          this.formBuilder.control(false),
          this.formBuilder.control(false)
        ]),
        workPhoneNumber: [''],
        workExtension: [''],
        reusingPhone: [''],
        computerEquipment: ['none'],
        // Tablet Profile controls
        rolesRequiredNewHires: ['', Validators.required],
        deliveryNote: [''],
        cargasAppRole: [''],
        dataCollectionRole: [''],
        milestoneRole: [''],
        xrsRole: [''],
        solarConnectionRole: [''],
        toddsRediMixRole: ['']
      }),
      applicationSoftware: this.formBuilder.array([
        this.formBuilder.group({
          applicationSoftware: [''],
          applicationAccessNote: ['']
        })
      ]),
      folderSharepoint: this.formBuilder.array([
        this.formBuilder.group({
          type: [''],
          folderSharepointMailbox: ['']
        })
      ])
    });
  }

  /**
   * Setup form value change subscriptions
   */
  private setupFormSubscriptions(): void {
    // Email required conditional logic for Microsoft Office License section
    this.promotionForm.get('itInfo.emailRequired')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(value => {
        this.handleEmailRequiredChange(value);
      });

    // Phone types mutual exclusion logic (Company Cell Phone and BYOD Cell Phone)
    const phoneTypesArray = this.promotionForm.get('itInfo.phoneTypes') as any;
    if (phoneTypesArray) {
      // Desk Phone (index 0) - clear related fields when unchecked, default reusingPhone to 'no' when checked
      phoneTypesArray.at(0)?.valueChanges.subscribe((isChecked: boolean) => {
        if (isChecked) {
          this.promotionForm.get('itInfo.reusingPhone')?.setValue('no');
        } else {
          this.promotionForm.get('itInfo.workPhoneNumber')?.setValue('');
          this.promotionForm.get('itInfo.workExtension')?.setValue('');
          this.promotionForm.get('itInfo.reusingPhone')?.setValue('');
        }
      });

      // Company Cell Phone (index 1) subscription
      phoneTypesArray.at(1)?.valueChanges.subscribe((isChecked: boolean) => {
        if (isChecked) {
          this.handlePhoneTypeChange(1, isChecked);
        }
      });

      // BYOD Cell Phone (index 2) subscription
      phoneTypesArray.at(2)?.valueChanges.subscribe((isChecked: boolean) => {
        if (isChecked) {
          this.handlePhoneTypeChange(2, isChecked);
        }
      });
    }

    // New Payroll Company change handler - reload all company-specific reference data
    this.promotionForm.get('positionInfo.newPayrollCompany')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(companyId => {
        // Skip this subscription during initial employee data population
        if (this.isPopulatingEmployeeData) {
          console.log('Skipping newPayrollCompany subscription - populating employee data');
          return;
        }
        if (this.selectedEmployee && companyId && !this.isEditMode) {
          const companyCode = this.getPayrollCompanyCode(companyId);
          if (companyCode) {
            console.log('New Payroll Company changed, clearing dependent dropdowns and reloading reference data for company code:', companyCode);
            // Clear dependent dropdown values and data
            this.clearCompanyDependentDropdowns();
            // Clear email address since payroll dept will change
            this.promotionForm.get('itInfo.emailAddress')?.setValue('');
            // Reload all reference data for the new company
            this.loadBuildingAccessRequirementsWithDualFilter();
            this.loadTabletProfilesByCompanyCode();
            this.loadReferenceDataByCompanyCode(companyCode);
          }
        }
      });

    // New Payroll Dept change handler - reload supervisors based on selected company and dept
    this.promotionForm.get('positionInfo.newPayrollDept')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(deptId => {
        // Skip this subscription during initial employee data population
        if (this.isPopulatingEmployeeData) {
          console.log('Skipping newPayrollDept subscription - populating employee data');
          return;
        }
        const companyId = this.promotionForm.get('positionInfo.newPayrollCompany')?.value;
        if (companyId && deptId) {
          const companyCode = this.getPayrollCompanyCode(companyId);
          const deptCode = this.getPayrollDeptCode(deptId);
          if (companyCode && deptCode) {
            console.log('New Payroll Dept changed, reloading supervisors for company:', companyCode, 'dept:', deptCode);
            this.loadSupervisorsByCompanyAndDept(companyCode, deptCode);
            // Generate new work email based on new dept's email domain
            this.generateNewWorkEmail(deptCode);
          }
        }
      });
  }

  /**
   * Create FormArray controls for building access checkboxes
   */
  private createBuildingAccessFormArray(): void {
    const buildingAccessArray = this.promotionForm.get('buildingAccess') as any;
    if (buildingAccessArray) {
      // Clear existing controls
      while (buildingAccessArray.length > 0) {
        buildingAccessArray.removeAt(0);
      }
      // Add a checkbox control for each building access item
      this.availableBuildingAccess.forEach(() => {
        buildingAccessArray.push(this.formBuilder.control(false));
      });
    }
  }

  /**
   * Load only companies on page load
   * Other reference data will be loaded after employee selection with proper company filtering
   */
  private loadCompaniesOnly(): void {
    this.referenceDataService.getCompanies()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.payrollCompanies = response.data || [];
        },
        error: (error: any) => console.error('Error loading companies:', error)
      });
  }

  /**
   * Load employee license classes (global data, not company-specific)
   * This is called during component initialization
   */
  private loadEmployeeLicenseClasses(): void {
    console.log('[loadEmployeeLicenseClasses] Loading employee license classes...');
    this.referenceDataService.getEmployeeLicenseClassesWithCache()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          console.log('[loadEmployeeLicenseClasses] Response:', response);
          if (response.success && response.data) {
            this.employeeLicenseClasses = response.data;
            // Map the data for the dropdown with display text combining licenseClass and description
            this.employeeLicenseClassesForDropdown = this.employeeLicenseClasses.map((item: any) => ({
              ...item,
              displayText: `${item.licenseClass} - ${item.description}`
            }));
            console.log('[loadEmployeeLicenseClasses] Successfully loaded', this.employeeLicenseClassesForDropdown.length, 'license classes');
            console.log('[loadEmployeeLicenseClasses] Sample data:', this.employeeLicenseClassesForDropdown.slice(0, 3));
          } else {
            console.error('[loadEmployeeLicenseClasses] Failed - response.success is false or no data');
            console.error('[loadEmployeeLicenseClasses] Full response:', response);
            this.toasterService.showError('Failed to load driver classification data', 'Error');
            this.employeeLicenseClasses = [];
            this.employeeLicenseClassesForDropdown = [];
          }
        },
        error: (error: any) => {
          console.error('[loadEmployeeLicenseClasses] Error:', error);
          this.toasterService.showError('Error loading driver classification data', 'Error');
          this.employeeLicenseClasses = [];
          this.employeeLicenseClassesForDropdown = [];
        }
      });
  }

  private loadReferenceData(): void {
    // Load all reference data in parallel
    this.referenceDataService.getCompanies()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.payrollCompanies = response.data || [];
        },
        error: (error: any) => console.error('Error loading companies:', error)
      });

    this.referenceDataService.getPayrollGroups()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.payrollGroups = response.data || [];
        },
        error: (error: any) => console.error('Error loading payroll groups:', error)
      });

    this.referenceDataService.getPayrollDepartments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.payrollDepts = response.data || [];
        },
        error: (error: any) => console.error('Error loading payroll depts:', error)
      });

    this.referenceDataService.getPositions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.positions = response.data || [];
        },
        error: (error: any) => console.error('Error loading positions:', error)
      });

    this.referenceDataService.getSupervisors()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.supervisors = response.data || [];
        },
        error: (error: any) => console.error('Error loading supervisors:', error)
      });

    this.referenceDataService.getPhysicalLocations()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.physicalLocations = response.data || [];
        },
        error: (error: any) => console.error('Error loading physical locations:', error)
      });

    this.referenceDataService.getEmploymentStatuses()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.employmentStatuses = response.data || [];
        },
        error: (error: any) => console.error('Error loading employment statuses:', error)
      });

    this.referenceDataService.getEmployeeSalaryTypes()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.employeeSalaryTypes = response.data || [];
        },
        error: (error: any) => console.error('Error loading employee salary types:', error)
      });

    // Note: Employee license classes are now loaded via loadEmployeeLicenseClasses() in ngOnInit

    this.referenceDataService.getComputerRequirements()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.computerRequirements = response.data || [];
          this.organizeComputerRequirements();
        },
        error: (error: any) => console.error('Error loading computer requirements:', error)
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupSearchDebounce(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(searchTerm => {
        this.performSearch(searchTerm);
      });
  }

  onSearchInputChange(searchTerm: string): void {
    this.searchInput = searchTerm;
    if (searchTerm.length >= 2) {
      this.searchSubject.next(searchTerm);
    } else {
      this.searchResults = [];
      this.showSearchResults = false;
    }
  }

  private performSearch(searchTerm: string): void {
    this.isLoading = true;
    this.employeeService.getEmployeesByHRRequest('promotion-request', 1, 25, undefined, false, searchTerm)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.searchResults = response.data || [];
          this.showSearchResults = this.searchResults.length > 0;
          this.isLoading = false;
        },
        error: (error: any) => {
          console.error('Employee search error:', error);
          this.searchResults = [];
          this.showSearchResults = false;
          this.isLoading = false;
          this.toasterService.showError('Error searching employees');
        }
      });
  }

  async selectEmployee(employee: EmployeeDto): Promise<void> {
    this.selectedEmployee = employee;
    this.formData.selectedEmployeeId = employee.employeeNumber;
    this.formData.selectedEmployeeName = employee.employeeName;
    this.searchResults = [];
    this.showSearchResults = false;
    this.showPositionComparison = true;

    console.log('[selectEmployee] Employee selected:', employee.employeeName);
    console.log('[selectEmployee] Employee company code:', employee.companyCode);

    // Clear effective date and notes when new employee is selected
    this.formData.effectiveDate = undefined;
    this.formData.notes = undefined;

    // Clear all dropdown arrays to ensure fresh data
    this.clearAllDropdownArrays();

    // Reset Additional Information and IT Related Information tabs
    this.resetAdditionalAndITInformation();

    // Load reference data with employee's payroll company code (falls back to company code)
    // PayrollCompanyCode maps to the Viewpoint PRCo used by reference data tables
    if (employee.payrollCompanyCode || employee.companyCode) {
      const companyCode = employee.payrollCompanyCode ?? parseInt(employee.companyCode, 10);
      console.log('[selectEmployee] Loading reference data for company code:', companyCode);

      // Load reference data and wait for it to complete
      await this.loadReferenceDataByCompanyCodeAsync(companyCode);

      console.log('[selectEmployee] Reference data loaded:', {
        payrollGroups: this.payrollGroups.length,
        payrollDepts: this.payrollDepts.length,
        positions: this.positions.length,
        supervisors: this.supervisors.length,
        physicalLocations: this.physicalLocations.length,
        employmentStatuses: this.employmentStatuses.length,
        employeeSalaryTypes: this.employeeSalaryTypes.length
      });

      // Now populate current position data with the loaded reference data
      this.populateCurrentPosition(employee);
    } else {
      console.warn('[selectEmployee] Employee has no company code - cannot load reference data');
    }

    // Load building access requirements filtered by company code
    this.loadBuildingAccessRequirements(employee);

    // Load tablet profiles filtered by company code
    this.loadTabletProfiles(employee);

    // Load applications filtered by company code
    this.loadApplications(employee);

    // Load computer requirements from API and organize for display
    this.loadComputerRequirements();

    this.showConditionalSections = true;
  }

  /**
   * Reset Additional Information and IT Related Information tabs when a new employee is selected
   */
  private resetAdditionalAndITInformation(): void {
    // Reset IT Tab visited flag
    this.hasVisitedITTab = false;
    console.log('[resetAdditionalAndITInformation] Reset hasVisitedITTab to false');

    // Reset Credit Cards Section
    this.promotionForm.patchValue({
      kwikTripCard: 'no',
      companyExpenseCard: 'no',
      creditExpenseType: '',
      weeklyLimit: '',
      fuelCardlockAccess: 'no',
      cardlockShipAddress: ''
    });

    // Reset Company Vehicles Section
    this.promotionForm.patchValue({
      companyVehicleApproved: 'no',
      driverClassification: '',
      companyCarNeeded: 'no',
      applicationPart2: 'no'
    });

    // Hide vehicle fields when employee is selected/changed
    this.showVehicleFields = false;

    // Reset Building Access array
    const buildingAccessArray = this.promotionForm.get('buildingAccess') as FormArray;
    buildingAccessArray.clear();

    // Also clear the available building access arrays to prevent template errors
    // Note: buildingAccessColumns is a getter that auto-computes from availableBuildingAccess
    this.availableBuildingAccess = [];
    this.buildingAccessRequirements = [];

    // Reset IT Related Information
    const itInfo = this.promotionForm.get('itInfo') as FormGroup;
    itInfo.patchValue({
      emailRequired: 'no',
      microsoftLicense: '',
      phoneTypes: [false, false, false],
      workPhoneNumber: '',
      workExtension: '',
      reusingPhone: '',
      computerEquipment: 'none',
      rolesRequiredNewHires: '',
      deliveryNote: '',
      cargasAppRole: '',
      dataCollectionRole: '',
      milestoneRole: '',
      xrsRole: '',
      solarConnectionRole: '',
      toddsRediMixRole: ''
    });

    // Reset phone types array explicitly
    const phoneTypesArray = itInfo.get('phoneTypes') as FormArray;
    phoneTypesArray.at(0)?.setValue(false);
    phoneTypesArray.at(1)?.setValue(false);
    phoneTypesArray.at(2)?.setValue(false);

    // Reset Application/Software array - keep one empty row
    const applicationArray = this.promotionForm.get('applicationSoftware') as FormArray;
    applicationArray.clear();
    applicationArray.push(this.formBuilder.group({
      applicationSoftware: [''],
      applicationAccessNote: ['']
    }));

    // Reset Folder/Sharepoint array - keep one empty row
    const folderArray = this.promotionForm.get('folderSharepoint') as FormArray;
    folderArray.clear();
    folderArray.push(this.formBuilder.group({
      type: [''],
      folderSharepointMailbox: ['']
    }));
  }

  /**
   * Load and filter building access requirements by employee's company code
   * This is called initially when employee is selected
   */
  private loadBuildingAccessRequirements(employee: EmployeeDto): void {
    if (!employee.companyCode) {
      console.log('Employee has no company code');
      this.availableBuildingAccess = [];
      this.createBuildingAccessFormArray();
      return;
    }

    const employeeCompanyCode = parseInt(employee.companyCode, 10);
    console.log('Loading building access for employee company code:', employeeCompanyCode);

    // Load building access requirements from API with company code parameter
    this.loadBuildingAccessByCompanyCode(employeeCompanyCode);
  }

  /**
   * Load building access requirements with dual filter
   * Called when new payroll company changes
   */
  private loadBuildingAccessRequirementsWithDualFilter(): void {
    if (!this.selectedEmployee || !this.selectedEmployee.companyCode) {
      console.log('No selected employee or company code');
      this.availableBuildingAccess = [];
      this.createBuildingAccessFormArray();
      return;
    }

    const employeeCompanyCode = parseInt(this.selectedEmployee.companyCode, 10);

    // Get the selected new payroll company code
    const newPayrollCompanyId = this.promotionForm.get('positionInfo.newPayrollCompany')?.value;
    const newPayrollCompanyCode = newPayrollCompanyId ? this.getPayrollCompanyCode(newPayrollCompanyId) : null;

    // Determine which company code to use
    // If New Payroll Company is selected, use that; otherwise use employee's current company
    const filterCompanyCode = newPayrollCompanyCode !== null ? newPayrollCompanyCode : employeeCompanyCode;

    console.log('New Payroll Company changed - loading building access for company code:', filterCompanyCode);

    // Load building access requirements from API with the determined company code
    this.loadBuildingAccessByCompanyCode(filterCompanyCode);
  }

  /**
   * Load building access requirements from API by company code
   */
  private loadBuildingAccessByCompanyCode(companyCode: number): void {
    // Skip loading in edit/view mode if building access is already loaded
    if (this.isEditMode && this.buildingAccessRequirements.length > 0) {
      console.log('Skipping building access reload in view mode - already loaded');
      return;
    }

    console.log('Calling API to load building access for company code:', companyCode);

    this.referenceDataService.getBuildingAccessRequirementsWithCache(companyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          console.log('Building access response received:', response);
          const buildingAccessData = response.data || [];
          console.log('Building access requirements loaded:', buildingAccessData);

          // Store the full building access data for later lookup
          this.buildingAccessRequirements = buildingAccessData;

          // Extract location/building names from description field for display
          this.availableBuildingAccess = buildingAccessData.map((item: any) =>
            item.description || item.buildingName || item.locationName || item.name || ''
          ).filter((name: string) => name);

          console.log('Available building access names:', this.availableBuildingAccess);

          // Create FormArray with appropriate number of controls
          this.createBuildingAccessFormArray();
        },
        error: (error: any) => {
          console.error('Error loading building access requirements:', error);
          this.buildingAccessRequirements = [];
          this.availableBuildingAccess = [];
          this.createBuildingAccessFormArray();
        }
      });
  }

  /**
   * Async version of loadBuildingAccessByCompanyCode for use in view mode loading
   */
  private async loadBuildingAccessByCompanyCodeAsync(companyCode: number): Promise<void> {
    console.log('Calling API to load building access for company code (async):', companyCode);

    try {
      const response = await this.referenceDataService.getBuildingAccessRequirementsWithCache(companyCode)
        .pipe(takeUntil(this.destroy$))
        .toPromise();

      console.log('Building access response received:', response);
      const buildingAccessData = response?.data || [];
      console.log('Building access requirements loaded:', buildingAccessData);

      // Store the full building access data for later lookup
      this.buildingAccessRequirements = buildingAccessData;

      // Extract location/building names from description field for display
      this.availableBuildingAccess = buildingAccessData.map((item: any) =>
        item.description || item.buildingName || item.locationName || item.name || ''
      ).filter((name: string) => name);

      console.log('Available building access names:', this.availableBuildingAccess);

      // Create FormArray with appropriate number of controls
      this.createBuildingAccessFormArray();
    } catch (error) {
      console.error('Error loading building access requirements:', error);
      this.buildingAccessRequirements = [];
      this.availableBuildingAccess = [];
      this.createBuildingAccessFormArray();
    }
  }

  /**
   * Load and filter tablet profiles by employee's company code
   * This is called initially when employee is selected
   */
  private loadTabletProfiles(employee: EmployeeDto): void {
    if (!employee.companyCode) {
      console.log('Employee has no company code');
      this.availableRoles = [];
      this.tabletProfiles = [];
      return;
    }

    const employeeCompanyCode = parseInt(employee.companyCode, 10);
    console.log('Loading tablet profiles for employee company code:', employeeCompanyCode);

    // Load tablet profiles by company code
    this.loadTabletProfilesFromAPI(employeeCompanyCode);
  }

  /**
   * Load tablet profiles based on selected New Payroll Company
   * Called when new payroll company changes
   */
  private loadTabletProfilesByCompanyCode(): void {
    if (!this.selectedEmployee || !this.selectedEmployee.companyCode) {
      console.log('No selected employee or company code');
      this.availableRoles = [];
      this.tabletProfiles = [];
      return;
    }

    const employeeCompanyCode = parseInt(this.selectedEmployee.companyCode, 10);

    // Get the selected new payroll company code
    const newPayrollCompanyId = this.promotionForm.get('positionInfo.newPayrollCompany')?.value;
    const newPayrollCompanyCode = newPayrollCompanyId ? this.getPayrollCompanyCode(newPayrollCompanyId) : null;

    // Determine which company code to use
    // If New Payroll Company is selected, use that; otherwise use employee's current company
    const filterCompanyCode = newPayrollCompanyCode !== null ? newPayrollCompanyCode : employeeCompanyCode;

    console.log('New Payroll Company changed - loading tablet profiles for company code:', filterCompanyCode);

    // Load tablet profiles from API with the determined company code
    this.loadTabletProfilesFromAPI(filterCompanyCode);
  }

  /**
   * Load tablet profiles from API by company code
   */
  private loadTabletProfilesFromAPI(companyCode: number): void {
    // Skip loading in edit/view mode if tablet profiles are already loaded
    if (this.isEditMode && this.tabletProfiles.length > 0) {
      console.log('Skipping tablet profiles reload in view mode - already loaded');
      return;
    }

    console.log('Calling API to load tablet profiles for company code:', companyCode);

    this.referenceDataService.getTabletProfiles(companyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          console.log('Tablet profiles response received:', response);
          this.tabletProfiles = response.data || [];
          console.log('Tablet profiles loaded:', this.tabletProfiles);

          // Update available roles from tablet profiles with filtering and sorting
          this.updateAvailableRolesFromTabletProfiles();
          console.log('Available roles:', this.availableRoles);
        },
        error: (error: any) => {
          console.error('Error loading tablet profiles:', error);
          this.availableRoles = [];
          this.tabletProfiles = [];
        }
      });
  }

  /**
   * Async version of loadTabletProfilesFromAPI for use in view mode loading
   */
  private async loadTabletProfilesFromAPIAsync(companyCode: number): Promise<void> {
    console.log('Calling API to load tablet profiles for company code (async):', companyCode);

    try {
      const response = await this.referenceDataService.getTabletProfiles(companyCode)
        .pipe(takeUntil(this.destroy$))
        .toPromise();

      console.log('Tablet profiles response received:', response);
      this.tabletProfiles = response?.data || [];
      console.log('Tablet profiles loaded:', this.tabletProfiles);

      // Update available roles from tablet profiles with filtering and sorting
      this.updateAvailableRolesFromTabletProfiles();
      console.log('Available roles:', this.availableRoles);
    } catch (error) {
      console.error('Error loading tablet profiles:', error);
      this.availableRoles = [];
      this.tabletProfiles = [];
    }
  }

  /**
   * Clear all dropdown arrays when selecting a new employee
   * This ensures we start with fresh data
   */
  private clearAllDropdownArrays(): void {
    // Clear Current Position dropdown arrays
    this.currentPayrollCompaniesForDropdown = [];
    this.currentPayrollGroupsForDropdown = [];
    this.currentPayrollDeptsForDropdown = [];
    this.currentPositionsForDropdown = [];
    this.currentSupervisorsForDropdown = [];
    this.currentPhysicalLocationsForDropdown = [];
    this.currentEmploymentStatusesForDropdown = [];
    this.currentEmployeeSalaryTypesForDropdown = [];

    // Clear New Position dropdown arrays
    this.newPayrollCompaniesForDropdown = [];
    this.newPayrollGroupsForDropdown = [];
    this.newPayrollDeptsForDropdown = [];
    this.newPositionsForDropdown = [];
    this.newSupervisorsForDropdown = [];
    this.newPhysicalLocationsForDropdown = [];
    this.newEmploymentStatusesForDropdown = [];
    this.newEmployeeSalaryTypesForDropdown = [];

    // Clear main reference data arrays
    this.payrollGroups = [];
    this.payrollDepts = [];
    this.positions = [];
    this.supervisors = [];
    this.physicalLocations = [];
    this.employmentStatuses = [];
    this.employeeSalaryTypes = [];
  }

  /**
   * Load reference data by company code and return a Promise
   * This allows us to wait for the data to load before populating fields
   */
  private loadReferenceDataByCompanyCodeAsync(companyCode: number): Promise<void> {
    return new Promise((resolve, reject) => {
      let completedRequests = 0;
      const totalRequests = 7; // payroll groups, depts, positions, supervisors, locations, statuses, salary types

      const checkComplete = () => {
        completedRequests++;
        if (completedRequests === totalRequests) {
          resolve();
        }
      };

      // Load Payroll Groups
      this.referenceDataService.getPayrollGroups(companyCode)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: any) => {
            this.payrollGroups = response.data || [];
            this.newPayrollGroupsForDropdown = this.payrollGroups.map(group => ({
              ...group,
              displayText: `${group.groupCode} - ${group.groupName}`
            }));
            checkComplete();
          },
          error: (error: any) => {
            console.error('Error loading payroll groups:', error);
            checkComplete();
          }
        });

      // Load Payroll Departments
      this.referenceDataService.getPayrollDepartmentsByCompanyWithCache(companyCode)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: any) => {
            this.payrollDepts = response.data || [];
            this.newPayrollDeptsForDropdown = this.payrollDepts.map(dept => ({
              ...dept,
              displayText: `${dept.deptCode} - ${dept.deptName}`
            }));
            checkComplete();
          },
          error: (error: any) => {
            console.error('Error loading payroll depts:', error);
            checkComplete();
          }
        });

      // Load Positions
      this.referenceDataService.getPositionsWithCache(companyCode)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: any) => {
            this.positions = response.data || [];
            this.newPositionsForDropdown = this.positions.map(position => ({
              ...position,
              displayText: `${position.positionCode} - ${position.positionName}`
            }));
            checkComplete();
          },
          error: (error: any) => {
            console.error('Error loading positions:', error);
            checkComplete();
          }
        });

      // Load Supervisors
      this.referenceDataService.getSupervisors(companyCode)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: any) => {
            this.supervisors = response.data || [];
            this.newSupervisorsForDropdown = this.supervisors.map(supervisor => ({
              ...supervisor,
              displayText: `${supervisor.firstName} ${supervisor.lastName} (${supervisor.employeeNumber})`
            }));

            if (this.newSupervisorsForDropdown.length === 0) {
              this.newSupervisorConfig.placeholder = 'Please contact HR';
            } else {
              this.newSupervisorConfig.placeholder = 'Select supervisor...';
            }
            checkComplete();
          },
          error: (error: any) => {
            console.error('Error loading supervisors:', error);
            checkComplete();
          }
        });

      // Load Physical Locations
      this.referenceDataService.getPhysicalLocationsWithCache()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: any) => {
            this.physicalLocations = response.data || [];
            this.newPhysicalLocationsForDropdown = this.physicalLocations.map(location => ({
              ...location,
              displayText: `${location.locationCode} - ${location.locationName}`
            }));
            checkComplete();
          },
          error: (error: any) => {
            console.error('Error loading physical locations:', error);
            checkComplete();
          }
        });

      // Load Employment Statuses (filtered by company code)
      this.referenceDataService.getEmploymentStatusesWithCache(companyCode)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: any) => {
            this.employmentStatuses = response.data || [];
            this.newEmploymentStatusesForDropdown = this.employmentStatuses.map(status => ({
              ...status,
              displayText: `${status.status} - ${status.description}`
            }));
            checkComplete();
          },
          error: (error: any) => {
            console.error('Error loading employment statuses:', error);
            checkComplete();
          }
        });

      // Load Employee Salary Types (filtered by company code)
      this.referenceDataService.getEmployeeSalaryTypesWithCache(companyCode)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: any) => {
            this.employeeSalaryTypes = response.data || [];
            this.newEmployeeSalaryTypesForDropdown = this.employeeSalaryTypes.map(salaryType => ({
              ...salaryType,
              displayText: salaryType.description
            }));
            checkComplete();
          },
          error: (error: any) => {
            console.error('Error loading employee salary types:', error);
            checkComplete();
          }
        });
    });
  }

  /**
   * Load reference data filtered by company code
   * Called when New Position Company Code changes
   */
  private loadReferenceDataByCompanyCode(companyCode: number): void {
    console.log('Loading reference data for company code:', companyCode);

    // Load Payroll Groups
    this.referenceDataService.getPayrollGroups(companyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.payrollGroups = response.data || [];
          // Populate dropdown array for display
          this.newPayrollGroupsForDropdown = this.payrollGroups.map(group => ({
            ...group,
            displayText: `${group.groupCode} - ${group.groupName}`
          }));
          console.log('Payroll groups loaded for company:', companyCode);
        },
        error: (error: any) => console.error('Error loading payroll groups:', error)
      });

    // Load Payroll Departments
    this.referenceDataService.getPayrollDepartmentsByCompanyWithCache(companyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.payrollDepts = response.data || [];
          // Populate dropdown array for display
          this.newPayrollDeptsForDropdown = this.payrollDepts.map(dept => ({
            ...dept,
            displayText: `${dept.deptCode} - ${dept.deptName}`
          }));
          console.log('Payroll departments loaded for company:', companyCode);
        },
        error: (error: any) => console.error('Error loading payroll depts:', error)
      });

    // Load Positions
    this.referenceDataService.getPositionsWithCache(companyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.positions = response.data || [];
          // Populate dropdown array for display
          this.newPositionsForDropdown = this.positions.map(position => ({
            ...position,
            displayText: `${position.positionCode} - ${position.positionName}`
          }));
          console.log('Positions loaded for company:', companyCode);
        },
        error: (error: any) => console.error('Error loading positions:', error)
      });

    // Load Supervisors (without dept filter - get all for the company)
    this.referenceDataService.getSupervisors(companyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.supervisors = response.data || [];
          // Populate dropdown array for display
          this.newSupervisorsForDropdown = this.supervisors.map(supervisor => ({
            ...supervisor,
            displayText: `${supervisor.firstName} ${supervisor.lastName} (${supervisor.employeeNumber})`
          }));

          // Update placeholder based on whether supervisors are available
          if (this.newSupervisorsForDropdown.length === 0) {
            this.newSupervisorConfig.placeholder = 'Please contact HR';
          } else {
            this.newSupervisorConfig.placeholder = 'Select supervisor...';
          }

          console.log('Supervisors loaded for company:', companyCode);
        },
        error: (error: any) => console.error('Error loading supervisors:', error)
      });

    // Load Physical Locations
    this.referenceDataService.getPhysicalLocationsWithCache()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.physicalLocations = response.data || [];
          // Populate dropdown array for display
          this.newPhysicalLocationsForDropdown = this.physicalLocations.map(location => ({
            ...location,
            displayText: `${location.locationCode} - ${location.locationName}`
          }));
          console.log('Physical locations loaded for company:', companyCode);
        },
        error: (error: any) => console.error('Error loading physical locations:', error)
      });

    // Load Employment Statuses
    this.referenceDataService.getEmploymentStatusesWithCache(companyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.employmentStatuses = response.data || [];
          // Populate dropdown array for display
          this.newEmploymentStatusesForDropdown = this.employmentStatuses.map(status => ({
            ...status,
            displayText: `${status.status} - ${status.description}`
          }));
          console.log('Employment statuses loaded for company:', companyCode);
        },
        error: (error: any) => console.error('Error loading employment statuses:', error)
      });

    // Load Employee Salary Types
    this.referenceDataService.getEmployeeSalaryTypesWithCache(companyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.employeeSalaryTypes = response.data || [];
          // Populate dropdown array for display
          this.newEmployeeSalaryTypesForDropdown = this.employeeSalaryTypes.map(salaryType => ({
            ...salaryType,
            displayText: salaryType.description
          }));
          console.log('Employee salary types loaded for company:', companyCode);
        },
        error: (error: any) => console.error('Error loading employee salary types:', error)
      });
  }

  /**
   * Clear company-dependent dropdown values and filtered dropdown arrays
   * Called when Payroll Company changes to reset all dependent fields
   */
  private clearCompanyDependentDropdowns(): void {
    const positionInfo = this.promotionForm.get('positionInfo');

    // Clear form values for company-dependent dropdowns in New Position
    positionInfo?.get('newPayrollGroup')?.setValue('');
    positionInfo?.get('newPayrollDept')?.setValue('');
    positionInfo?.get('newPosition')?.setValue('');
    positionInfo?.get('newSupervisor')?.setValue('');
    positionInfo?.get('newPhysicalLocation')?.setValue('');
    positionInfo?.get('newEmploymentStatus')?.setValue('');
    positionInfo?.get('newEmployeeSalaryType')?.setValue('');

    // Clear the filtered dropdown arrays (for display) but NOT the main data arrays
    // The main arrays will be reloaded by loadReferenceDataByCompanyCode
    this.newPayrollGroupsForDropdown = [];
    this.newPayrollDeptsForDropdown = [];
    this.newPositionsForDropdown = [];
    this.newSupervisorsForDropdown = [];
    this.newPhysicalLocationsForDropdown = [];
    this.newEmploymentStatusesForDropdown = [];
    this.newEmployeeSalaryTypesForDropdown = [];

    console.log('Cleared all company-dependent dropdown values and filtered arrays');
  }

  /**
   * Load supervisors filtered by company code and payroll dept code
   * Called when New Position Payroll Dept changes
   */
  private loadSupervisorsByCompanyAndDept(companyCode: number, deptCode: number): void {
    console.log('Loading supervisors for company:', companyCode, 'dept:', deptCode);

    this.referenceDataService.getSupervisorsWithCache(companyCode, deptCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.supervisors = response.data || [];
          console.log('Supervisors loaded:', this.supervisors.length);
        },
        error: (error: any) => {
          console.error('Error loading supervisors:', error);
          this.supervisors = [];
        }
      });
  }

  /**
   * Load applications filtered by employee's company code
   */
  private loadApplications(employee: EmployeeDto): void {
    if (!employee.companyCode) {
      console.log('Employee has no company code');
      this.applicationsForDropdown = [];
      return;
    }

    const employeeCompanyCode = parseInt(employee.companyCode, 10);
    console.log('Loading applications for company code:', employeeCompanyCode);

    // Load applications for the company
    this.referenceDataService.getApplicationsWithCache(employeeCompanyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          console.log('Applications response received:', response);
          if (response.success && response.data) {
            this.applicationsForDropdown = response.data.map((app: any) => ({
              ...app,
              displayText: app.name
            }));
            console.log('Applications loaded:', this.applicationsForDropdown);
          } else {
            console.error('Failed to load applications for company:', employeeCompanyCode, response.errors);
            this.applicationsForDropdown = [];
          }
        },
        error: (error: any) => {
          console.error('Error loading applications:', error);
          this.applicationsForDropdown = [];
        }
      });
  }

  /**
   * Async version of loadApplications for view mode
   */
  private async loadApplicationsAsync(companyCode: number): Promise<void> {
    console.log('[loadApplicationsAsync] Loading applications for company code:', companyCode);

    try {
      const response: any = await firstValueFrom(
        this.referenceDataService.getApplicationsWithCache(companyCode)
      );

      if (response.success && response.data) {
        this.applicationsForDropdown = response.data.map((app: any) => ({
          ...app,
          displayText: app.name
        }));
        console.log('[loadApplicationsAsync] Applications loaded:', this.applicationsForDropdown);
      } else {
        console.error('[loadApplicationsAsync] Failed to load applications:', response.errors);
        this.applicationsForDropdown = [];
      }
    } catch (error) {
      console.error('[loadApplicationsAsync] Error loading applications:', error);
      this.applicationsForDropdown = [];
    }
  }

  /**
   * Load computer requirements from the API
   * This is called when employee is selected to ensure computer requirements are loaded and displayed
   */
  private loadComputerRequirements(): void {
    console.log('Loading computer requirements from API');

    this.referenceDataService.getComputerRequirements()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          console.log('Computer requirements response received:', response);
          if (response.success && response.data) {
            this.computerRequirements = response.data;
            console.log('Computer requirements loaded:', this.computerRequirements.length);

            // Organize the requirements into parent and child groups for display
            this.organizeComputerRequirements();

            console.log('Parent computer requirements:', this.parentComputerRequirements.length);
            console.log('Child computer requirements groups:', this.childComputerRequirements.size);

            // Default select 'None' when loading (only in create mode)
            if (!this.isEditMode) {
              this.promotionForm.get('itInfo.computerEquipment')?.setValue('none');
              console.log('[loadComputerRequirements] Default selected "None" for Computer Equipment');
            }
          } else {
            console.error('Failed to load computer requirements:', response.errors);
            this.computerRequirements = [];
            this.parentComputerRequirements = [];
            this.childComputerRequirements.clear();
          }
        },
        error: (error: any) => {
          console.error('Error loading computer requirements:', error);
          this.computerRequirements = [];
          this.parentComputerRequirements = [];
          this.childComputerRequirements.clear();
        }
      });
  }

  /**
   * Async version of loadComputerRequirements for view mode
   */
  private async loadComputerRequirementsAsync(): Promise<void> {
    console.log('[loadComputerRequirementsAsync] Loading computer requirements from API');

    try {
      const response: any = await firstValueFrom(
        this.referenceDataService.getComputerRequirements()
      );

      if (response.success && response.data) {
        this.computerRequirements = response.data;
        console.log('[loadComputerRequirementsAsync] Computer requirements loaded:', this.computerRequirements.length);

        // Organize the requirements into parent and child groups for display
        this.organizeComputerRequirements();

        console.log('[loadComputerRequirementsAsync] Parent computer requirements:', this.parentComputerRequirements.length);
        console.log('[loadComputerRequirementsAsync] Child computer requirements groups:', this.childComputerRequirements.size);
      } else {
        console.error('[loadComputerRequirementsAsync] Failed to load computer requirements:', response.errors);
        this.computerRequirements = [];
        this.parentComputerRequirements = [];
        this.childComputerRequirements.clear();
      }
    } catch (error) {
      console.error('[loadComputerRequirementsAsync] Error loading computer requirements:', error);
      this.computerRequirements = [];
      this.parentComputerRequirements = [];
      this.childComputerRequirements.clear();
    }
  }

  private populateCurrentPosition(employee: EmployeeDto): void {
    // Populate current position data from employee
    // Filter pre-loaded reference data based on employee's current position
    this.filterCurrentPositionDropdowns(employee);
    this.filterNewPositionDropdowns(employee);
  }

  /**
   * Filter pre-loaded reference data to populate Current Position Card dropdowns
   * Shows data that matches the employee's current position attributes
   */
  private filterCurrentPositionDropdowns(employee: EmployeeDto): void {
    console.log('Filtering current position dropdowns for employee:', {
      employeeName: employee.employeeName,
      companyCode: employee.companyCode,
      payrollDeptCode: employee.payrollDeptCode,
      position: employee.position,
      physicalLocationCode: employee.physicalLocationCode,
      supervisorId: employee.supervisorId
    });

    // IMPORTANT: Clear all Current Position formData fields first to prevent old data from persisting
    this.formData.currentPayrollCompany = undefined;
    this.formData.currentPayrollGroup = undefined;
    this.formData.currentPayrollDept = undefined;
    this.formData.currentPositionId = undefined;
    this.formData.currentSupervisor = undefined;
    this.formData.currentPhysicalLocation = undefined;
    this.formData.currentStatus = undefined;
    this.formData.currentSalaryCode = undefined;
    this.formData.currentPayRate = undefined;

    const employeeCompanyCode = employee.payrollCompanyCode ?? (employee.companyCode ? parseInt(employee.companyCode, 10) : undefined);
    const employeePayrollDeptCode = employee.payrollDeptCode;
    const employeePhysicalLocationCode = employee.physicalLocationCode;

    // Filter companies - show company matching employee's company code
    if (this.payrollCompanies.length > 0) {
      this.currentPayrollCompaniesForDropdown = this.payrollCompanies.map(company => ({
        ...company,
        displayText: `${company.companyCode} - ${company.companyName}`
      }));

      // Set current company if available
      if (employeeCompanyCode) {
        const matchedCompany = this.payrollCompanies.find(c => c.companyCode === employeeCompanyCode);
        if (matchedCompany) {
          this.formData.currentPayrollCompany = matchedCompany.id;
        }
      }
    }

    // Filter payroll groups - filter by company code and group code
    if (this.payrollGroups.length > 0) {
      let filteredPayrollGroups = this.payrollGroups;

      // Filter by company code if available
      if (employeeCompanyCode) {
        filteredPayrollGroups = filteredPayrollGroups.filter(pg =>
          pg.companyCode === employeeCompanyCode
        );
      }

      this.currentPayrollGroupsForDropdown = filteredPayrollGroups.map(group => ({
        ...group,
        displayText: `${group.groupCode} - ${group.groupName}`
      }));

      console.log('[filterCurrentPosition] Payroll Groups:', {
        total: this.payrollGroups.length,
        filtered: filteredPayrollGroups.length,
        employeeGroupCode: employee.payrollGroupCode
      });

      // Try to match employee's payroll group code and auto-select
      if (employee.payrollGroupCode) {
        const matchedGroup = filteredPayrollGroups.find(g => g.groupCode === employee.payrollGroupCode);
        if (matchedGroup) {
          this.formData.currentPayrollGroup = matchedGroup.id;
          console.log('[filterCurrentPosition] Matched payroll group:', matchedGroup);
        } else {
          console.warn('[filterCurrentPosition] No payroll group matched for code:', employee.payrollGroupCode);
        }
      }
    } else {
      console.warn('[filterCurrentPosition] payrollGroups array is empty');
    }

    // Filter payroll departments - show all departments
    if (this.payrollDepts.length > 0) {
      this.currentPayrollDeptsForDropdown = this.payrollDepts.map(dept => ({
        ...dept,
        displayText: `${dept.deptCode} - ${dept.deptName}`
      }));

      // Try to match employee's payroll dept code
      if (employeePayrollDeptCode) {
        const matchedDept = this.payrollDepts.find(d => d.deptCode === employeePayrollDeptCode);
        if (matchedDept) {
          this.formData.currentPayrollDept = matchedDept.id;
        }
      }
    }

    // Filter positions - show all positions
    if (this.positions.length > 0) {
      this.currentPositionsForDropdown = this.positions.map(position => ({
        ...position,
        displayText: `${position.positionCode} - ${position.positionName}`
      }));

      console.log('[filterCurrentPosition] Positions:', {
        total: this.positions.length,
        employeePosition: employee.position
      });

      // Match employee's position name to position ID
      if (employee.position) {
        const matchedPosition = this.positions.find(p =>
          p.positionName === employee.position || p.positionCode === employee.position
        );
        if (matchedPosition) {
          this.formData.currentPositionId = matchedPosition.id;
          console.log('[filterCurrentPosition] Matched position:', matchedPosition);
        } else {
          console.warn('[filterCurrentPosition] No position matched for:', employee.position);
        }
      }
    } else {
      console.warn('[filterCurrentPosition] positions array is empty');
    }

    // Filter supervisors - show supervisors matching employee's company and dept codes
    if (this.supervisors.length > 0) {
      let filteredSupervisors = this.supervisors;

      // Filter by company code if available
      if (employeeCompanyCode) {
        filteredSupervisors = filteredSupervisors.filter(s =>
          s.companyCode === employeeCompanyCode || s.companyCode?.toString() === employee.companyCode
        );
      }

      // Filter by payroll dept code if available
      if (employeePayrollDeptCode) {
        filteredSupervisors = filteredSupervisors.filter(s => s.payrollDeptCode === employeePayrollDeptCode);
      }

      this.currentSupervisorsForDropdown = filteredSupervisors.map(supervisor => ({
        ...supervisor,
        displayText: `${supervisor.firstName} ${supervisor.lastName} (${supervisor.employeeNumber})`
      }));

      console.log('[filterCurrentPosition] Supervisors:', {
        total: this.supervisors.length,
        filtered: filteredSupervisors.length,
        employeeSupervisorId: employee.supervisorId
      });

      // Update placeholder based on whether supervisors are available
      if (filteredSupervisors.length === 0) {
        this.currentSupervisorConfig.placeholder = 'Please contact HR';
      } else {
        this.currentSupervisorConfig.placeholder = 'Select supervisor...';
      }

      // Try to match and auto-select employee's supervisor by supervisorId
      // Use firstOrDefault pattern: try to find exact match, fallback to first filtered result
      if (employee.supervisorId && filteredSupervisors.length > 0) {
        // Match by employeeNumber (supervisor's employee number = employee.supervisorId)
        let matchedSupervisor = filteredSupervisors.find(s => s.employeeNumber === employee.supervisorId);

        if (!matchedSupervisor && filteredSupervisors.length > 0) {
          matchedSupervisor = filteredSupervisors[0];
        }

        if (matchedSupervisor) {
          this.formData.currentSupervisor = matchedSupervisor.id;
          console.log('[filterCurrentPosition] Matched supervisor:', matchedSupervisor);
        } else {
          console.warn('[filterCurrentPosition] No supervisor matched');
        }
      }
    } else {
      console.warn('[filterCurrentPosition] supervisors array is empty');
    }

    // Filter physical locations - show all locations for current position
    if (this.physicalLocations.length > 0) {
      this.currentPhysicalLocationsForDropdown = this.physicalLocations.map(location => ({
        ...location,
        displayText: `${location.locationCode} - ${location.locationName}`
      }));

      // Try to match and select employee's current physical location code
      if (employeePhysicalLocationCode) {
        const matchedLocation = this.physicalLocations.find(l => l.locationCode === employeePhysicalLocationCode);
        if (matchedLocation) {
          this.formData.currentPhysicalLocation = matchedLocation.id;
        }
      }
    }

    // Filter employment statuses - show all statuses
    if (this.employmentStatuses.length > 0) {
      this.currentEmploymentStatusesForDropdown = this.employmentStatuses.map(status => ({
        ...status,
        displayText: `${status.status} - ${status.description}`
      }));

      console.log('[filterCurrentPosition] Employment Statuses:', {
        total: this.employmentStatuses.length,
        employeeStatus: employee.status
      });

      // Try to match and select employee's current employment status
      if (employee.status) {
        const matchedStatus = this.employmentStatuses.find(s => s.status === employee.status);
        if (matchedStatus) {
          this.formData.currentStatus = matchedStatus.id;
          console.log('[filterCurrentPosition] Matched status:', matchedStatus);
        } else {
          console.warn('[filterCurrentPosition] No status matched for:', employee.status);
        }
      }
    } else {
      console.warn('[filterCurrentPosition] employmentStatuses array is empty');
    }

    // Filter employee salary types - match employee's salary code and company code
    const employeeSalaryCode = employee.salaryCode;
    const employeeCompanyCodeInt = employee.companyCode ? parseInt(employee.companyCode, 10) : undefined;

    if (this.employeeSalaryTypes.length > 0) {
      // Filter salary types by company code first
      let filteredSalaryTypes = this.employeeSalaryTypes;

      // Filter by company code if available
      if (employeeCompanyCodeInt) {
        filteredSalaryTypes = filteredSalaryTypes.filter(s =>
          s.companyCode === employeeCompanyCodeInt || s.companyCode?.toString() === employee.companyCode
        );
      }

      this.currentEmployeeSalaryTypesForDropdown = filteredSalaryTypes.map(salaryType => ({
        ...salaryType,
        displayText: salaryType.description
      }));

      console.log('[filterCurrentPosition] Employee Salary Types:', {
        total: this.employeeSalaryTypes.length,
        filtered: filteredSalaryTypes.length,
        employeeSalaryCode: employeeSalaryCode
      });

      // Try to match and select employee's current salary code (pay rate) from filtered results
      // First try to find match by both salary code AND company code
      if (employeeSalaryCode && filteredSalaryTypes.length > 0) {
        let matchedSalaryType = filteredSalaryTypes.find(s =>
          s.salaryCode === employeeSalaryCode &&
          (s.companyCode === employeeCompanyCodeInt || s.companyCode?.toString() === employee.companyCode)
        );

        // Fall back to first matching salary code if no company+salary match found
        if (!matchedSalaryType) {
          matchedSalaryType = this.employeeSalaryTypes.find(s => s.salaryCode === employeeSalaryCode);
        }

        if (matchedSalaryType) {
          this.formData.currentPayRate = matchedSalaryType.id;
          console.log('[filterCurrentPosition] Matched pay rate:', matchedSalaryType);
        } else {
          console.warn('[filterCurrentPosition] No pay rate matched for code:', employeeSalaryCode);
        }
      }
    } else {
      console.warn('[filterCurrentPosition] employeeSalaryTypes array is empty');
    }
  }

  /**
   * Filter pre-loaded reference data to populate New Position Card dropdowns
   * Shows data available for the new position based on employee's company, dept, and location
   */
  private filterNewPositionDropdowns(employee: EmployeeDto): void {
    const employeeCompanyCode = employee.payrollCompanyCode ?? (employee.companyCode ? parseInt(employee.companyCode, 10) : undefined);
    const employeePayrollDeptCode = employee.payrollDeptCode;
    const employeePhysicalLocationCode = employee.physicalLocationCode;

    // For new position, we show all companies available
    if (this.payrollCompanies.length > 0) {
      this.newPayrollCompaniesForDropdown = this.payrollCompanies.map(company => ({
        ...company,
        displayText: `${company.companyCode} - ${company.companyName}`
      }));
    }

    // For new position, show payroll groups filtered by employee's company code
    if (this.payrollGroups.length > 0) {
      let filteredGroups = this.payrollGroups;

      // Filter by company code if available
      if (employeeCompanyCode) {
        filteredGroups = filteredGroups.filter(g =>
          g.companyCode === employeeCompanyCode || g.companyCode?.toString() === employee.companyCode
        );
      }

      this.newPayrollGroupsForDropdown = filteredGroups.map(group => ({
        ...group,
        displayText: `${group.groupCode} - ${group.groupName}`
      }));
    }

    // For new position, show payroll departments filtered by employee's company code
    if (this.payrollDepts.length > 0) {
      let filteredDepts = this.payrollDepts;

      // Filter by company code if available
      if (employeeCompanyCode) {
        filteredDepts = filteredDepts.filter(d =>
          d.companyCode === employeeCompanyCode || d.companyCode?.toString() === employee.companyCode
        );
      }

      this.newPayrollDeptsForDropdown = filteredDepts.map(dept => ({
        ...dept,
        displayText: `${dept.deptCode} - ${dept.deptName}`
      }));
    }

    // For new position, show positions filtered by employee's company code
    if (this.positions.length > 0) {
      let filteredPositions = this.positions;

      // Filter by company code if available
      if (employeeCompanyCode) {
        filteredPositions = filteredPositions.filter(p =>
          p.companyCode === employeeCompanyCode || p.companyCode?.toString() === employee.companyCode
        );
      }

      this.newPositionsForDropdown = filteredPositions.map(position => ({
        ...position,
        displayText: `${position.positionCode} - ${position.positionName}`
      }));
    }

    // For new position, show supervisors filtered by employee's company and dept codes
    if (this.supervisors.length > 0) {
      let filteredSupervisors = this.supervisors;

      // Filter by company code if available
      if (employeeCompanyCode) {
        filteredSupervisors = filteredSupervisors.filter(s =>
          s.companyCode === employeeCompanyCode || s.companyCode?.toString() === employee.companyCode
        );
      }

      // Filter by payroll dept code if available
      if (employeePayrollDeptCode) {
        filteredSupervisors = filteredSupervisors.filter(s => s.payrollDeptCode === employeePayrollDeptCode);
      }

      this.newSupervisorsForDropdown = filteredSupervisors.map(supervisor => ({
        ...supervisor,
        displayText: `${supervisor.firstName} ${supervisor.lastName} (${supervisor.employeeNumber})`
      }));
    }

    // For new position, show all available physical locations
    if (this.physicalLocations.length > 0) {
      this.newPhysicalLocationsForDropdown = this.physicalLocations.map(location => ({
        ...location,
        displayText: `${location.locationCode} - ${location.locationName}`
      }));
    }

    // For new position, show all employment statuses
    if (this.employmentStatuses.length > 0) {
      this.newEmploymentStatusesForDropdown = this.employmentStatuses.map(status => ({
        ...status,
        displayText: `${status.status} - ${status.description}`
      }));
    }

    // For new position, show all salary types
    if (this.employeeSalaryTypes.length > 0) {
      this.newEmployeeSalaryTypesForDropdown = this.employeeSalaryTypes.map(salaryType => ({
        ...salaryType,
        displayText: salaryType.description
      }));
    }

    // Auto-populate New Position fields with employee's current data
    this.populateNewPositionWithCurrentData(employee);
  }

  /**
   * Auto-populate New Position fields with employee's current data
   * This pre-fills the form so users can easily see and modify what needs to change
   */
  private populateNewPositionWithCurrentData(employee: EmployeeDto): void {
    // Set flag to prevent subscription from firing during population
    this.isPopulatingEmployeeData = true;

    const employeeCompanyCode = employee.payrollCompanyCode ?? (employee.companyCode ? parseInt(employee.companyCode, 10) : undefined);
    const positionInfoGroup = this.promotionForm?.get('positionInfo') as any;

    console.log('[populateNewPosition] Starting to populate new position with employee data');
    console.log('[populateNewPosition] Dropdown arrays status:', {
      newPayrollDeptsForDropdown: this.newPayrollDeptsForDropdown.length,
      newSupervisorsForDropdown: this.newSupervisorsForDropdown.length,
      newPhysicalLocationsForDropdown: this.newPhysicalLocationsForDropdown.length,
      newEmploymentStatusesForDropdown: this.newEmploymentStatusesForDropdown.length,
      newEmployeeSalaryTypesForDropdown: this.newEmployeeSalaryTypesForDropdown.length
    });
    console.log('[populateNewPosition] Dropdown array IDs:', {
      payrollDeptIds: this.newPayrollDeptsForDropdown.map(d => d.id),
      supervisorIds: this.newSupervisorsForDropdown.map(s => s.id),
      physicalLocationIds: this.newPhysicalLocationsForDropdown.map(l => l.id),
      statusIds: this.newEmploymentStatusesForDropdown.map(s => s.id),
      salaryTypeIds: this.newEmployeeSalaryTypesForDropdown.map(s => s.id)
    });

    if (!positionInfoGroup) return;

    // IMPORTANT: Clear all New Position form fields first to prevent old data from persisting
    positionInfoGroup.patchValue({
      newPayrollCompany: '',
      newPayrollGroup: '',
      newPayrollDept: '',
      newPosition: '',
      newSupervisor: '',
      newPhysicalLocation: '',
      newStatus: '',
      newpayRate: ''
    });

    // Also clear formData New Position fields
    this.formData.newPayrollCompany = undefined;
    this.formData.newPayrollGroup = undefined;
    this.formData.newPayrollDept = undefined;
    this.formData.newPosition = undefined;
    this.formData.newSupervisor = undefined;
    this.formData.newPhysicalLocation = undefined;
    this.formData.newStatus = undefined;
    this.formData.newSalaryCode = undefined;
    this.formData.newpayRate = undefined;

    // Auto-select payroll company
    if (employeeCompanyCode && this.payrollCompanies.length > 0) {
      const matchedCompany = this.payrollCompanies.find(c => c.companyCode === employeeCompanyCode);
      if (matchedCompany) {
        positionInfoGroup.patchValue({ newPayrollCompany: matchedCompany.id });
        this.formData.newPayrollCompany = matchedCompany.id;
      }
    }

    // Auto-select payroll group
    if (employee.payrollGroupCode && this.payrollGroups.length > 0) {
      // Try to match by both groupCode and companyCode first (most specific match)
      let matchedGroup = this.payrollGroups.find(g =>
        g.groupCode === employee.payrollGroupCode &&
        (g.companyCode === employeeCompanyCode || g.companyCode?.toString() === employee.companyCode)
      );

      // If not found by company code, try just groupCode (fallback for legacy data)
      if (!matchedGroup) {
        matchedGroup = this.payrollGroups.find(g => g.groupCode === employee.payrollGroupCode);
      }

      if (matchedGroup) {
        positionInfoGroup.patchValue({ newPayrollGroup: matchedGroup.id });
        this.formData.newPayrollGroup = matchedGroup.id;

        // IMPORTANT: Ensure the matched payroll group is in the newPayrollGroupsForDropdown array
        // The dropdown is filtered by company code, but the employee's current payroll group might be from a different company
        const groupExistsInDropdown = this.newPayrollGroupsForDropdown.some(g => g.id === matchedGroup!.id);
        if (!groupExistsInDropdown) {
          this.newPayrollGroupsForDropdown.push({
            ...matchedGroup,
            displayText: `${matchedGroup.groupCode} - ${matchedGroup.groupName}`
          });
        }
      }
    }

    // Auto-select payroll dept
    if (employee.payrollDeptCode && this.payrollDepts.length > 0) {
      const matchedDept = this.payrollDepts.find(d =>
        d.deptCode === employee.payrollDeptCode &&
        (d.companyCode === employeeCompanyCode || d.companyCode?.toString() === employee.companyCode)
      );
      if (matchedDept) {
        positionInfoGroup.patchValue({ newPayrollDept: matchedDept.id });
        this.formData.newPayrollDept = matchedDept.id;
        console.log('[populateNewPosition] Matched payroll dept:', matchedDept);
      } else {
        console.warn('[populateNewPosition] No payroll dept matched for code:', employee.payrollDeptCode);
      }
    }

    // Auto-select position by matching position name
    if (employee.position && this.positions.length > 0) {
      // Try exact match first on positionName
      let matchedPosition = this.positions.find(p => p.positionName === employee.position);

      // If no exact match, try case-insensitive match
      if (!matchedPosition) {
        matchedPosition = this.positions.find(p =>
          p.positionName && p.positionName.toLowerCase() === employee.position.toLowerCase()
        );
      }

      // If still no match, try matching by position code if employee has one
      if (!matchedPosition && employeeCompanyCode) {
        matchedPosition = this.positions.find(p =>
          p.companyCode === employeeCompanyCode && p.positionName === employee.position
        );
      }

      if (matchedPosition) {
        positionInfoGroup.patchValue({ newPosition: matchedPosition.id });
        this.formData.newPosition = matchedPosition.id;

        // IMPORTANT: Ensure the matched position is in the newPositionsForDropdown array
        // The dropdown is filtered by company code, but the employee's current position might be from a different company
        const positionExistsInDropdown = this.newPositionsForDropdown.some(p => p.id === matchedPosition!.id);
        console.log('[populateNewPosition] Position check:', {
          matchedPositionId: matchedPosition.id,
          matchedPositionCompanyCode: matchedPosition.companyCode,
          employeeCompanyCode: employeeCompanyCode,
          positionExistsInDropdown: positionExistsInDropdown,
          dropdownCount: this.newPositionsForDropdown.length
        });
        if (!positionExistsInDropdown) {
          console.log('[populateNewPosition] Adding position to dropdown array:', matchedPosition);
          this.newPositionsForDropdown.push({
            ...matchedPosition,
            displayText: `${matchedPosition.positionCode} - ${matchedPosition.positionName}`
          });
          console.log('[populateNewPosition] Dropdown count after adding:', this.newPositionsForDropdown.length);
        }
        console.log('[populateNewPosition] Matched position:', matchedPosition);
      } else {
        console.warn('[populateNewPosition] No position matched for:', employee.position);
      }
    }

    // Auto-select supervisor by supervisor ID
    if (employee.supervisorId && this.supervisors.length > 0) {
      // Try matching by employeeNumber first (supervisor's employee number = employee.supervisorId)
      let matchedSupervisor = this.supervisors.find(s => s.employeeNumber === employee.supervisorId);

      // If not found, try matching by employeeId (fallback)
      if (!matchedSupervisor) {
        matchedSupervisor = this.supervisors.find(s => s.employeeId === employee.supervisorId);
      }

      // If not found, try matching by id property (fallback)
      if (!matchedSupervisor) {
        matchedSupervisor = this.supervisors.find(s => s.id === employee.supervisorId);
      }

      // If still not found, try matching by supervisorId property (last resort fallback)
      if (!matchedSupervisor) {
        matchedSupervisor = this.supervisors.find(s => s.supervisorId === employee.supervisorId);
      }

      if (matchedSupervisor) {
        positionInfoGroup.patchValue({ newSupervisor: matchedSupervisor.id });
        this.formData.newSupervisor = matchedSupervisor.id;
        console.log('[populateNewPosition] Matched supervisor:', matchedSupervisor);
      } else {
        console.warn('[populateNewPosition] No supervisor matched for ID:', employee.supervisorId);
      }
    }

    // Auto-select physical location
    if (employee.physicalLocationCode && this.physicalLocations.length > 0) {
      const matchedLocation = this.physicalLocations.find(l => l.locationCode === employee.physicalLocationCode);
      if (matchedLocation) {
        positionInfoGroup.patchValue({ newPhysicalLocation: matchedLocation.id });
        this.formData.newPhysicalLocation = matchedLocation.id;
        console.log('[populateNewPosition] Matched physical location:', matchedLocation);
      } else {
        console.warn('[populateNewPosition] No physical location matched for code:', employee.physicalLocationCode);
      }
    }

    // Auto-select employment status
    if (employee.status && this.employmentStatuses.length > 0) {
      const matchedStatus = this.employmentStatuses.find(s => s.status === employee.status);
      if (matchedStatus) {
        positionInfoGroup.patchValue({ newStatus: matchedStatus.id });
        this.formData.newStatus = matchedStatus.id;
        console.log('[populateNewPosition] Matched employment status:', matchedStatus);
      } else {
        console.warn('[populateNewPosition] No employment status matched for:', employee.status);
      }
    }

    // Auto-select pay rate (salary type)
    if (employee.salaryCode && this.employeeSalaryTypes.length > 0) {
      const matchedSalaryType = this.employeeSalaryTypes.find(s =>
        s.salaryCode === employee.salaryCode &&
        (s.companyCode === employeeCompanyCode || s.companyCode?.toString() === employee.companyCode)
      );
      if (matchedSalaryType) {
        positionInfoGroup.patchValue({ newpayRate: matchedSalaryType.id });
        this.formData.newpayRate = matchedSalaryType.id;
        console.log('[populateNewPosition] Matched pay rate:', matchedSalaryType);
      } else {
        console.warn('[populateNewPosition] No pay rate matched for code:', employee.salaryCode);
      }
    }

    // Auto-populate new salary code from current salary code
    if (employee.salaryCode) {
      this.formData.newSalaryCode = employee.salaryCode;
      console.log('[populateNewPosition] Auto-populated newSalaryCode:', employee.salaryCode);
    }

    console.log('[populateNewPosition] Final form values:', positionInfoGroup.value);

    // Reset the flag after population is complete
    this.isPopulatingEmployeeData = false;
  }

  clearSelectedEmployee(): void {
    this.selectedEmployee = null;
    this.searchInput = '';
    this.showPositionComparison = false;
    this.showConditionalSections = false;
    this.showVehicleFields = false;
    this.formData = {};
  }

  /**
   * Handle new pay rate (salary type) selection change
   * Updates formData.newSalaryCode with the actual salaryCode from the selected item
   */
  onNewPayRateSelected(selectedItem: any): void {
    if (selectedItem) {
      // Find the salary type to get the actual salaryCode
      const salaryType = this.employeeSalaryTypes.find(s => s.id === selectedItem.id);
      if (salaryType) {
        this.formData.newSalaryCode = salaryType.salaryCode;
        console.log('[onNewPayRateSelected] Updated newSalaryCode:', salaryType.salaryCode);
      }
    } else {
      this.formData.newSalaryCode = undefined;
    }
  }

  /**
   * Check if a form field has errors
   */
  isFieldInvalid(fieldName: string): boolean {
    const field = this.promotionForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  /**
   * Get error message for a form field
   */
  getFieldError(fieldName: string): string {
    const field = this.promotionForm.get(fieldName);
    if (!field || !field.errors) return '';

    if (field.errors['required']) {
      return `${this.formatFieldName(fieldName)} is required`;
    }
    return '';
  }

  /**
   * Format field name for display (e.g., 'kwikTripCard' -> 'Kwik Trip Card')
   */
  private formatFieldName(fieldName: string): string {
    return fieldName
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
      .trim();
  }

  /**
   * Sync form values to formData before submission
   */
  private syncFormToFormData(): void {
    if (this.promotionForm) {
      const formValue = this.promotionForm.value;
      this.formData.kwikTripCard = formValue.kwikTripCard;
      this.formData.companyExpenseCard = formValue.companyExpenseCard;
      this.formData.creditExpenseType = formValue.creditExpenseType;
      this.formData.weeklyLimit = formValue.weeklyLimit;
      this.formData.fuelCardlockAccess = formValue.fuelCardlockAccess;
      this.formData.cardlockShipAddress = formValue.cardlockShipAddress;
      this.formData.companyVehicleApproved = formValue.companyVehicleApproved;
    }
  }

  showTab(tabName: string): void {
    this.activeTab = tabName;

    // Track if IT Related Information tab has been visited
    if (tabName === 'it') {
      this.hasVisitedITTab = true;
      console.log('[showTab] IT Related Information tab visited');
    }

    // Scroll to the top of the tabs section
    const anchor = document.getElementById('tabs-anchor');
    if (anchor) {
      anchor.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  }

  onCompanyExpenseCardChange(): void {
    const isYes = this.formData.companyExpenseCard === 'yes';
    // Show/hide conditional fields and reset form controls
    if (!isYes) {
      this.formData.creditExpenseType = undefined;
      this.formData.weeklyLimit = undefined;
      this.promotionForm.patchValue({
        creditExpenseType: '',
        weeklyLimit: ''
      });
    }
  }

  onFuelCardlockAccessChange(): void {
    const isYes = this.formData.fuelCardlockAccess === 'yes';
    if (!isYes) {
      this.formData.cardlockShipAddress = undefined;
    }
  }

  onCompanyVehicleApprovedChange(): void {
    const vehicleFormGroup = this.promotionForm?.get('vehicleInfo') as any;
    const isYes = vehicleFormGroup?.get('approvedVehicle')?.value === 'yes';
    this.showVehicleFields = isYes;
    console.log('[onCompanyVehicleApprovedChange] showVehicleFields:', this.showVehicleFields);
    console.log('[onCompanyVehicleApprovedChange] employeeLicenseClassesForDropdown length:', this.employeeLicenseClassesForDropdown.length);
    if (isYes && this.employeeLicenseClassesForDropdown.length === 0) {
      console.warn('[onCompanyVehicleApprovedChange] WARNING: Vehicle fields shown but no license classes available!');
    }
  }

  /**
   * Get building access items distributed in 3 columns for grid layout
   */
  get buildingAccessColumns(): string[][] {
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

  /**
   * Get form control index for building access checkbox based on column and item position
   */
  getItemFormIndex(columnIndex: number, itemIndex: number): number {
    const itemsPerColumn = Math.ceil(this.availableBuildingAccess.length / 3);
    return columnIndex * itemsPerColumn + itemIndex;
  }

  /**
   * Handle building access checkbox change
   */
  onBuildingAccessChange(columnIndex: number, itemIndex: number, event: any): void {
    // If a building access checkbox is checked, uncheck the "use existing key fob" checkbox
    if (event.target.checked) {
      this.promotionForm.get('useExistingKeyFob')?.setValue(false, { emitEvent: false });
    }
    console.log('Building access updated:', {
      columnIndex,
      itemIndex,
      checked: event.target.checked
    });
  }

  /**
   * Handle "use existing key fob" checkbox change - unchecks all building access checkboxes
   */
  onUseExistingKeyFobChange(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    if (checkbox.checked) {
      const buildingAccessArray = this.promotionForm.get('buildingAccess') as FormArray;
      if (buildingAccessArray) {
        buildingAccessArray.controls.forEach(control => control.setValue(false, { emitEvent: false }));
      }
    }
  }

  goBack(): void {
    this.location.back();
  }

  handleLogout(): void {
    this.authService.logout();
  }

  submitForm(): void {
    // Validate reactive form fields
    if (this.promotionForm && this.promotionForm.invalid) {
      this.toasterService.showError('Please fill all required fields in the form');
      return;
    }

    // Sync form values to formData
    this.syncFormToFormData();

    // Validate required fields
    if (!this.selectedEmployee || !this.formData.newPosition) {
      this.toasterService.showError('Please fill all required fields');
      return;
    }

    // Check if IT Related Information tab has been visited
    if (!this.hasVisitedITTab) {
      // Show IT Tab Review confirmation dialog
      this.showITTabReviewDialog = true;
      console.log('[submitForm] IT Tab not visited - showing review dialog');
      return; // Stop submission, wait for user response
    }

    // IT tab visited - continue with computer requirements check
    this.checkComputerRequirementsAndSubmit();
  }

  /**
   * Check Computer Requirements and proceed with submission
   * Called after IT Tab review check passes
   */
  private checkComputerRequirementsAndSubmit(): void {
    // Check if Computer Requirements are selected
    const itInfoFormGroup = this.promotionForm?.get('itInfo') as any;
    const parentEquipmentId = itInfoFormGroup?.get('computerEquipment')?.value;

    if (!parentEquipmentId || parentEquipmentId === 'none') {
      // 'None' is selected or nothing selected - proceed without showing dialog
      // since user explicitly chose 'None' or it's defaulted
      this.proceedWithSubmission();
      return;
    }

    // Computer requirements selected or user chose to proceed - continue with submission
    this.proceedWithSubmission();
  }

  /**
   * Handle user confirming to review IT Related Information tab
   * Switches to IT Related Information tab and scrolls to Other Info section
   */
  onITTabReviewConfirmed(): void {
    this.showITTabReviewDialog = false;

    // Switch to IT Related Information tab (this will also set hasVisitedITTab to true)
    this.showTab('it');

    // Wait for the tab to render, then scroll to Other Info section
    setTimeout(() => {
      const otherInfoSection = document.getElementById('other-info-section');
      if (otherInfoSection) {
        otherInfoSection.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });

        // Add a subtle highlight effect to draw attention
        otherInfoSection.style.transition = 'background-color 0.5s ease';
        otherInfoSection.style.backgroundColor = '#fff3cd';

        setTimeout(() => {
          otherInfoSection.style.backgroundColor = '';
        }, 2000);
      }
    }, 100);

    console.log('[onITTabReviewConfirmed] Switched to IT Related Information tab and focused on Other Info section');
  }

  /**
   * Handle user choosing to submit without reviewing IT Tab
   * Proceeds with submission
   */
  onITTabReviewCancelled(): void {
    this.showITTabReviewDialog = false;

    // Mark as visited since user chose to skip
    this.hasVisitedITTab = true;

    // Continue with computer requirements check and submission
    this.checkComputerRequirementsAndSubmit();

    console.log('[onITTabReviewCancelled] User chose to submit anyway');
  }

  /**
   * Handle IT Tab Review dialog closed
   */
  onITTabReviewDialogClosed(): void {
    this.showITTabReviewDialog = false;
  }

  /**
   * Handle user confirming to review Computer Requirements
   * Switches to IT Related Information tab and scrolls to Computer Requirements section
   */
  onComputerRequirementsConfirmed(): void {
    this.showComputerRequirementsDialog = false;

    // Switch to IT Related Information tab
    this.activeTab = 'it';

    // Wait for the tab to render, then scroll to Computer Requirements section
    setTimeout(() => {
      const computerRequirementsSection = document.getElementById('computer-requirements-section');
      if (computerRequirementsSection) {
        computerRequirementsSection.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });

        // Optional: Add a subtle highlight effect to draw attention
        computerRequirementsSection.style.transition = 'background-color 0.5s ease';
        computerRequirementsSection.style.backgroundColor = '#fff3cd';

        setTimeout(() => {
          computerRequirementsSection.style.backgroundColor = '';
        }, 2000);
      }
    }, 100);
  }

  /**
   * Handle user choosing to proceed without reviewing Computer Requirements
   * Proceeds with form submission
   */
  onComputerRequirementsCancelled(): void {
    this.showComputerRequirementsDialog = false;

    // Proceed with submission by calling the submission logic
    this.proceedWithSubmission();
  }

  /**
   * Handle dialog closed
   */
  onComputerRequirementsDialogClosed(): void {
    this.showComputerRequirementsDialog = false;
  }

  /**
   * Proceed with form submission (extracted from submitForm)
   * This is called when user chooses to proceed without Computer Requirements
   */
  private proceedWithSubmission(): void {
    this.isSaving = true;

    // Build CreatePromotionRequestDto with proper structure
    const positionInfoGroup = this.promotionForm?.get('positionInfo') as any;

    const promotionRequestDto: CreatePromotionRequestDto = {
      notes: this.formData.notes,
      employeeId: parseInt(this.formData.selectedEmployeeId || '0', 10),

      // Current position fields (from selected employee data)
      currentPayrollCompanyCode: this.selectedEmployee!.companyCode ? parseInt(this.selectedEmployee!.companyCode.toString(), 10) : undefined,
      currentPayrollGroupCode: this.selectedEmployee!.payrollGroupCode ? parseInt(this.selectedEmployee!.payrollGroupCode.toString(), 10) : undefined,
      currentPayrollDeptCode: this.selectedEmployee!.payrollDeptCode ? parseInt(this.selectedEmployee!.payrollDeptCode.toString(), 10) : undefined,
      currentPositionCode: this.selectedEmployee!.position,
      currentSupervisorId: this.selectedEmployee!.supervisorId ? parseInt(this.selectedEmployee!.supervisorId.toString(), 10) : undefined,
      currentPhysicalLocationCode: this.selectedEmployee!.physicalLocationCode ? parseInt(this.selectedEmployee!.physicalLocationCode.toString(), 10) : undefined,
      currentStatus: this.selectedEmployee!.status,
      currentSalaryCode: this.selectedEmployee!.salaryCode,
      currentWorkEmail: this.selectedEmployee!.workEmail || undefined,

      // New position fields (required) - read from positionInfo FormGroup
      // Note: Form stores IDs, but we convert them to business codes for backend
      newPayrollCompanyCode: this.getPayrollCompanyCode(positionInfoGroup?.get('newPayrollCompany')?.value) || 0,
      newPayrollGroupCode: this.getPayrollGroupCode(positionInfoGroup?.get('newPayrollGroup')?.value) || 0,
      newPayrollDeptCode: this.getPayrollDeptCode(positionInfoGroup?.get('newPayrollDept')?.value) || 0,
      newPositionCode: this.getPositionCode(positionInfoGroup?.get('newPosition')?.value) || '',
      newSupervisorId: this.getSupervisorEmployeeNumber(positionInfoGroup?.get('newSupervisor')?.value) || 0,
      newPhysicalLocationCode: this.getPhysicalLocationCode(positionInfoGroup?.get('newPhysicalLocation')?.value) || 0,
      newStatus: this.getEmploymentStatusText(positionInfoGroup?.get('newStatus')?.value) || '',
      newSalaryCode: this.formData.newSalaryCode ? parseInt(this.formData.newSalaryCode.toString(), 10) : undefined,
      newWorkEmail: this.promotionForm.get('itInfo.emailAddress')?.value || undefined,

      // Effective date (from formData using ngModel)
      effectiveDate: this.formData.effectiveDate || new Date().toISOString().split('T')[0],

      // Credit Card Information
      creditCardInfo: this.buildCreditCardInfo(),

      // Vehicle Information
      vehicleInfo: this.buildVehicleInfo(),

      // IT Information
      itInfo: this.buildITInfo(),

      // Phone Requirements
      phoneInfo: this.buildPhoneInfo(),

      // Collections
      applications: this.buildApplications(),
      folders: this.buildFolders(),
      tabletProfiles: this.buildTabletProfiles(),
      computerRequirements: this.buildComputerRequirements(),
      buildingAccess: this.buildBuildingAccess(),
      useExistingKeyFob: this.promotionForm.get('useExistingKeyFob')?.value || false
    };

    // Log the entire CreatePromotionRequestDto for debugging comparison with backend
    console.log('[PROMOTION-FRONTEND] ===== SENDING CreatePromotionRequestDto =====');
    console.log('[PROMOTION-FRONTEND] employeeId:', promotionRequestDto.employeeId);
    console.log('[PROMOTION-FRONTEND] notes:', promotionRequestDto.notes);
    console.log('[PROMOTION-FRONTEND] --- Current Position ---');
    console.log('[PROMOTION-FRONTEND] currentPayrollCompanyCode:', promotionRequestDto.currentPayrollCompanyCode);
    console.log('[PROMOTION-FRONTEND] currentPayrollGroupCode:', promotionRequestDto.currentPayrollGroupCode);
    console.log('[PROMOTION-FRONTEND] currentPayrollDeptCode:', promotionRequestDto.currentPayrollDeptCode);
    console.log('[PROMOTION-FRONTEND] currentPositionCode:', promotionRequestDto.currentPositionCode);
    console.log('[PROMOTION-FRONTEND] currentSupervisorId:', promotionRequestDto.currentSupervisorId);
    console.log('[PROMOTION-FRONTEND] currentPhysicalLocationCode:', promotionRequestDto.currentPhysicalLocationCode);
    console.log('[PROMOTION-FRONTEND] currentStatus:', promotionRequestDto.currentStatus);
    console.log('[PROMOTION-FRONTEND] currentSalaryCode:', promotionRequestDto.currentSalaryCode);
    console.log('[PROMOTION-FRONTEND] --- New Position ---');
    console.log('[PROMOTION-FRONTEND] newPayrollCompanyCode:', promotionRequestDto.newPayrollCompanyCode);
    console.log('[PROMOTION-FRONTEND] newPayrollGroupCode:', promotionRequestDto.newPayrollGroupCode);
    console.log('[PROMOTION-FRONTEND] newPayrollDeptCode:', promotionRequestDto.newPayrollDeptCode);
    console.log('[PROMOTION-FRONTEND] newPositionCode:', promotionRequestDto.newPositionCode);
    console.log('[PROMOTION-FRONTEND] newSupervisorId:', promotionRequestDto.newSupervisorId);
    console.log('[PROMOTION-FRONTEND] newPhysicalLocationCode:', promotionRequestDto.newPhysicalLocationCode);
    console.log('[PROMOTION-FRONTEND] newStatus:', promotionRequestDto.newStatus);
    console.log('[PROMOTION-FRONTEND] newSalaryCode:', promotionRequestDto.newSalaryCode);
    console.log('[PROMOTION-FRONTEND] effectiveDate:', promotionRequestDto.effectiveDate);
    console.log('[PROMOTION-FRONTEND] --- Credit Card Info ---');
    if (promotionRequestDto.creditCardInfo) {
      console.log('[PROMOTION-FRONTEND] kwikTripCard:', promotionRequestDto.creditCardInfo.kwikTripCard);
      console.log('[PROMOTION-FRONTEND] companyExpenseCard:', promotionRequestDto.creditCardInfo.companyExpenseCard);
      console.log('[PROMOTION-FRONTEND] creditExpenseType:', promotionRequestDto.creditCardInfo.creditExpenseType);
      console.log('[PROMOTION-FRONTEND] weeklyLimit:', promotionRequestDto.creditCardInfo.weeklyLimit);
      console.log('[PROMOTION-FRONTEND] fuelCardlockAccess:', promotionRequestDto.creditCardInfo.fuelCardlockAccess);
      console.log('[PROMOTION-FRONTEND] fuelCardlockAddress:', promotionRequestDto.creditCardInfo.fuelCardlockAddress);
    } else {
      console.log('[PROMOTION-FRONTEND] creditCardInfo: NULL');
    }
    console.log('[PROMOTION-FRONTEND] --- Vehicle Info ---');
    if (promotionRequestDto.vehicleInfo) {
      console.log('[PROMOTION-FRONTEND] isApprovedToOperate:', promotionRequestDto.vehicleInfo.isApprovedToOperate);
      console.log('[PROMOTION-FRONTEND] licenseClass:', promotionRequestDto.vehicleInfo.licenseClass);
      console.log('[PROMOTION-FRONTEND] drugAndAlcoholProfile:', promotionRequestDto.vehicleInfo.drugAndAlcoholProfile);
      console.log('[PROMOTION-FRONTEND] needCompanyCar:', promotionRequestDto.vehicleInfo.needCompanyCar);
      console.log('[PROMOTION-FRONTEND] isApplicationPart2Complete:', promotionRequestDto.vehicleInfo.isApplicationPart2Complete);
    } else {
      console.log('[PROMOTION-FRONTEND] vehicleInfo: NULL');
    }
    console.log('[PROMOTION-FRONTEND] --- IT Info ---');
    if (promotionRequestDto.itInfo) {
      console.log('[PROMOTION-FRONTEND] emailRequired:', promotionRequestDto.itInfo.emailRequired);
      console.log('[PROMOTION-FRONTEND] alternateDeliveryLocation:', promotionRequestDto.itInfo.alternateDeliveryLocation);
      console.log('[PROMOTION-FRONTEND] mSOfficeLicenseE5:', promotionRequestDto.itInfo.mSOfficeLicenseE5);
      console.log('[PROMOTION-FRONTEND] mSOfficeLicenseF3:', promotionRequestDto.itInfo.mSOfficeLicenseF3);
    } else {
      console.log('[PROMOTION-FRONTEND] itInfo: NULL');
    }
    console.log('[PROMOTION-FRONTEND] --- Phone Info ---');
    if (promotionRequestDto.phoneInfo) {
      console.log('[PROMOTION-FRONTEND] deskPhone:', promotionRequestDto.phoneInfo.deskPhone);
      console.log('[PROMOTION-FRONTEND] companyCellphone:', promotionRequestDto.phoneInfo.companyCellphone);
      console.log('[PROMOTION-FRONTEND] byodCellphone:', promotionRequestDto.phoneInfo.byodCellphone);
      console.log('[PROMOTION-FRONTEND] workPhoneNumber:', promotionRequestDto.phoneInfo.workPhoneNumber);
      console.log('[PROMOTION-FRONTEND] workExtension:', promotionRequestDto.phoneInfo.workExtension);
      console.log('[PROMOTION-FRONTEND] reusingExistingPhone:', promotionRequestDto.phoneInfo.reusingExistingPhone);
    } else {
      console.log('[PROMOTION-FRONTEND] phoneInfo: NULL');
    }
    console.log('[PROMOTION-FRONTEND] --- Applications (Count:', promotionRequestDto.applications?.length ?? 0, ') ---');
    if (promotionRequestDto.applications && promotionRequestDto.applications.length > 0) {
      promotionRequestDto.applications.forEach((app) => {
        console.log('[PROMOTION-FRONTEND]   applicationId:', app.applicationId, ', applicationName:', app.applicationName);
      });
    }
    console.log('[PROMOTION-FRONTEND] --- Folders (Count:', promotionRequestDto.folders?.length ?? 0, ') ---');
    if (promotionRequestDto.folders && promotionRequestDto.folders.length > 0) {
      promotionRequestDto.folders.forEach((folder) => {
        console.log('[PROMOTION-FRONTEND]   folderType:', folder.folderType, ', folderName:', folder.folderName);
      });
    }
    console.log('[PROMOTION-FRONTEND] --- Tablet Profiles (Count:', promotionRequestDto.tabletProfiles?.length ?? 0, ') ---');
    if (promotionRequestDto.tabletProfiles && promotionRequestDto.tabletProfiles.length > 0) {
      promotionRequestDto.tabletProfiles.forEach((tablet) => {
        console.log('[PROMOTION-FRONTEND]   tabletProfileId:', tablet.tabletProfileId, ', tabletProfileName:', tablet.tabletProfileName, ', roles:', tablet.rolesRequiredForNewHire);
      });
    }
    console.log('[PROMOTION-FRONTEND] --- Computer Requirements (Count:', promotionRequestDto.computerRequirements?.length ?? 0, ') ---');
    if (promotionRequestDto.computerRequirements && promotionRequestDto.computerRequirements.length > 0) {
      promotionRequestDto.computerRequirements.forEach((comp) => {
        console.log('[PROMOTION-FRONTEND]   requirementId:', comp.requirementId, ', requirementName:', comp.requirementName);
      });
    }
    console.log('[PROMOTION-FRONTEND] --- Building Access (Count:', promotionRequestDto.buildingAccess?.length ?? 0, ') ---');
    if (promotionRequestDto.buildingAccess && promotionRequestDto.buildingAccess.length > 0) {
      promotionRequestDto.buildingAccess.forEach((access) => {
        console.log('[PROMOTION-FRONTEND]   accessId:', access.accessId, ', accessDescription:', access.accessDescription);
      });
    }
    console.log('[PROMOTION-FRONTEND] ===== END CreatePromotionRequestDto =====');

    this.hrRequestService.createPromotionRequest(promotionRequestDto)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.isSaving = false;
          this.toasterService.showSuccess('Promotion request submitted successfully');
          this.router.navigate(['/']);
        },
        error: (error: any) => {
          this.isSaving = false;
          console.error('Error submitting promotion request:', error);
          this.toasterService.showError('Error submitting promotion request');
        }
      });
  }

  private buildCreditCardInfo() {
    const hasCardData = this.formData.kwikTripCard || this.formData.companyExpenseCard ||
                       this.formData.creditExpenseType ||
                       this.formData.fuelCardlockAccess;

    if (!hasCardData) return undefined;

    return {
      kwikTripCard: this.formData.kwikTripCard === 'yes',
      companyExpenseCard: this.formData.companyExpenseCard === 'yes',
      creditExpenseType: this.formData.creditExpenseType || undefined,
      weeklyLimit: this.formData.weeklyLimit ? parseInt(this.formData.weeklyLimit.toString(), 10) : undefined,
      fuelCardlockAccess: this.formData.fuelCardlockAccess === 'yes',
      fuelCardlockAddress: this.formData.cardlockShipAddress
    };
  }

  private buildVehicleInfo(): PromotionVehicleInfoDto | undefined {
    const vehicleFormGroup = this.promotionForm?.get('vehicleInfo') as any;
    const approvedVehicle = vehicleFormGroup?.get('approvedVehicle')?.value;
    const driverClassification = vehicleFormGroup?.get('driverClassification')?.value;
    const drugAlcoholProfile = vehicleFormGroup?.get('drugAlcoholProfile')?.value;
    const companyCarNeeded = vehicleFormGroup?.get('companyCarNeeded')?.value;
    const applicationPart2 = vehicleFormGroup?.get('applicationPart2')?.value;

    const hasVehicleData = approvedVehicle || driverClassification || drugAlcoholProfile || companyCarNeeded || applicationPart2;

    if (!hasVehicleData) return undefined;

    return {
      isApprovedToOperate: approvedVehicle === 'yes',
      licenseClass: this.getDriverClassificationText(driverClassification) || undefined,
      drugAndAlcoholProfile: drugAlcoholProfile || undefined,
      needCompanyCar: companyCarNeeded === 'yes',
      isApplicationPart2Complete: applicationPart2 === 'yes'
    };
  }

  private buildITInfo() {
    const itInfoFormGroup = this.promotionForm?.get('itInfo') as any;
    const emailRequired = itInfoFormGroup?.get('emailRequired')?.value;
    const computerEquipment = itInfoFormGroup?.get('computerEquipment')?.value;
    const microsoftLicense = itInfoFormGroup?.get('microsoftLicense')?.value;

    const hasITData = emailRequired === 'yes' || computerEquipment;

    if (!hasITData) return undefined;

    // Initialize both MS Office licenses to false
    let mSOfficeLicenseE5 = false;
    let mSOfficeLicenseF3 = false;

    // Set the correct license based on microsoftLicense value
    if (microsoftLicense === 'e5') {
      mSOfficeLicenseE5 = true;
    } else if (microsoftLicense === 'f3') {
      mSOfficeLicenseF3 = true;
    }

    return {
      emailRequired: emailRequired === 'yes',
      alternateDeliveryLocation: itInfoFormGroup?.get('deliveryNote')?.value,
      mSOfficeLicenseE5: mSOfficeLicenseE5,
      mSOfficeLicenseF3: mSOfficeLicenseF3
    };
  }

  private buildPhoneInfo() {
    const itInfoFormGroup = this.promotionForm?.get('itInfo');
    const phoneTypesArray = itInfoFormGroup?.get('phoneTypes') as any;
    const phoneTypesValues = phoneTypesArray?.value || [false, false, false];

    const hasPhoneData = phoneTypesValues.some((val: boolean) => val) ||
                        itInfoFormGroup?.get('workPhoneNumber')?.value;

    if (!hasPhoneData) return undefined;

    return {
      deskPhone: phoneTypesValues[0] === true,
      companyCellphone: phoneTypesValues[1] === true,
      byodCellphone: phoneTypesValues[2] === true,
      workPhoneNumber: itInfoFormGroup?.get('workPhoneNumber')?.value,
      workExtension: itInfoFormGroup?.get('workExtension')?.value,
      reusingExistingPhone: itInfoFormGroup?.get('reusingPhone')?.value === 'yes'
    };
  }

  private buildApplications() {
    const applicationArray = this.promotionForm?.get('applicationSoftware') as any;
    if (!applicationArray || applicationArray.length === 0) return undefined;

    return applicationArray.value
      .filter((app: any) => app.applicationSoftware)
      .map((app: any) => ({
        applicationId: parseInt(app.applicationSoftware),
        accessNotes: app.applicationAccessNote || null
      }));
  }

  private buildFolders() {
    const folderArray = this.promotionForm?.get('folderSharepoint') as any;
    if (!folderArray || folderArray.length === 0) return undefined;

    return folderArray.value
      .filter((folder: any) => folder.folderSharepointMailbox || folder.type)
      .map((folder: any) => ({
        folderType: folder.type || 'Folder',
        folderName: folder.folderSharepointMailbox
      }));
  }

  private buildTabletProfiles(): PromotionTabletProfileDto[] | undefined {
    const itInfoFormGroup = this.promotionForm?.get('itInfo');
    const rolesRequired = itInfoFormGroup?.get('rolesRequiredNewHires')?.value;

    if (!rolesRequired) return undefined;

    // Find the selected profile from tabletProfiles list using rolesRequired as ID
    const selectedProfile = this.tabletProfiles.find((p: any) => p.id === parseInt(rolesRequired));
    if (!selectedProfile) return undefined;

    // Try to get the optional textbox value for this role
    const roleTextboxName = this.getRoleTextboxControlName(rolesRequired);
    const roleSpecificInfo = roleTextboxName ? itInfoFormGroup?.get(roleTextboxName)?.value : undefined;

    // Build tablet profiles array with the selected profile
    const tabletProfiles: PromotionTabletProfileDto[] = [{
      tabletProfileId: selectedProfile.id as number,
      tabletProfileName: (selectedProfile.displayText || selectedProfile.name) as string,
      rolesRequiredForNewHire: (roleSpecificInfo || '') as string
    }];

    return tabletProfiles.length > 0 ? tabletProfiles : undefined;
  }

  private buildComputerRequirements() {
    const itInfoFormGroup = this.promotionForm?.get('itInfo') as any;
    const parentEquipmentId = itInfoFormGroup?.get('computerEquipment')?.value;

    // If no parent equipment selected or 'None' is selected, return undefined (don't save to database)
    if (!parentEquipmentId || parentEquipmentId === 'none') return undefined;

    const computerRequirements: any[] = [];

    // Add parent requirement
    const parentId = parseInt(parentEquipmentId, 10);
    computerRequirements.push({
      computerRequirementsId: parentId,
      isChild: false,
      parentId: null
    });

    // Add selected child requirements
    Object.keys(this.selectedChildRequirements).forEach(key => {
      if (this.selectedChildRequirements[key]) {
        // Key format is "parentId_childId"
        const [parentIdStr, childIdStr] = key.split('_');
        const parentFromKey = parseInt(parentIdStr, 10);

        // Only include children of the selected parent
        if (parentFromKey === parentId) {
          const childId = parseInt(childIdStr, 10);
          computerRequirements.push({
            computerRequirementsId: childId,
            isChild: true,
            parentId: parentId
          });
        }
      }
    });

    return computerRequirements.length > 0 ? computerRequirements : undefined;
  }

  private buildBuildingAccess() {
    const buildingAccessArray = this.promotionForm?.get('buildingAccess') as FormArray;

    if (!buildingAccessArray || buildingAccessArray.length === 0) {
      return undefined;
    }

    const buildingAccessResults: { accessId: number; accessDescription: string }[] = [];

    buildingAccessArray.controls.forEach((control, index) => {
      if (control.value === true) {
        // Get the description from availableBuildingAccess at the same index
        if (this.availableBuildingAccess && this.availableBuildingAccess[index]) {
          const sortedDescription = this.availableBuildingAccess[index];

          // Find the requirement by description to get the accessId
          const requirement = this.buildingAccessRequirements.find(req =>
            req.description === sortedDescription
          );

          if (requirement) {
            buildingAccessResults.push({
              accessId: requirement.id,
              accessDescription: requirement.description
            });
          }
        }
      }
    });

    return buildingAccessResults.length > 0 ? buildingAccessResults : undefined;
  }

  /**
   * Helper methods to convert form IDs to business codes
   * Following the New Hire Request pattern for code/ID conversion
   */

  private getPayrollCompanyCode(selectedId: number | string | null): number | null {
    if (!selectedId) return null;

    const selectedCompany = this.newPayrollCompaniesForDropdown.find(
      company => company.id == selectedId
    );

    return selectedCompany ? selectedCompany.companyCode : null;
  }

  private getPayrollGroupCode(selectedId: number | string | null): number | null {
    if (!selectedId) return null;

    const selectedGroup = this.newPayrollGroupsForDropdown.find(
      group => group.id == selectedId
    );

    return selectedGroup ? selectedGroup.groupCode : null;
  }

  private getPayrollDeptCode(selectedId: number | string | null): number | null {
    if (!selectedId) return null;

    const selectedDept = this.newPayrollDeptsForDropdown.find(
      dept => dept.id == selectedId
    );

    return selectedDept ? selectedDept.deptCode : null;
  }

  private getPositionCode(selectedId: number | string | null): string | null {
    if (!selectedId) return null;

    const selectedPosition = this.newPositionsForDropdown.find(
      position => position.id == selectedId
    );

    return selectedPosition ? selectedPosition.positionCode : null;
  }

  private getSupervisorEmployeeNumber(selectedId: number | string | null): number | null {
    if (!selectedId) return null;

    const selectedSupervisor = this.newSupervisorsForDropdown.find(
      supervisor => supervisor.id == selectedId
    );

    return selectedSupervisor ? selectedSupervisor.employeeNumber : null;
  }

  private getPhysicalLocationCode(selectedId: number | string | null): number | null {
    if (!selectedId) return null;

    const selectedLocation = this.newPhysicalLocationsForDropdown.find(
      location => location.id == selectedId
    );

    return selectedLocation ? selectedLocation.locationCode : null;
  }

  private getDriverClassificationText(selectedId: number | string | null): string | null {
    if (!selectedId) return null;

    const selectedLicenseClass = this.employeeLicenseClassesForDropdown.find(
      licenseClass => licenseClass.id == selectedId
    );

    return selectedLicenseClass ? selectedLicenseClass.licenseClass : null;
  }

  private getEmploymentStatusText(selectedId: number | string | null): string | null {
    if (!selectedId) return null;

    const selectedStatus = this.newEmploymentStatusesForDropdown.find(
      status => status.id == selectedId
    );

    return selectedStatus ? selectedStatus.status : null;
  }

  cancelForm(): void {
    if (confirm('Are you sure you want to cancel? Any unsaved changes will be lost.')) {
      this.router.navigate(['/']);
    }
  }

  /**
   * Handle request cancellation event from cancel button component
   */
  onRequestCancelled(): void {
    console.log('Promotion request has been cancelled');
    // The cancel button component handles navigation, so no additional action needed
  }

  get canUpdateDate(): boolean {
    if (!this.isEditMode || !this.viewData) return false;
    const status = this.viewData.requestStatusName?.toLowerCase() || '';
    if (!status.includes('pending')) return false;
    // Disable editing if the original effective date is today or in the past
    if (this.originalEffectiveDate) {
      // Parse YYYY-MM-DD as local date (not UTC) by splitting the string
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

  updateEffectiveDate(): void {
    if (!this.parentId || this.isUpdatingDate) return;

    const effectiveDate = this.formData.effectiveDate;
    if (!effectiveDate) {
      this.toasterService.showError('Please select a date');
      return;
    }

    this.isUpdatingDate = true;
    this.hrRequestService.updateEffectiveDate(this.parentId, effectiveDate).subscribe({
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

  // Helper methods to get display text for Current Position readonly fields
  getCurrentPayrollCompanyDisplay(): string {
    if (!this.formData.currentPayrollCompany) return '';
    const item = this.currentPayrollCompaniesForDropdown.find(x => x.id === this.formData.currentPayrollCompany);
    return item?.displayText || '';
  }

  getCurrentPayrollGroupDisplay(): string {
    // In view mode, display data directly from viewData
    if (this.isEditMode && this.viewData?.currentPayrollGroupCode && this.viewData?.currentPayrollGroupName) {
      return `${this.viewData.currentPayrollGroupCode} - ${this.viewData.currentPayrollGroupName}`;
    }

    // In create mode, use the dropdown lookup
    if (!this.formData.currentPayrollGroup) return '';
    const item = this.currentPayrollGroupsForDropdown.find(x => x.id === this.formData.currentPayrollGroup);
    return item?.displayText || '';
  }

  getCurrentPayrollDeptDisplay(): string {
    // In view mode, display data directly from viewData
    if (this.isEditMode && this.viewData?.currentPayrollDeptCode && this.viewData?.currentPayrollDeptName) {
      return `${this.viewData.currentPayrollDeptCode} - ${this.viewData.currentPayrollDeptName}`;
    }

    // In create mode, use the dropdown lookup
    if (!this.formData.currentPayrollDept) return '';
    const item = this.currentPayrollDeptsForDropdown.find(x => x.id === this.formData.currentPayrollDept);
    return item?.displayText || '';
  }

  getCurrentPositionDisplay(): string {
    if (!this.formData.currentPositionId) return '';
    const item = this.currentPositionsForDropdown.find(x => x.id === this.formData.currentPositionId);
    return item?.displayText || '';
  }

  getCurrentSupervisorDisplay(): string {
    if (!this.formData.currentSupervisor) return 'Please contact HR';
    const item = this.currentSupervisorsForDropdown.find(x => x.id === this.formData.currentSupervisor);
    return item?.displayText || 'Please contact HR';
  }

  getCurrentPhysicalLocationDisplay(): string {
    if (!this.formData.currentPhysicalLocation) return '';
    const item = this.currentPhysicalLocationsForDropdown.find(x => x.id === this.formData.currentPhysicalLocation);
    return item?.displayText || '';
  }

  getCurrentEmploymentStatusDisplay(): string {
    if (!this.formData.currentStatus) return '';
    const item = this.currentEmploymentStatusesForDropdown.find(x => x.id === this.formData.currentStatus);
    return item?.displayText || '';
  }

  getCurrentPayRateDisplay(): string {
    // In view mode, use the description from the API response (resolved server-side)
    if (this.isEditMode && this.viewData?.currentSalaryCode !== undefined) {
      if (this.viewData.currentSalaryDescription) {
        return this.viewData.currentSalaryDescription;
      }
      return this.viewData.currentSalaryCode?.toString() || '';
    }
    // In create mode, try to get from dropdown first, then look up description
    if (this.formData.currentPayRate) {
      const item = this.currentEmployeeSalaryTypesForDropdown.find(x => x.id === this.formData.currentPayRate);
      if (item) return item.displayText;
    }
    // Fall back to looking up description from employee's salary code
    if (this.selectedEmployee?.salaryCode) {
      const employeeSalaryCode = this.selectedEmployee.salaryCode;
      const salaryType = this.employeeSalaryTypes.find(s =>
        s.salaryCode === employeeSalaryCode?.toString() ||
        parseInt(s.salaryCode) === employeeSalaryCode
      );
      return salaryType?.description || employeeSalaryCode.toString();
    }
    return '';
  }

  /**
   * New Position display methods for view mode
   */
  getNewPayrollCompanyDisplay(): string {
    if (!this.viewData?.newPayrollCompanyCode) return '';
    return `${this.viewData.newPayrollCompanyCode} - ${this.viewData.newCompanyName || ''}`;
  }

  getNewPayrollGroupDisplay(): string {
    if (!this.viewData?.newPayrollGroupCode) return '';

    // Use the group name from the API response (resolved server-side)
    if (this.viewData.newPayrollGroupName) {
      return `${this.viewData.newPayrollGroupCode} - ${this.viewData.newPayrollGroupName}`;
    }

    // Fallback to just the code if name not available
    return this.viewData.newPayrollGroupCode.toString();
  }

  getNewPayrollDeptDisplay(): string {
    if (!this.viewData?.newPayrollDeptCode) return '';
    return `${this.viewData.newPayrollDeptCode} - ${this.viewData.newPayrollDeptName || ''}`;
  }

  getNewPositionDisplay(): string {
    if (!this.viewData?.newPositionCode) return '';

    // Format as "CODE - Name" to match Current Position display
    if (this.viewData.newPositionName) {
      return `${this.viewData.newPositionCode} - ${this.viewData.newPositionName}`;
    }

    // Fallback to just the code if name is not available
    return this.viewData.newPositionCode;
  }

  getNewSupervisorDisplay(): string {
    if (!this.viewData?.newSupervisorId) return 'Please contact HR';

    // Try to find the supervisor in the reference data to get the full name
    const supervisor = this.supervisors.find((s: any) =>
      s.employeeNumber === this.viewData.newSupervisorId ||
      s.employeeNumber?.toString() === this.viewData.newSupervisorId?.toString() ||
      s.id === this.viewData.newSupervisorId
    );

    if (supervisor) {
      return `${supervisor.firstName} ${supervisor.lastName} (${supervisor.employeeNumber})`;
    }

    // Fallback: if supervisor name is available in viewData, use it
    if (this.viewData.newSupervisorName) {
      return `${this.viewData.newSupervisorName} (${this.viewData.newSupervisorId})`;
    }

    // Last resort: if we have an ID but no name, show "Please contact HR"
    return 'Please contact HR';
  }

  getNewPhysicalLocationDisplay(): string {
    if (!this.viewData?.newPhysicalLocationCode) return '';
    return `${this.viewData.newPhysicalLocationCode} - ${this.viewData.newPhysicalLocationName || ''}`;
  }

  getNewStatusDisplay(): string {
    if (!this.viewData?.newStatus) return '';
    return this.viewData.newStatus;
  }

  getNewPayRateDisplay(): string {
    // In view mode, use the description from the API response (resolved server-side)
    if (this.isEditMode && this.viewData?.newSalaryCode !== undefined) {
      if (this.viewData.newSalaryDescription) {
        return this.viewData.newSalaryDescription;
      }
      return this.viewData.newSalaryCode?.toString() || '';
    }
    // In create mode, look up description from formData newSalaryCode
    if (this.formData.newSalaryCode) {
      const salaryType = this.employeeSalaryTypes.find(s =>
        s.salaryCode === this.formData.newSalaryCode?.toString() ||
        parseInt(s.salaryCode) === this.formData.newSalaryCode
      );
      return salaryType?.description || this.formData.newSalaryCode.toString();
    }
    return '';
  }

  /**
   * Get driver classification display for view mode
   * Returns format: "licenseClass - description"
   */
  getDriverClassificationDisplay(): string {
    const licenseClass = this.viewData?.vehicleInfo?.licenseClass;

    if (!licenseClass) {
      return '';
    }

    // Find the matching license class in the dropdown array to get the description
    const matchingLicenseClass = this.employeeLicenseClassesForDropdown.find(
      item => item.licenseClass === licenseClass
    );

    if (matchingLicenseClass && matchingLicenseClass.description) {
      // Return format: "0-10K LBS - Class D holder and approved to operate vehicles 0 - 10k lbs"
      return `${matchingLicenseClass.licenseClass} - ${matchingLicenseClass.description}`;
    }

    // Fallback to just the license class if no description found
    return licenseClass;
  }

  /**
   * Get formatted effective date for display in view mode
   * Formats the date as MM/DD/YYYY to match backend format
   */
  getEffectiveDateDisplay(): string {
    if (!this.viewData?.effectiveDate) return '';

    try {
      const dateString = this.viewData.effectiveDate;

      // Check if the date is already in MM/DD/YYYY format (just extract the date part)
      const mmddyyyyPattern = /^(\d{1,2})\/(\d{1,2})\/(\d{4})/;
      const match = dateString.match(mmddyyyyPattern);

      if (match) {
        // Already in MM/DD/YYYY format, return the date part
        const month = match[1];
        const day = match[2];
        const year = match[3];
        return `${month}/${day}/${year}`;
      }

      // Check if it's ISO format (YYYY-MM-DD or YYYY-MM-DDTHH:mm:ss)
      const isoPattern = /^(\d{4})-(\d{2})-(\d{2})/;
      const isoMatch = dateString.match(isoPattern);

      if (isoMatch) {
        // Parse ISO format directly without timezone conversion
        const year = isoMatch[1];
        const month = parseInt(isoMatch[2], 10); // Remove leading zero
        const day = parseInt(isoMatch[3], 10);   // Remove leading zero
        return `${month}/${day}/${year}`;
      }

      // Otherwise parse and format
      const date = new Date(dateString);
      if (isNaN(date.getTime())) {
        return dateString; // Return as-is if can't parse
      }

      // Format as MM/DD/YYYY using UTC to avoid timezone shifts
      const month = date.getUTCMonth() + 1;
      const day = date.getUTCDate();
      const year = date.getUTCFullYear();
      return `${month}/${day}/${year}`;
    } catch (error) {
      return this.viewData.effectiveDate || '';
    }
  }

  /**
   * IT Related Information form getters
   */

  get isEmailRequired(): boolean {
    return this.promotionForm.get('itInfo.emailRequired')?.value === 'yes';
  }

  get isDeskPhoneSelected(): boolean {
    const phoneTypes = this.promotionForm.get('itInfo.phoneTypes') as FormArray;
    return phoneTypes?.at(0)?.value === true;
  }

  /**
   * Check if the form is valid and can be submitted
   */
  get isFormValid(): boolean {
    // Must have a selected employee
    if (!this.selectedEmployee) return false;

    // Must have promotionForm valid (all required fields filled)
    if (this.promotionForm && this.promotionForm.invalid) return false;

    // Must have a new position selected
    if (!this.formData.newPosition) return false;

    // Must have an effective date selected
    if (!this.formData.effectiveDate) return false;

    return true;
  }

  get applicationSoftwareArray() {
    return this.promotionForm.get('applicationSoftware') as any;
  }

  get folderSharepointArray() {
    return this.promotionForm.get('folderSharepoint') as any;
  }

  /**
   * Add a new row to application/software section
   */
  addApplicationRow(): void {
    this.applicationSoftwareArray.push(
      this.formBuilder.group({
        applicationSoftware: [''],
        applicationAccessNote: ['']
      })
    );
  }

  /**
   * Remove a row from application/software section
   */
  removeApplicationRow(index: number): void {
    if (this.applicationSoftwareArray.length > 1) {
      this.applicationSoftwareArray.removeAt(index);
    }
  }

  /**
   * Add a new row to folder/sharepoint/mailbox section
   */
  addFolderSharepointRow(): void {
    this.folderSharepointArray.push(
      this.formBuilder.group({
        type: [''],
        folderSharepointMailbox: ['']
      })
    );
  }

  /**
   * Remove a row from folder/sharepoint/mailbox section
   */
  removeFolderSharepointRow(index: number): void {
    if (this.folderSharepointArray.length > 1) {
      this.folderSharepointArray.removeAt(index);
    }
  }

  /**
   * Organize computer requirements into parent and child groups
   */
  private organizeComputerRequirements(): void {
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

  /**
   * Handle parent computer requirement change
   */
  onParentComputerRequirementChange(requirement: any): void {
    // Update FormControl value
    const computerEquipmentControl = this.promotionForm.get('itInfo.computerEquipment');
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

  /**
   * Check if a parent requirement is selected
   */
  isParentSelected(parentId: number): boolean {
    const computerEquipmentControl = this.promotionForm.get('itInfo.computerEquipment');
    const controlValue = computerEquipmentControl?.value;
    return controlValue == parentId; // Use == for type coercion (string/number comparison)
  }

  /**
   * Check if 'None' is selected for Computer Equipment
   */
  isComputerEquipmentNoneSelected(): boolean {
    const computerEquipmentControl = this.promotionForm.get('itInfo.computerEquipment');
    return computerEquipmentControl?.value === 'none';
  }

  /**
   * Get child requirements for a parent
   */
  getChildRequirements(parentId: number): any[] {
    return this.childComputerRequirements.get(parentId) || [];
  }

  /**
   * Get child control value for a computer requirement
   */
  getChildControlValue(parentId: number, childId: number): boolean {
    const key = `${parentId}_${childId}`;
    return this.selectedChildRequirements[key] || false;
  }

  /**
   * Handle child computer requirement change
   */
  onChildComputerRequirementChange(event: any, parentId: number, childId: number): void {
    const isChecked = event.target.checked;
    const key = `${parentId}_${childId}`;
    this.selectedChildRequirements[key] = isChecked;
  }

  /**
   * Clear all child requirements when switching parent selection
   */
  private clearAllChildRequirements(): void {
    this.selectedChildRequirements = {};
  }

  /**
   * Tablet Profile Methods
   */

  /**
   * Check if a role textbox should be shown for a given role value
   * Returns true when the role is selected in the radio button group
   * Returns false if the role label is 'None'
   */
  shouldShowRoleTextbox(roleValue: string | number): boolean {
    // Find the role by value to check its label
    const role = this.availableRoles.find(r => r.value === roleValue);

    // Don't show textbox if role label is 'None'
    if (role && role.label === 'None') {
      return false;
    }

    // Show textbox only if this role is currently selected
    const selectedRole = this.promotionForm.get('itInfo.rolesRequiredNewHires')?.value;
    const shouldShow = selectedRole === roleValue;
    return shouldShow;
  }

  /**
   * Get the form control name for a role's textbox
   * Maps profile names to form control names dynamically
   */
  getRoleTextboxControlName(roleValue: string | number): string {
    // Fixed mapping of role labels to control names (must match form initialization)
    const controlMap: {[key: string]: string} = {
      'Cargas App': 'cargasAppRole',
      'Data Collection App': 'dataCollectionRole',
      'Milestone Agg. Production App': 'milestoneRole',
      'XRS App': 'xrsRole',
      'Solar Connection': 'solarConnectionRole',
      "Todd's Redi-Mix": 'toddsRediMixRole'
    };

    // Find the role by value to get its label
    if (!roleValue) {
      return '';
    }

    const role = this.availableRoles.find(r => r.value === roleValue);
    if (!role || !role.label) {
      return '';
    }

    // Return the mapped control name, or empty string if not found
    return controlMap[role.label] || '';
  }

  /**
   * Get the itInfo FormGroup for template binding
   */
  get itInfoFormGroup(): FormGroup {
    return this.promotionForm.get('itInfo') as FormGroup;
  }

  /**
   * Generate a unique ID for a role radio button
   */
  getRoleId(roleValue: string | number): string {
    // Find the role by value to get its label
    const role = this.availableRoles.find(r => r.value === roleValue);
    if (!role || !role.label) {
      return `role-${roleValue}`;
    }

    // Use the role label to generate a clean ID
    const roleStr = role.label.toLowerCase().replace(/[\s']/g, '').replace(/_/g, '-');
    return `role-${roleStr}`;
  }

  /**
   * Update available roles based on selected company
   */
  updateAvailableRoles(company: string): void {
    this.availableRoles = [];
    if (!company) {
      return;
    }

    // Filter tablet profiles for the selected company
    const companyProfiles = this.tabletProfiles.filter(
      (profile: any) => profile.companyCode === company
    );

    if (companyProfiles && companyProfiles.length > 0) {
      // Extract unique roles from filtered profiles
      const uniqueRoles = new Map<string, string>();
      companyProfiles.forEach((profile: any) => {
        if (profile.roleValue && profile.roleLabel) {
          uniqueRoles.set(profile.roleValue, profile.roleLabel);
        }
      });

      // Convert to array format and add OTHER option
      this.availableRoles = Array.from(uniqueRoles, ([value, label]) => ({
        value,
        label
      }));

      // Add OTHER option if not already present
      if (!uniqueRoles.has('OTHER')) {
        this.availableRoles.push({ value: 'OTHER', label: 'Other (Please Specify)' });
      }
    }
  }

  /**
   * Update available roles from tablet profile data
   * Filters by isActive status, sorts alphabetically with 'None' first
   */
  updateAvailableRolesFromTabletProfiles(): void {
    if (this.tabletProfiles && this.tabletProfiles.length > 0) {
      this.availableRoles = [];

      // Filter active profiles and extract profile names
      this.tabletProfiles.forEach((profile: any) => {
        if (profile.isActive && profile.profileName) {
          this.availableRoles.push({
            value: profile.id,
            label: profile.profileName
          });
        }
      });

      // Sort: 'None' first, then alphabetically
      this.availableRoles.sort((a, b) => {
        if (a.label === 'None') return -1;
        if (b.label === 'None') return 1;
        return a.label.localeCompare(b.label);
      });

      // Default select 'None' when loading roles (only in create mode, not edit/view mode)
      if (!this.isEditMode) {
        const noneRole = this.availableRoles.find(role => role.label === 'None');
        if (noneRole) {
          this.promotionForm.get('itInfo.rolesRequiredNewHires')?.setValue(noneRole.value);
          console.log('[updateAvailableRolesFromTabletProfiles] Default selected "None" role with value:', noneRole.value);
        }
      }
    } else {
      this.availableRoles = [];
    }
  }

  /**
   * Handle email required field changes to auto-select Microsoft Office License
   */
  private handleEmailRequiredChange(value: string): void {
    const itInfo = this.promotionForm.get('itInfo');

    // If email not required, clear Microsoft License field and email address
    if (value !== 'yes') {
      itInfo?.get('microsoftLicense')?.setValue('');
      itInfo?.get('emailAddress')?.setValue('');
    } else {
      // If email is required and company is not Mathy Construction (19), auto-select F3 license
      if (this.shouldAutoSelectF3License()) {
        itInfo?.get('microsoftLicense')?.setValue('f3');
      }

      // Generate email based on currently selected new payroll dept (if available)
      // Skip in edit mode - populateITInfo handles setting the saved NewWorkEmail
      if (!this.isEditMode) {
        const deptId = this.promotionForm.get('positionInfo.newPayrollDept')?.value;
        const deptCode = deptId ? this.getPayrollDeptCode(deptId) : null;
        if (deptCode) {
          this.generateNewWorkEmail(deptCode);
        } else if (this.selectedEmployee?.workEmail && this.selectedEmployee?.employeeName) {
          // Fallback: use firstName.lastName@currentDomain if no new dept selected yet
          const currentDomain = this.selectedEmployee.workEmail.split('@')[1];
          const nameParts = this.selectedEmployee.employeeName.trim().split(/\s+/);
          const firstName = (nameParts[0] || '').toLowerCase().replace(/\s/g, '');
          const lastName = (nameParts[nameParts.length - 1] || '').toLowerCase().replace(/\s/g, '');
          if (firstName && lastName && currentDomain) {
            itInfo?.get('emailAddress')?.setValue(`${firstName}.${lastName}@${currentDomain}`);
          }
        }
      }
    }
  }

  /**
   * Generate new work email using firstName.lastName@emailDomain
   * from the selected employee's name and the new payroll department's email domain.
   */
  private generateNewWorkEmail(newDeptCode: number): void {
    const employeeName = this.selectedEmployee?.employeeName;
    if (!employeeName) {
      console.log('[generateNewWorkEmail] No employee name available');
      return;
    }

    // Get email domain from the new payroll department
    const dept = this.payrollDepts.find(d => d.deptCode === newDeptCode);
    if (!dept?.emailDomain) {
      console.log('[generateNewWorkEmail] No email domain found for dept code:', newDeptCode);
      return;
    }

    // Parse employeeName format: "FirstName MiddleName LastName" or "FirstName LastName"
    const nameParts = employeeName.trim().split(/\s+/);
    const firstName = (nameParts[0] || '').toLowerCase().replace(/\s/g, '');
    const lastName = (nameParts[nameParts.length - 1] || '').toLowerCase().replace(/\s/g, '');

    if (!firstName || !lastName) {
      console.log('[generateNewWorkEmail] Could not parse first/last name from:', employeeName);
      return;
    }

    const newEmail = `${firstName}.${lastName}@${dept.emailDomain}`;
    console.log('[generateNewWorkEmail] Generated new work email:', newEmail);

    this.promotionForm.get('itInfo.emailAddress')?.setValue(newEmail);
  }

  /**
   * Check if F3 license should be auto-selected based on company code
   * Auto-select F3 license if company is NOT Mathy Construction (19) AND email is required
   */
  private shouldAutoSelectF3License(): boolean {
    const emailRequired = this.promotionForm.get('itInfo.emailRequired')?.value;

    // Get company code from selected employee
    if (!this.selectedEmployee || !this.selectedEmployee.companyCode) {
      return false;
    }

    const companyCode = this.selectedEmployee.companyCode;

    // Auto-select F3 license if company is NOT Mathy Construction (19) AND email is required
    return !!(companyCode && companyCode !== '19' && emailRequired === 'yes');
  }

  /**
   * Handle phone type checkbox changes with mutual exclusion logic
   * Company Cell Phone and BYOD Cell Phone are mutually exclusive
   */
  private handlePhoneTypeChange(phoneTypeIndex: number, isChecked: boolean): void {
    // Prevent infinite loops during form updates
    if (this.isUpdatingForm) {
      return;
    }

    const phoneTypesArray = this.promotionForm.get('itInfo.phoneTypes') as any;

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

  /**
   * Get the selected physical location name for the delivery label
   * Returns the location name from the selected employee's physical location
   */
  get selectedPhysicalLocation(): string {
    // Use the selected employee's physical location
    if (!this.selectedEmployee || !this.selectedEmployee.physicalLocationCode) {
      return '-';
    }

    // Try to find the location name from the physical locations dropdown
    if (this.physicalLocations.length > 0) {
      const location = this.physicalLocations.find(loc =>
        loc.locationCode === this.selectedEmployee!.physicalLocationCode
      );
      return location ? location.locationName : '-';
    }

    return '-';
  }

  /**
   * Handle payroll company change (for cascading dropdown updates)
   */
  onNewPayrollCompanyChange(): void {
    // This is a no-op stub for form binding
  }

  /**
   * Handle payroll group change (for cascading dropdown updates)
   */
  onNewPayrollGroupChange(): void {
    // This is a no-op stub for form binding
  }

  // ============================================================================
  // View Mode Support Methods
  // ============================================================================

  /**
   * Initialize route parameters to detect view mode
   */
  private initializeRouteParameters(): void {
    // Check for parentId parameter from route
    const parentIdParam = this.route.snapshot.paramMap.get('parentId');
    if (parentIdParam) {
      this.parentId = parseInt(parentIdParam, 10);
      this.isEditMode = true;
      console.log('Promotion component loaded in view mode with parentId:', this.parentId);
      return;
    }

    // If no parentId found, we're in create mode
    console.log('Promotion component loaded in create mode');
    this.isEditMode = false;
  }

  /**
   * Load existing promotion request data for view mode
   */
  private async loadExistingRequest(): Promise<void> {
    if (!this.parentId) {
      console.error('Cannot load request: no parentId provided');
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    try {
      console.log('Loading promotion request for parentId:', this.parentId);

      const response = await this.hrRequestService.getPromotionRequestDetails(this.parentId)
        .pipe(takeUntil(this.destroy$))
        .toPromise();

      if (!response?.success || !response.data) {
        throw new Error(response?.message || 'Failed to load promotion request data');
      }

      this.viewData = response.data;
      console.log('Promotion request data loaded successfully:', this.viewData);
      console.log('[loadExistingRequest] Building access from API response:', this.viewData.buildingAccess);

      // Set the request detail ID for cancel operations
      this.requestDetailId = this.viewData.requestDetailId;

      // Set the request status ID for cancel button visibility
      this.requestStatusId = this.viewData.requestStatusId || null;

      // Check if the request is cancelled
      this.isCancelledRequest = this.viewData.requestStatusName?.toLowerCase().includes('cancelled') || false;

      // Check if the request is a draft
      this.isDraftRequest = this.viewData.requestStatusName?.toLowerCase().includes('draft') || false;

      // Set form editability: editable if draft, readonly if not
      this.isFormEditable = this.isDraftRequest;

      console.log('Request status:', this.viewData.requestStatusName, 'isDraftRequest:', this.isDraftRequest, 'isFormEditable:', this.isFormEditable, 'requestStatusId:', this.requestStatusId);

      // Create a selectedEmployee object from viewData to display in the selected employee section
      // Using 'as any' to bypass TypeScript strict checking since we're building an object with additional properties
      this.selectedEmployee = {
        employeeNumber: this.viewData.employeeId?.toString() || '',
        employeeName: this.viewData.employeeName,
        companyCode: this.viewData.currentPayrollCompanyCode?.toString() || '',
        companyName: this.viewData.currentCompanyName || '',
        email: this.viewData.currentWorkEmail || '',
        workEmail: this.viewData.currentWorkEmail || '',
        position: this.viewData.currentPositionName || this.viewData.employeePositionCode,
        department: this.viewData.currentPayrollDeptName || '',
        payrollCompanyCode: this.viewData.currentPayrollCompanyCode,
        payrollDeptCode: this.viewData.currentPayrollDeptCode,
        payrollGroupCode: this.viewData.currentPayrollGroupCode,
        physicalLocationCode: this.viewData.currentPhysicalLocationCode,
        isActive: true,
        hasExistingHRRequest: false,
        supervisorId: this.viewData.currentSupervisorId,
        status: this.viewData.currentStatus
      } as EmployeeDto;

      console.log('Selected employee created from viewData:', this.selectedEmployee);

      // Load all reference data for the employee's payroll company before populating form
      if (this.selectedEmployee && (this.selectedEmployee.payrollCompanyCode || this.selectedEmployee.companyCode)) {
        const employeeCompanyCode = this.selectedEmployee.payrollCompanyCode ?? parseInt(this.selectedEmployee.companyCode, 10);

        // Load main reference data (payroll groups, depts, positions, supervisors, locations, statuses, salary types)
        console.log('[loadExistingRequest] Loading reference data for company code:', employeeCompanyCode);
        await this.loadReferenceDataByCompanyCodeAsync(employeeCompanyCode);
        console.log('[loadExistingRequest] Reference data loaded:', {
          payrollGroups: this.payrollGroups.length,
          payrollDepts: this.payrollDepts.length,
          positions: this.positions.length,
          supervisors: this.supervisors.length,
          physicalLocations: this.physicalLocations.length,
          employmentStatuses: this.employmentStatuses.length,
          employeeSalaryTypes: this.employeeSalaryTypes.length
        });

        console.log('[loadExistingRequest] Loading building access for company code:', employeeCompanyCode);
        await this.loadBuildingAccessByCompanyCodeAsync(employeeCompanyCode);
        console.log('[loadExistingRequest] Building access loaded. Requirements count:', this.buildingAccessRequirements.length);

        // Load tablet profiles for the employee's company before populating form
        console.log('[loadExistingRequest] Loading tablet profiles for company code:', employeeCompanyCode);
        await this.loadTabletProfilesFromAPIAsync(employeeCompanyCode);
        console.log('[loadExistingRequest] Tablet profiles loaded. Count:', this.tabletProfiles.length);

        // Load applications for the employee's company before populating form
        console.log('[loadExistingRequest] Loading applications for company code:', employeeCompanyCode);
        await this.loadApplicationsAsync(employeeCompanyCode);
        console.log('[loadExistingRequest] Applications loaded. Count:', this.applicationsForDropdown.length);

        // Load computer requirements before populating form
        console.log('[loadExistingRequest] Loading computer requirements');
        await this.loadComputerRequirementsAsync();
        console.log('[loadExistingRequest] Computer requirements loaded. Count:', this.computerRequirements.length);
      } else {
        console.warn('[loadExistingRequest] No company code found for reference data loading');
      }

      // Populate form with existing data
      this.populateFormWithExistingData(this.viewData);

      // Show the form sections in view mode
      this.showPositionComparison = true;
      this.showConditionalSections = true;

      this.toasterService.showSuccess('Promotion request loaded successfully', 'View Mode');

    } catch (error: any) {
      console.error('Error loading promotion request:', error);
      this.errorMessage = error?.error?.message || error?.message || 'Failed to load promotion request';
      this.toasterService.showError(this.errorMessage, 'Load Error');
    } finally {
      this.isLoading = false;
    }
  }

  /**
   * Populate form with existing promotion request data
   */
  private populateFormWithExistingData(data: any): void {
    // First, populate the current position using the selectedEmployee we created
    if (this.selectedEmployee) {
      this.populateCurrentPosition(this.selectedEmployee);
    }

    // Populate formData fields (ngModel) - EffectiveDate and Notes
    console.log('[populateFormWithExistingData] Raw effectiveDate from backend:', data.effectiveDate);
    if (data.effectiveDate) {
      this.formData.effectiveDate = this.formatDateForInput(data.effectiveDate);
      this.originalEffectiveDate = this.formData.effectiveDate;
      console.log('[populateFormWithExistingData] Formatted effectiveDate:', this.formData.effectiveDate);
    }
    this.formData.notes = data.notes || '';

    // Use setTimeout to wait for reference data to load and dropdowns to render
    // The reference data loading is asynchronous, so we need to wait longer
    setTimeout(() => {
      try {
        console.log('[VIEW MODE] Populating dropdowns with data:', data);
        console.log('[VIEW MODE] Reference data available:', {
          companies: this.payrollCompanies.length,
          groups: this.payrollGroups.length,
          depts: this.payrollDepts.length,
          positions: this.positions.length,
          supervisors: this.supervisors.length,
          locations: this.physicalLocations.length,
          statuses: this.employmentStatuses.length
        });

        // Populate all the dropdown arrays with data
        this.populateNewPositionDropdowns(data);

        console.log('[VIEW MODE] Dropdown arrays populated:', {
          newCompanies: this.newPayrollCompaniesForDropdown.length,
          newGroups: this.newPayrollGroupsForDropdown.length,
          newDepts: this.newPayrollDeptsForDropdown.length,
          newPositions: this.newPositionsForDropdown.length,
          newSupervisors: this.newSupervisorsForDropdown.length,
          newLocations: this.newPhysicalLocationsForDropdown.length,
          newStatuses: this.newEmploymentStatusesForDropdown.length
        });

        // Populate position information (form values)
        this.setNewPositionFormValues(data);

        console.log('[VIEW MODE] Form values set');

        // Populate credit card information
        this.populateCreditCardInfo(data.creditCardInfo);

        // Populate vehicle information
        this.populateVehicleInfo(data.vehicleInfo);

        // Populate IT information
        this.populateITInfo(data.itInfo);

        // Populate phone information
        this.populatePhoneInfo(data.phoneInfo);

        // Populate complex structures
        this.populateApplicationSoftware(data.applications);
        this.populateFolders(data.folders);
        this.populateComputerRequirements(data.computerRequirements);

      } catch (error) {
        console.error('Error during form population:', error);
        // Continue even if some sections fail to populate
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

        // Restore useExistingKeyFob flag
        this.promotionForm.get('useExistingKeyFob')?.setValue(this.viewData?.useExistingKeyFob ?? false, { emitEvent: false });
      } catch (error) {
        console.error('Error populating tablet profiles or building access after disable:', error);
      }
    }, 500); // Increased delay to wait for async reference data loading
  }

  /**
   * Format date for HTML date input (YYYY-MM-DD)
   * Handles timezone issues by parsing date as local date without time component
   */
  private formatDateForInput(dateString: string): string {
    if (!dateString) return '';
    try {
      console.log('[formatDateForInput] Input dateString:', dateString);

      // Check if the date is in MM/DD/YYYY format
      const mmddyyyyPattern = /^(\d{1,2})\/(\d{1,2})\/(\d{4})/;
      const match = dateString.match(mmddyyyyPattern);

      if (match) {
        // Parse MM/DD/YYYY format directly to avoid timezone issues
        const month = match[1].padStart(2, '0');
        const day = match[2].padStart(2, '0');
        const year = match[3];
        const result = `${year}-${month}-${day}`;
        console.log('[formatDateForInput] Parsed MM/DD/YYYY format, result:', result);
        return result;
      }

      // Check if it's already in ISO format (YYYY-MM-DD or YYYY-MM-DDTHH:mm:ss)
      const isoPattern = /^(\d{4})-(\d{2})-(\d{2})/;
      const isoMatch = dateString.match(isoPattern);

      if (isoMatch) {
        // Already in ISO format, just return the date part (YYYY-MM-DD)
        const result = `${isoMatch[1]}-${isoMatch[2]}-${isoMatch[3]}`;
        console.log('[formatDateForInput] Already ISO format, result:', result);
        return result;
      }

      // Otherwise, try other standard formats
      // Parse the date and use UTC to avoid timezone shifts
      const date = new Date(dateString);
      if (isNaN(date.getTime())) {
        console.warn('[formatDateForInput] Invalid date:', dateString);
        return '';
      }

      // Extract year, month, day in UTC to avoid timezone shifts
      const year = date.getUTCFullYear();
      const month = String(date.getUTCMonth() + 1).padStart(2, '0');
      const day = String(date.getUTCDate()).padStart(2, '0');

      const result = `${year}-${month}-${day}`;
      console.log('[formatDateForInput] Parsed using Date object, result:', result);
      return result;
    } catch (error) {
      console.error('[formatDateForInput] Error:', error);
      return '';
    }
  }

  /**
   * Set new position form values
   */
  private setNewPositionFormValues(data: any): void {
    try {
      const positionInfoGroup = this.promotionForm.get('positionInfo');
      if (!positionInfoGroup) {
        console.warn('positionInfo form group not found');
        return;
      }

      // Set the form values WITHOUT emitting events to prevent valueChanges subscriptions
      // from triggering and reloading reference data
      const control1 = this.promotionForm.get('positionInfo.newPayrollCompany');
      if (control1) {
        control1.setValue(data.newPayrollCompanyCode ? data.newPayrollCompanyCode.toString() : '', { emitEvent: false });
      }

      const control2 = this.promotionForm.get('positionInfo.newPayrollGroup');
      if (control2) {
        control2.setValue(data.newPayrollGroupCode ? data.newPayrollGroupCode.toString() : '', { emitEvent: false });
      }

      const control3 = this.promotionForm.get('positionInfo.newPayrollDept');
      if (control3) {
        control3.setValue(data.newPayrollDeptCode ? data.newPayrollDeptCode.toString() : '', { emitEvent: false });
      }

      const control4 = this.promotionForm.get('positionInfo.newPosition');
      if (control4) {
        control4.setValue(data.newPositionCode || '', { emitEvent: false });
      }

      const control5 = this.promotionForm.get('positionInfo.newSupervisor');
      if (control5) {
        control5.setValue(data.newSupervisorId ? data.newSupervisorId.toString() : '', { emitEvent: false });
      }

      const control6 = this.promotionForm.get('positionInfo.newPhysicalLocation');
      if (control6) {
        control6.setValue(data.newPhysicalLocationCode ? data.newPhysicalLocationCode.toString() : '', { emitEvent: false });
      }

      const control7 = this.promotionForm.get('positionInfo.newStatus');
      if (control7) {
        control7.setValue(data.newStatus || '', { emitEvent: false });
      }

      console.log('[VIEW MODE] Form values set (without emitting events)');
    } catch (error) {
      console.error('Error setting position form values:', error);
    }
  }

  /**
   * Populate new position dropdowns based on the data
   */
  private populateNewPositionDropdowns(data: any): void {
    // Populate companies dropdown
    if (this.payrollCompanies.length > 0) {
      this.newPayrollCompaniesForDropdown = this.payrollCompanies.map(company => ({
        ...company,
        displayText: `${company.companyCode} - ${company.companyName}`
      }));
    }

    // Populate payroll groups dropdown
    if (this.payrollGroups.length > 0) {
      let filteredGroups = this.payrollGroups;

      // Filter by company code if available
      if (data.newPayrollCompanyCode) {
        filteredGroups = filteredGroups.filter((g: any) =>
          g.companyCode === data.newPayrollCompanyCode || g.companyCode?.toString() === data.newPayrollCompanyCode?.toString()
        );
      }

      this.newPayrollGroupsForDropdown = filteredGroups.map((group: any) => ({
        ...group,
        displayText: `${group.prGroup} - ${group.prGroupDesc}`
      }));
    }

    // Populate payroll departments dropdown
    if (this.payrollDepts.length > 0) {
      let filteredDepts = this.payrollDepts;

      console.log('[DEBUG] Total departments before filtering:', this.payrollDepts.length);
      console.log('[DEBUG] Filtering with companyCode:', data.newPayrollCompanyCode, 'and prGroup:', data.newPayrollGroupCode);
      console.log('[DEBUG] Sample dept structure:', this.payrollDepts[0]);

      // Filter by company code and payroll group if available
      if (data.newPayrollCompanyCode) {
        const beforeFilter = filteredDepts.length;
        filteredDepts = filteredDepts.filter((d: any) =>
          d.companyCode === data.newPayrollCompanyCode || d.companyCode?.toString() === data.newPayrollCompanyCode?.toString()
        );
        console.log('[DEBUG] After company filter:', filteredDepts.length, '(was', beforeFilter, ')');
      }

      if (data.newPayrollGroupCode && filteredDepts.length > 0) {
        const beforeFilter = filteredDepts.length;
        filteredDepts = filteredDepts.filter((d: any) =>
          d.prGroup === data.newPayrollGroupCode || d.prGroup?.toString() === data.newPayrollGroupCode?.toString()
        );
        console.log('[DEBUG] After prGroup filter:', filteredDepts.length, '(was', beforeFilter, ')');
      }

      // If filtering results in no departments, use all departments
      if (filteredDepts.length === 0) {
        console.warn('[DEBUG] Filtering resulted in 0 departments, using all departments');
        filteredDepts = this.payrollDepts;
      }

      this.newPayrollDeptsForDropdown = filteredDepts.map((dept: any) => ({
        ...dept,
        displayText: `${dept.department} - ${dept.departmentName}`
      }));
      console.log('[DEBUG] Final newPayrollDeptsForDropdown length:', this.newPayrollDeptsForDropdown.length);
    }

    // Populate positions dropdown
    if (this.positions.length > 0) {
      this.newPositionsForDropdown = this.positions.map((position: any) => ({
        ...position,
        displayText: position.positionDescription || position.positionCode
      }));
    }

    // Populate supervisors dropdown
    if (this.supervisors.length > 0) {
      this.newSupervisorsForDropdown = this.supervisors.map((supervisor: any) => ({
        ...supervisor,
        displayText: supervisor.supervisorName || supervisor.supervisorId?.toString()
      }));
    }

    // Populate physical locations dropdown
    if (this.physicalLocations.length > 0) {
      this.newPhysicalLocationsForDropdown = this.physicalLocations.map((location: any) => ({
        ...location,
        displayText: `${location.physicalLocationCode} - ${location.locationDescription}`
      }));
    }

    // Populate employment statuses dropdown
    if (this.employmentStatuses.length > 0) {
      this.newEmploymentStatusesForDropdown = this.employmentStatuses.map((status: any) => ({
        ...status,
        displayText: status.statusDescription || status.statusCode
      }));
    }

    // Populate employee salary types dropdown
    if (this.employeeSalaryTypes.length > 0) {
      this.newEmployeeSalaryTypesForDropdown = this.employeeSalaryTypes.map((type: any) => ({
        ...type,
        displayText: type.description || type.salaryType
      }));
    }
  }

  /**
   * Populate credit card information
   */
  private populateCreditCardInfo(creditCardInfo: any): void {
    if (!creditCardInfo) return;

    try {
      this.safeSetFormValue('kwikTripCard', creditCardInfo.kwikTripCard ? 'yes' : 'no');
      this.safeSetFormValue('companyExpenseCard', creditCardInfo.companyExpenseCard ? 'yes' : 'no');
      this.safeSetFormValue('creditExpenseType', creditCardInfo.creditExpenseType || '');
      this.safeSetFormValue('weeklyLimit', creditCardInfo.weeklyLimit || '');
      this.safeSetFormValue('fuelCardlockAccess', creditCardInfo.fuelCardlockAccess ? 'yes' : 'no');
      this.safeSetFormValue('cardlockShipAddress', creditCardInfo.fuelCardlockAddress || '');
    } catch (error) {
      console.error('Error populating credit card info:', error);
    }
  }

  /**
   * Populate vehicle information
   */
  private populateVehicleInfo(vehicleInfo: any): void {
    if (!vehicleInfo) {
      console.log('[populateVehicleInfo] No vehicleInfo provided');
      return;
    }

    console.log('[populateVehicleInfo] vehicleInfo data:', vehicleInfo);

    try {
      const vehicleGroup = this.promotionForm.get('vehicleInfo');
      if (!vehicleGroup) {
        console.warn('vehicleInfo form group not found');
        return;
      }

      this.safeSetFormValue('vehicleInfo.approvedVehicle', vehicleInfo.isApprovedToOperate ? 'yes' : 'no');

      // Set showVehicleFields based on isApprovedToOperate
      this.showVehicleFields = vehicleInfo.isApprovedToOperate === true;
      console.log('[populateVehicleInfo] showVehicleFields set to:', this.showVehicleFields);

      // Driver classification - find matching item by licenseClass string
      console.log('[populateVehicleInfo] licenseClass from backend:', vehicleInfo.licenseClass);
      console.log('[populateVehicleInfo] Available license classes:', this.employeeLicenseClassesForDropdown);

      if (vehicleInfo.licenseClass) {
        const matchingLicenseClass = this.employeeLicenseClassesForDropdown.find(
          item => item.licenseClass === vehicleInfo.licenseClass
        );
        console.log('[populateVehicleInfo] Matched license class:', matchingLicenseClass);

        if (matchingLicenseClass) {
          this.safeSetFormValue('vehicleInfo.driverClassification', matchingLicenseClass.id.toString());
        } else {
          console.warn('[populateVehicleInfo] No matching license class found for:', vehicleInfo.licenseClass);
          this.safeSetFormValue('vehicleInfo.driverClassification', '');
        }
      } else {
        console.log('[populateVehicleInfo] No licenseClass in vehicleInfo');
        this.safeSetFormValue('vehicleInfo.driverClassification', '');
      }

      console.log('[populateVehicleInfo] drugAndAlcoholProfile:', vehicleInfo.drugAndAlcoholProfile);
      this.safeSetFormValue('vehicleInfo.drugAlcoholProfile', vehicleInfo.drugAndAlcoholProfile || '');
      this.safeSetFormValue('vehicleInfo.companyCarNeeded', vehicleInfo.needCompanyCar ? 'yes' : 'no');
      this.safeSetFormValue('vehicleInfo.applicationPart2', vehicleInfo.isApplicationPart2Complete ? 'yes' : 'no');
    } catch (error) {
      console.error('Error populating vehicle info:', error);
    }
  }

  /**
   * Populate IT information
   */
  private populateITInfo(itInfo: any): void {
    if (!itInfo) return;

    try {
      const itInfoGroup = this.promotionForm.get('itInfo');
      if (!itInfoGroup) {
        console.warn('itInfo form group not found');
        return;
      }

      this.safeSetFormValue('itInfo.emailRequired', itInfo.emailRequired ? 'yes' : 'no');
      this.safeSetFormValue('itInfo.deliveryNote', itInfo.alternateDeliveryLocation || '');

      // Populate email address from viewData's newWorkEmail (saved email) or current work email
      if (itInfo.emailRequired && this.viewData) {
        this.safeSetFormValue('itInfo.emailAddress', this.viewData.newWorkEmail || this.viewData.currentWorkEmail || '');
      }

      // Set Microsoft License based on boolean flags
      if (itInfo.msofficeLicenseE5 && itInfo.msofficeLicenseF3) {
        this.safeSetFormValue('itInfo.microsoftLicense', 'both');
      } else if (itInfo.msofficeLicenseE5) {
        this.safeSetFormValue('itInfo.microsoftLicense', 'e5');
      } else if (itInfo.msofficeLicenseF3) {
        this.safeSetFormValue('itInfo.microsoftLicense', 'f3');
      }
    } catch (error) {
      console.error('Error populating IT info:', error);
    }
  }

  /**
   * Populate phone information
   */
  private populatePhoneInfo(phoneInfo: any): void {
    if (!phoneInfo) return;

    try {
      const phoneTypesArray = this.promotionForm.get('itInfo.phoneTypes') as FormArray;
      if (phoneTypesArray) {
        try {
          phoneTypesArray.setValue([
            phoneInfo.deskPhone || false,
            phoneInfo.companyCellphone || false,
            phoneInfo.byodCellphone || false
          ]);
        } catch (error) {
          console.warn('Error setting phone types array:', error);
        }
      }

      // Handle other phone fields
      this.safeSetFormValue('itInfo.workPhoneNumber', phoneInfo.workPhoneNumber || '');
      this.safeSetFormValue('itInfo.workExtension', phoneInfo.workExtension || '');
      this.safeSetFormValue('itInfo.reusingPhone', phoneInfo.reusingExistingPhone ? 'yes' : 'no');
    } catch (error) {
      console.error('Error populating phone info:', error);
    }
  }

  /**
   * Populate application software
   * Pattern from New Hire Request
   */
  private populateApplicationSoftware(applications: any[]): void {
    if (!applications || applications.length === 0) {
      return;
    }

    const applicationArray = this.promotionForm.get('applicationSoftware') as FormArray;
    applicationArray.clear();

    applications.forEach(app => {
      const appGroup = this.formBuilder.group({
        applicationSoftware: [''],
        applicationAccessNote: ['']
      });
      appGroup.patchValue({
        applicationSoftware: app.applicationId ? app.applicationId.toString() : '',
        applicationAccessNote: app.accessNotes || ''
      });
      applicationArray.push(appGroup);
    });
  }

  /**
   * Populate folders
   */
  private populateFolders(folders: any[]): void {
    if (!folders || folders.length === 0) {
      return;
    }

    try {
      const foldersArray = this.promotionForm.get('folderSharepoint') as FormArray;
      if (foldersArray) {
        // Clear existing items
        while (foldersArray.length > 0) {
          foldersArray.removeAt(0);
        }

        // Add folder items from viewData
        folders.forEach(folder => {
          const folderGroup = this.formBuilder.group({
            type: [folder.folderType || ''],
            folderSharepointMailbox: [folder.folderName || '']
          });
          foldersArray.push(folderGroup);
        });
      }
    } catch (error) {
      console.error('Error populating folders:', error);
    }
  }

  /**
   * Populate tablet profiles
   * Pattern from New Hire Request
   */
  private populateTabletProfiles(tabletProfiles: any[]): void {
    if (!tabletProfiles || tabletProfiles.length === 0) {
      return;
    }

    try {
      // Set the first tablet profile as the selected role
      const firstProfile = tabletProfiles[0];

      if (firstProfile) {
        const profileName = firstProfile.tabletProfileName || '';

        // Check if the profile name exists in available roles
        const matchingRole = this.availableRoles.find(role => role.value === profileName);

        if (!matchingRole) {
          // Try using tabletProfileId instead (radio buttons use numeric IDs)
          const profileId = firstProfile.tabletProfileId;
          const matchingRoleById = this.availableRoles.find(role => role.value === profileId || role.value === profileId.toString());

          if (matchingRoleById) {
            this.safeSetFormValue('itInfo.rolesRequiredNewHires', matchingRoleById.value);

            // Set role-specific text if available - use matchingRoleById.value for control name lookup
            const roleControlName = this.getRoleTextboxControlName(matchingRoleById.value);

            if (roleControlName && firstProfile.rolesRequiredForNewHire) {
              this.safeSetFormValue(`itInfo.${roleControlName}`, firstProfile.rolesRequiredForNewHire);
            }
            return;
          }
        }

        this.safeSetFormValue('itInfo.rolesRequiredNewHires', profileName);

        // Set role-specific text if available
        const roleControlName = this.getRoleTextboxControlName(firstProfile.tabletProfileName);

        if (roleControlName && firstProfile.rolesRequiredForNewHire) {
          this.safeSetFormValue(`itInfo.${roleControlName}`, firstProfile.rolesRequiredForNewHire);
        }
      }
    } catch (error) {
      console.error('[populateTabletProfiles] Error:', error);
    }
  }

  /**
   * Populate computer requirements
   * Pattern from New Hire Request
   */
  private populateComputerRequirements(computerRequirements: any[]): void {
    if (!computerRequirements || computerRequirements.length === 0) {
      return;
    }

    // Find parent computer requirement
    const parentRequirement = computerRequirements.find(req => !req.isChild);
    if (parentRequirement) {
      this.promotionForm.patchValue({
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

  /**
   * Populate building access requirements
   * Pattern from New Hire Request
   */
  private populateBuildingAccess(buildingAccess: any[]): void {
    if (!buildingAccess || buildingAccess.length === 0 || !this.buildingAccessRequirements) {
      return;
    }

    const buildingAccessArray = this.promotionForm.get('buildingAccess') as FormArray;
    if (!buildingAccessArray) {
      return;
    }

    // Create a boolean array based on the SORTED availableBuildingAccess order
    // This matches the pattern from New Hire Request
    const accessValues = this.availableBuildingAccess.map(sortedDescription => {
      // Find the requirement that matches this sorted description
      const matchingRequirement = this.buildingAccessRequirements.find(req =>
        req.description === sortedDescription
      );

      if (!matchingRequirement) {
        return false;
      }

      // Check if this requirement was saved in the data
      const isSelected = buildingAccess.some(access =>
        access.accessId === matchingRequirement.id ||
        access.accessDescription === matchingRequirement.description
      );

      return isSelected;
    });

    buildingAccessArray.patchValue(accessValues);
  }

  /**
   * Helper method to safely set form values
   */
  private safeSetFormValue(controlPath: string, value: any): void {
    try {
      const control = this.promotionForm.get(controlPath);
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
      console.warn(`Error setting value for ${controlPath}:`, error);
    }
  }

  /**
   * Disable form in edit/view mode (non-draft)
   */
  private disableFormInEditMode(): void {
    if (this.isFormEditable) {
      return; // Don't disable if form should be editable
    }

    // Disable all form controls recursively
    this.disableFormGroup(this.promotionForm);
  }

  private disableFormGroup(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      if (control instanceof FormGroup) {
        this.disableFormGroup(control);
      } else if (control instanceof FormArray) {
        this.disableFormArray(control);
      } else {
        control?.disable();
      }
    });
  }

  private disableFormArray(formArray: FormArray): void {
    formArray.controls.forEach(control => {
      if (control instanceof FormGroup) {
        this.disableFormGroup(control);
      } else {
        control.disable();
      }
    });
  }

  /**
   * Enable form controls (for draft mode)
   */
  private enableFormControls(): void {
    this.enableFormGroup(this.promotionForm);
  }

  private enableFormGroup(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      if (control instanceof FormGroup) {
        this.enableFormGroup(control);
      } else if (control instanceof FormArray) {
        this.enableFormArray(control);
      } else {
        control?.enable();
      }
    });
  }

  private enableFormArray(formArray: FormArray): void {
    formArray.controls.forEach(control => {
      if (control instanceof FormGroup) {
        this.enableFormGroup(control);
      } else {
        control.enable();
      }
    });
  }
}
