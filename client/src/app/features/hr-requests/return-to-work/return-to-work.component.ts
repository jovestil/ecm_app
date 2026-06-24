import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { AppHeaderComponent } from '../../../shared/app-header/app-header.component';
import { BackToHomepageButtonComponent } from '../../../shared/back-to-homepage-button/back-to-homepage-button.component';
import { CancelRequestButtonComponent } from '../../../shared/cancel-request-button/cancel-request-button.component';
import { EmployeeGridComponent } from '../../../shared/employee-grid/employee-grid.component';
import { EmployeeSearchComponent, EmployeeSearchConfig, SearchResult } from '../../../shared/employee-search/employee-search.component';
import { AuthService } from '../../../core/services/auth.service';
import { ReturnToWorkService } from '../../../core/services/return-to-work.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { HRRequestService } from '../../../core/services/hr-request.service';
import { ReferenceDataService } from '../../../core/services/reference-data.service';
import { UpdateHRRequestDetailDto } from '../../../models/api-hr-request.model';
import { 
  ReturnToWorkState, 
  EmployeeSearchParams, 
  ReturnToWorkFormData
} from '../../../models/return-to-work.model';
import { 
  Employee, 
  SortConfig 
} from '../../../shared/employee-grid/employee-grid.interface';

@Component({
  selector: 'app-return-to-work',
  standalone: true,
  imports: [CommonModule, FormsModule, AppHeaderComponent, BackToHomepageButtonComponent, CancelRequestButtonComponent, EmployeeGridComponent, EmployeeSearchComponent],
  templateUrl: './return-to-work.component.html',
  styleUrls: ['../../../shared/styles/common.css', './return-to-work.component.css']
})
export class ReturnToWorkComponent implements OnInit, OnDestroy {
  // Component state using the new model
  state: ReturnToWorkState = {
    parentId: null,
    isEditMode: false,
    searchType: 'division',
    searchQuery: '',
    showSearchResults: false,
    showEmployeeGrid: true,
    currentPage: 1,
    pageSize: 25,
    totalPages: 1,
    totalCount: 0,
    currentSort: { field: null, direction: 'asc' },
    selectedEmployees: [],
    searchResults: [],
    laidOffEmployees: [],
    filteredEmployees: [],
    effectiveDate: '',
    notes: '',
    isLoading: false
  };
  
  // Request status tracking
  requestStatus: string = '';
  isCancelledRequest: boolean = false;
  requestStatusId: number | null = null;


  // Debounce search functionality
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  // Search configuration for the common search component
  searchConfig: EmployeeSearchConfig = {
    // searchModes: ['employee', 'division'],
    // defaultSearchMode: 'employee',
    // employeePlaceholder: 'Search employees... (select multiple with checkboxes)',
    searchModes: ['division'],
    defaultSearchMode: 'division',
    divisionPlaceholder: 'Search employees... (select multiple with checkboxes)',
    showSearchTip: true
  };

  // Search service for autonomous search component
  returnToWorkSearchService = {
    searchEmployees: async (searchTerm: string): Promise<Employee[]> => {
      try {
        const searchParams: EmployeeSearchParams = {
          page: 1,
          pageSize: 25,
          searchQuery: searchTerm,
          searchType: 'employee',
          orderBy: undefined,
          orderByDesc: false,
          isEditMode: false
        };
        
        const result = await this.returnToWorkService.searchLaidOffEmployees(searchParams);
        return result.employees || [];
      } catch (error) {
        console.error('Error searching employees:', error);
        return [];
      }
    }
  };

  // Convenience getters for template binding
  get selectedEmployees(): Employee[] { return this.state.selectedEmployees; }
  get searchQuery(): string { return this.state.searchQuery; }
  get searchType(): 'employee' | 'division' { return this.state.searchType; }
  get searchResults(): Employee[] { return this.state.searchResults; }
  get showSearchResults(): boolean { return this.state.showSearchResults; }
  get showEmployeeGrid(): boolean { return this.state.showEmployeeGrid; }
  get currentPage(): number { return this.state.currentPage; }
  get pageSize(): number { return this.state.pageSize; }
  get totalPages(): number { return this.state.totalPages; }
  get totalCount(): number { return this.state.totalCount; }
  get currentSort(): SortConfig { return this.state.currentSort; }
  get effectiveDate(): string { return this.state.effectiveDate; }
  originalEffectiveDate: string = '';
  get notes(): string { return this.state.notes; }
  get isLoading(): boolean { return this.state.isLoading; }
  get parentId(): number | null { return this.state.parentId; }
  get isEditMode(): boolean { return this.state.isEditMode; }
  get filteredEmployees(): Employee[] { return this.state.filteredEmployees; }
  get laidOffEmployees(): Employee[] { return this.state.laidOffEmployees; }

  // Setters for two-way binding
  set searchQuery(value: string) { this.state.searchQuery = value; }
  set effectiveDate(value: string) { this.state.effectiveDate = value; }
  set notes(value: string) { this.state.notes = value; }

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private location: Location,
    private authService: AuthService,
    private returnToWorkService: ReturnToWorkService,
    private toasterService: ToasterService,
    private hrRequestService: HRRequestService,
    private referenceDataService: ReferenceDataService
  ) {}

  async ngOnInit(): Promise<void> {
    // Setup debounced search
    this.setupDebouncedSearch();
    
    // Initialize route parameters
    this.initializeRouteParameters();
    
    await this.checkUserAuthorization();
    
    if (this.state.isEditMode && this.state.parentId) {
      await this.loadExistingRequest();
    } else {
      this.initializeCreateMode();
      await this.loadEmployees();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupDebouncedSearch(): void {
    this.searchSubject.pipe(
      debounceTime(300), // Wait 300ms after user stops typing
      distinctUntilChanged(), // Only emit if search term actually changed
      takeUntil(this.destroy$) // Cleanup on component destroy
    ).subscribe(searchTerm => {
      this.performSearch(searchTerm);
    });
  }

  /**
   * Initialize route parameters and determine edit mode
   */
  private initializeRouteParameters(): void {
    // Check for parentId parameter from route (synchronously)
    const parentIdParam = this.route.snapshot.paramMap.get('parentId');
    if (parentIdParam) {
      this.state.parentId = parseInt(parentIdParam, 10);
      this.state.isEditMode = true;
      console.log('Return-to-work component loaded in edit mode with parentId:', this.state.parentId);
      return;
    }

    // Also check query parameters as an alternative (synchronously)
    const parentIdQuery = this.route.snapshot.queryParamMap.get('parentId');
    if (parentIdQuery) {
      this.state.parentId = parseInt(parentIdQuery, 10);
      this.state.isEditMode = true;
      console.log('Return-to-work component loaded with parentId from query params:', this.state.parentId);
      return;
    }

    // If no parentId found, we're in create mode
    console.log('Return-to-work component loaded in create mode');
    this.state.isEditMode = false;
    this.state.parentId = null;
  }

  private async checkUserAuthorization(): Promise<void> {
    const isAuthorized = await this.authService.checkUserAuthorization();
    if (!isAuthorized) {
      console.log('User not authorized for return-to-work request');
      this.router.navigate(['/unauthorized']);
    }
  }

  private initializeCreateMode(): void {
    // Ensure proper initial state for create mode
    // Individual search disabled — default to division mode
    this.state.searchType = 'division';
    this.state.showSearchResults = false;
    this.state.showEmployeeGrid = true;
    this.state.searchQuery = '';
    this.state.searchResults = [];
    console.log('Initialized create mode with Division search tab active');
  }

  private async loadExistingRequest(): Promise<void> {
    if (!this.state.parentId) return;

    this.state.isLoading = true;
    try {
      const requestData = await this.returnToWorkService.loadExistingRequest(this.state.parentId);
      
      // Populate form fields from loaded data
      this.state.effectiveDate = requestData.effectiveDate;
      this.originalEffectiveDate = requestData.effectiveDate;
      this.state.notes = requestData.notes;
      this.state.selectedEmployees = requestData.selectedEmployees;
      
      // Check if the request is cancelled
      this.isCancelledRequest = requestData.isCancelled || false;
      this.requestStatus = requestData.status || 'Unknown';
      
      // Capture the request status ID for cancel button visibility
      this.requestStatusId = requestData.requestStatusId || null;
      
    } catch (error) {
      console.error('Error loading existing return-to-work request:', error);
      // Fall back to normal create mode
      this.state.isEditMode = false;
      this.state.parentId = null;
      await this.loadEmployees();
    } finally {
      this.state.isLoading = false;
    }
  }

  private getReturnToWorkRequestTypeId(): number | null {
    return this.returnToWorkService.getReturnToWorkRequestTypeId();
  }

  async loadEmployeesForSearch(): Promise<void> {
    // Don't load employees when in edit mode
    if (this.state.isEditMode) {
      return;
    }

    this.state.isLoading = true;
    try {
      const searchParams: EmployeeSearchParams = {
        page: this.state.currentPage,
        pageSize: this.state.pageSize,
        searchQuery: this.state.searchQuery,
        searchType: this.state.searchType,
        orderBy: this.state.currentSort.field ?? undefined,
        orderByDesc: this.state.currentSort.direction === 'desc',
        isEditMode: this.state.isEditMode
      };
      
      const result = await this.returnToWorkService.searchLaidOffEmployees(searchParams);
      
      this.state.searchResults = result.employees;
      this.state.totalCount = result.totalCount;
      this.state.totalPages = result.totalPages;
      
    } catch (error) {
      console.error('Error searching employees:', error);
      this.state.searchResults = [];
    } finally {
      this.state.isLoading = false;
    }
  }

  async loadEmployeesForSearchWithTerm(searchTerm: string): Promise<void> {
    // Don't load employees when in edit mode
    if (this.state.isEditMode) {
      return;
    }

    this.state.isLoading = true;
    try {
      const searchParams: EmployeeSearchParams = {
        page: this.state.currentPage,
        pageSize: this.state.pageSize,
        searchQuery: searchTerm,
        searchType: this.state.searchType,
        orderBy: this.state.currentSort.field ?? undefined,
        orderByDesc: this.state.currentSort.direction === 'desc',
        isEditMode: this.state.isEditMode
      };
      
      const result = await this.returnToWorkService.searchLaidOffEmployees(searchParams);
      
      this.state.searchResults = result.employees;
      this.state.totalCount = result.totalCount;
      this.state.totalPages = result.totalPages;
      
    } catch (error) {
      console.error('Error searching employees:', error);
      this.state.searchResults = [];
    } finally {
      this.state.isLoading = false;
    }
  }

  async loadEmployees(divisionSearchTerm?: string): Promise<void> {
    // Don't load employees when in edit mode
    if (this.state.isEditMode) {
      return;
    }

    this.state.isLoading = true;
    try {
      const searchParams: EmployeeSearchParams = {
        page: this.state.currentPage,
        pageSize: this.state.pageSize,
        searchQuery: divisionSearchTerm || this.state.searchQuery,
        searchType: this.state.searchType,
        orderBy: this.state.currentSort.field ?? undefined,
        orderByDesc: this.state.currentSort.direction === 'desc',
        isEditMode: this.state.isEditMode
      };
      
      const result = await this.returnToWorkService.searchLaidOffEmployees(searchParams);
      
      this.state.laidOffEmployees = result.employees;
      this.state.totalCount = result.totalCount;
      this.state.totalPages = result.totalPages;
      
      this.displayEmployeeGrid();
    } catch (error) {
      console.error('Error loading employees:', error);
      this.state.laidOffEmployees = [];
    } finally {
      this.state.isLoading = false;
    }
  }

  async loadEmployeesWithSearchTerm(searchTerm: string): Promise<void> {
    await this.loadEmployees(searchTerm);
  }


  toggleSearchType(type: 'employee' | 'division'): void {
    this.state.searchType = type;
    this.state.searchQuery = '';
    
    // Clear any pending debounced search when switching types
    this.searchSubject.next('');
    
    if (type === 'employee') {
      this.state.showSearchResults = true;
      this.state.showEmployeeGrid = false;
      this.state.searchResults = [];
    } else {
      this.state.showSearchResults = false;
      this.state.showEmployeeGrid = true;
      // Reset to first page and reload employees for division mode
      this.state.currentPage = 1;
      this.loadEmployees();
    }
  }

  searchEmployees(): void {
    // Emit search term to the debounced subject
    this.searchSubject.next(this.state.searchQuery);
  }

  searchEmployeesWithTerm(searchTerm: string): void {
    // Emit search term to the debounced subject
    this.searchSubject.next(searchTerm);
  }

  private performSearch(searchTerm: string): void {
    // Don't update state.searchQuery to prevent circular data flow
    // The search component now manages its own local state
    
    if (this.state.searchType === 'division') {
      // For division mode, use server-side search
      this.state.currentPage = 1; // Reset to first page when searching
      this.loadEmployees();
      return;
    }

    // For employee mode, also use server-side search
    if (searchTerm.length < 2) {
      this.state.searchResults = [];
      return;
    }

    // Use server-side search for employee mode as well
    this.state.currentPage = 1; // Reset to first page when searching
    this.loadEmployeesForSearchWithTerm(searchTerm);
  }

  displayEmployeeGrid(): void {
    // Use the loaded employees directly
    this.state.filteredEmployees = this.state.laidOffEmployees;
    
    // Apply client-side sorting only for employee search mode
    // For division mode, sorting is handled by the API
    if (this.state.searchType === 'employee' && this.state.currentSort.field) {
      this.state.filteredEmployees.sort((a, b) => {
        let aVal: any = a[this.state.currentSort.field as keyof Employee];
        let bVal: any = b[this.state.currentSort.field as keyof Employee];

        if (this.state.currentSort.field === 'company') {
          aVal = a.company;
          bVal = b.company;
        } else if (this.state.currentSort.field === 'division') {
          aVal = a.division;
          bVal = b.division;
        }

        if (typeof aVal === 'string') {
          aVal = aVal.toLowerCase();
          bVal = bVal.toLowerCase();
        }

        if (this.state.currentSort.direction === 'asc') {
          return aVal > bVal ? 1 : -1;
        } else {
          return aVal < bVal ? 1 : -1;
        }
      });
    }

    // Ensure current page is valid
    if (this.state.currentPage > this.state.totalPages && this.state.totalPages > 0) {
      this.state.currentPage = 1;
    }
  }

  get paginatedEmployees(): Employee[] {
    // For employee search mode, return search results
    if (this.state.searchType === 'employee' && this.state.searchQuery.length >= 2) {
      return this.state.searchResults;
    }
    // For division mode, return filtered employees
    return this.state.filteredEmployees;
  }

  get gridInfo(): string {
    const startIndex = (this.state.currentPage - 1) * this.state.pageSize + 1;
    const endIndex = Math.min(this.state.currentPage * this.state.pageSize, this.state.totalCount);
    const total = this.state.totalCount;

    if (total === 0) {
      return 'No laid-off employees found';
    }
    return `Showing ${startIndex}-${endIndex} of ${total} laid-off employees`;
  }

  get gridInfoBottom(): string {
    const total = this.state.totalCount;
    return `Total: ${total} laid-off employees`;
  }

  get pageInfo(): string {
    return `Page ${this.state.currentPage} of ${this.state.totalPages}`;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.state.totalPages) {
      this.state.currentPage = page;
      
      // Use appropriate loading method based on search type and query
      if (this.state.searchType === 'employee' && this.state.searchQuery.length >= 2) {
        this.loadEmployeesForSearch();
      } else {
        this.loadEmployees();
      }
    }
  }

  changePageSize(newSize: number): void {
    this.state.pageSize = newSize;
    this.state.currentPage = 1;
    
    // Use appropriate loading method based on search type and query
    if (this.state.searchType === 'employee' && this.state.searchQuery.length >= 2) {
      this.loadEmployeesForSearch();
    } else {
      this.loadEmployees();
    }
  }

  async sortGrid(field: string): Promise<void> {
    if (this.state.currentSort.field === field) {
      this.state.currentSort.direction = this.state.currentSort.direction === 'asc' ? 'desc' : 'asc';
    } else {
      this.state.currentSort.field = field;
      this.state.currentSort.direction = 'asc';
    }

    // If we're in division mode (company/division tab), call API with sorting
    if (this.state.searchType === 'division') {
      await this.loadEmployees();
    } else {
      // For employee search mode, use client-side sorting
      this.displayEmployeeGrid();
    }
  }

  getSortIndicator(field: string): string {
    if (this.state.currentSort.field === field) {
      return this.state.currentSort.direction === 'asc' ? '↑' : '↓';
    }
    return '↕';
  }

  isSortActive(field: string): boolean {
    return this.state.currentSort.field === field;
  }

  toggleEmployee(employeeNumber: number): void {
    // Find employee in the appropriate array based on current mode
    let employee: Employee | undefined;
    if (this.state.searchType === 'employee' && this.state.searchQuery.length >= 2) {
      employee = this.state.searchResults.find(emp => emp.employeeNumber === employeeNumber);
    } else {
      employee = this.state.laidOffEmployees.find(emp => emp.employeeNumber === employeeNumber);
    }
    
    if (!employee) return;

    // Don't allow toggling disabled employees
    if (this.isEmployeeDisabled(employee)) return;

    const existingIndex = this.state.selectedEmployees.findIndex(sel => sel.employeeNumber === employeeNumber);

    if (existingIndex > -1) {
      this.state.selectedEmployees.splice(existingIndex, 1);
    } else {
      this.state.selectedEmployees.push(employee);
    }

    // No need to reload data, just update UI state
  }

  isEmployeeSelected(employeeNumber: number): boolean {
    return this.state.selectedEmployees.some(emp => emp.employeeNumber === employeeNumber);
  }

  isEmployeeDisabled(employee: Employee): boolean {
    return employee.hasExistingHRRequest === true;
  }

  removeEmployee(employeeNumber: number): void {
    const index = this.state.selectedEmployees.findIndex(emp => emp.employeeNumber === employeeNumber);
    if (index > -1) {
      this.state.selectedEmployees.splice(index, 1);
    }
    
    // No need to reload data, just update UI state
  }

  isFormValid(): boolean {
    return this.state.selectedEmployees.length > 0 && this.state.effectiveDate !== '';
  }

  async submitReturnToWork(): Promise<void> {
    if (!this.isFormValid()) {
      this.toasterService.showWarning('Please select at least one employee and provide the first day to return to work', 'Form Validation');
      return;
    }

    this.state.isLoading = true;

    try {
      // Get RequestTypeId with fallback to API call
      const requestTypeId = await this.returnToWorkService.getReturnToWorkRequestTypeIdWithFallback();

      // Prepare form data for submission
      const formData: ReturnToWorkFormData = {
        selectedEmployees: this.state.selectedEmployees,
        effectiveDate: this.state.effectiveDate,
        notes: this.state.notes,
        requestTypeId: requestTypeId
      };

      // Submit through service
      const submissionData = await this.returnToWorkService.submitReturnToWorkRequest(formData);

      console.log('Return to Work Request submitted successfully:', submissionData);
      this.toasterService.showSuccess(
        `Return to work request submitted successfully for ${submissionData.employees.length} employee(s)`,
        'Request Submitted'
      );
      
      // Add delay to allow toaster to display before navigation
      setTimeout(() => {
        this.goBack();
      }, 500); // 0.5 second delay
      
    } catch (error: any) {
      console.error('Error submitting return to work request:', error);
      
      let errorMessage = 'Error submitting return to work request. Please try again.';
      
      // Extract validation errors if available
      if (error.error && error.error.errors) {
        console.error('Validation errors:', error.error.errors);
        
        // Build detailed error message from validation errors
        const validationErrors = Object.keys(error.error.errors).map(key => 
          `${key}: ${error.error.errors[key].join(', ')}`
        ).join('\n');
        
        errorMessage = `Validation errors:\n${validationErrors}`;
      } else if (error.error) {
        if (typeof error.error === 'string') {
          errorMessage = error.error;
        } else if (error.error.Message) {
          errorMessage = error.error.Message;
        } else if (error.error.message) {
          errorMessage = error.error.message;
        } else if (error.error.title) {
          errorMessage = error.error.title;
        } else {
          errorMessage = JSON.stringify(error.error);
        }
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      this.toasterService.showError(errorMessage, 'Submission Error');
    } finally {
      this.state.isLoading = false;
    }
  }

  goBack(): void {
    this.location.back();
  }

  // Event handlers for the employee grid component
  onEmployeeToggle(employeeNumber: number): void {
    this.toggleEmployee(employeeNumber);
  }

  onPageChange(page: number): void {
    this.goToPage(page);
  }

  onPageSizeChange(newSize: number): void {
    this.changePageSize(newSize);
  }

  onSortChange(sortConfig: SortConfig): void {
    // Update the sort config directly from the grid component
    // Don't call sortGrid() because it would toggle the direction again
    this.state.currentSort = sortConfig;
    
    // Apply the sorting based on current search mode
    if (this.state.searchType === 'division') {
      // For division mode, reload data from API with new sort parameters
      this.loadEmployees();
    } else {
      // For employee search mode, use client-side sorting
      this.displayEmployeeGrid();
    }
  }

  onSearchChange(query: string): void {
    this.state.searchQuery = query;
    this.searchEmployees();
  }

  onSearchModeChange(mode: 'employee' | 'division'): void {
    this.toggleSearchType(mode);
  }

  // New event handlers for autonomous search component
  onSearchResultsChanged(results: Employee[]): void {
    this.state.searchResults = results;
  }

  onSearchModeChanged(mode: 'employee' | 'division'): void {
    this.state.searchType = mode;
  }

  onShowGridChanged(showGrid: boolean): void {
    this.state.showEmployeeGrid = showGrid;
    this.state.showSearchResults = !showGrid;
    
    if (showGrid) {
      // Load employees for grid view (division mode)
      this.loadEmployees();
    }
  }

  onDivisionSearchChanged(searchTerm: string): void {
    console.log('Division search changed to:', searchTerm);
    // Update the loadEmployees to filter by division/company
    this.loadEmployeesWithDivisionFilter(searchTerm);
  }

  private async loadEmployeesWithDivisionFilter(searchTerm: string): Promise<void> {
    // Reset to first page when searching
    this.state.currentPage = 1;
    await this.loadEmployeesWithSearchTerm(searchTerm);
  }

  onRequestCancelled(): void {
    // Handle any post-cancellation logic if needed
    console.log('Request has been cancelled');
  }

  get canUpdateDate(): boolean {
    if (!this.isEditMode) return false;
    const status = this.requestStatus?.toLowerCase() || '';
    if (!status.includes('pending')) return false;
    // Disable editing if the original effective date is today or in the past
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

  updateEffectiveDate(): void {
    if (!this.state.parentId || this.isUpdatingDate) return;

    if (!this.effectiveDate) {
      this.toasterService.showError('Please select a date');
      return;
    }

    this.isUpdatingDate = true;
    this.hrRequestService.updateEffectiveDate(this.state.parentId, this.effectiveDate).subscribe({
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

}