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
import { ToasterService } from '../../../core/services/toaster.service';
import { ReferenceDataService, RequestTypeDto } from '../../../core/services/reference-data.service';
import { HRRequestService, CreateMultiEmployeeHRRequestDto } from '../../../core/services/hr-request.service';
import { EmployeeService, EmployeeDto, PagedResponse } from '../../../core/services/employee.service';
import { ApiHRRequestDetailDto, UpdateHRRequestDetailDto } from '../../../models/api-hr-request.model';
import { 
  Employee, 
  SortConfig 
} from '../../../shared/employee-grid/employee-grid.interface';


@Component({
  selector: 'app-layoff-request',
  standalone: true,
  imports: [CommonModule, FormsModule, AppHeaderComponent, BackToHomepageButtonComponent, CancelRequestButtonComponent, EmployeeGridComponent, EmployeeSearchComponent],
  templateUrl: './layoff-request.component.html',
  styleUrls: ['../../../shared/styles/common.css', './layoff-request.component.css']
})
export class LayoffRequestComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  // Employee arrays using shared interface directly (like return-to-work)
  selectedEmployees: Employee[] = [];
  searchResults: Employee[] = [];
  showSearchResults: boolean = false;
  showEmployeeGrid: boolean = true;
  
  // Main employee arrays
  allEmployees: Employee[] = [];
  
  // Parent ID handling
  parentId: number | null = null;
  isEditMode: boolean = false;
  
  // Request status tracking
  requestStatus: string = '';
  isCancelledRequest: boolean = false;
  requestStatusId: number | null = null;
  
  // Pagination - API-based
  currentPage: number = 1;
  pageSize: number = 25; // Default to 25
  totalPages: number = 1;
  totalCount: number = 0;
  // Grid-compatible filtered employees
  get filteredEmployees(): Employee[] {
    return this.allEmployees;
  }
  
  // Loading states
  isLoading: boolean = false;
  
  // Sorting - updated to use shared interface
  currentSort: SortConfig = { field: null, direction: 'asc' };
  
  // Form fields
  effectiveDate: string = '';
  originalEffectiveDate: string = '';
  notes: string = '';
  
  // Layoff request typepl
  layoffRequestType: RequestTypeDto | null = null;


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
  layoffSearchService = {
    searchEmployees: async (searchTerm: string): Promise<Employee[]> => {
      console.log('LayoffSearchService.searchEmployees called with:', searchTerm);
      
      try {
        const response = await this.employeeService.getEmployeesByHRRequest(
          'layoff-request',
          1,
          25,
          undefined,
          false,
          searchTerm
        ).toPromise();

        console.log('LayoffSearchService API response:', response);
        if (response && response.data) {
          const mappedResults = response.data.map(emp => ({
            employeeNumber: parseInt(emp.employeeNumber),
            name: emp.employeeName,
            title: emp.position || '',
            division: emp.divisionCode && emp.divisionName 
              ? `${emp.divisionCode} - ${emp.divisionName}` 
              : emp.divisionName || emp.divisionCode || '',
            department: emp.department || '',
            positionCode: emp.position || '',
            company: emp.companyCode && emp.companyName 
              ? `${emp.companyCode} - ${emp.companyName}` 
              : emp.companyName || emp.companyCode || '',
            companyCode: parseInt(emp.companyCode) || 0,
            hasExistingHRRequest: emp.hasExistingHRRequest
          }));
          console.log('LayoffSearchService mapped results:', mappedResults);
          return mappedResults;
        }
        console.log('LayoffSearchService: No response data, returning empty array');
        return [];
      } catch (error) {
        console.error('LayoffSearchService error:', error);
        
        // Fallback to mock data if API fails
        console.log('Using mock data as fallback');
        const allMockEmployees: Employee[] = [
          { employeeNumber: 1001, name: 'Smith, John', title: 'Site Supervisor', division: 'FIELD - Field Operations', department: 'Commercial Construction', positionCode: 'SITESUPR', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
          { employeeNumber: 1002, name: 'Johnson, Sarah', title: 'Project Manager', division: 'PROJ - Project Management', department: 'Residential Projects', positionCode: 'PROJMGR', company: 'MTHY - Mathy Construction', companyCode: 200 },
          { employeeNumber: 1003, name: 'Chen, Mike', title: 'Heavy Equipment Operator', division: 'FIELD - Field Operations', department: 'Infrastructure', positionCode: 'EQPOPER', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
          { employeeNumber: 1004, name: 'Davis, Emily', title: 'Safety Coordinator', division: 'SAFE - Safety & Compliance', department: 'Job Site Safety', positionCode: 'SFTYCOORD', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
          { employeeNumber: 1005, name: 'Rodriguez, David', title: 'Electrician', division: 'TRADE - Trades', department: 'Electrical Services', positionCode: 'ELECT', company: 'MTHY - Mathy Construction', companyCode: 200 },
          { employeeNumber: 1006, name: 'Wang, Lisa', title: 'CAD Technician', division: 'ENG - Engineering', department: 'Design & Drafting', positionCode: 'CADTECH', company: 'MTS - Mathy Transportation Systems', companyCode: 100 }
        ];

        const mockResults = allMockEmployees.filter(emp => 
          emp.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
          emp.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
          emp.division.toLowerCase().includes(searchTerm.toLowerCase()) ||
          emp.company.toLowerCase().includes(searchTerm.toLowerCase())
        );
        
        console.log('LayoffSearchService mock results (fallback):', mockResults);
        return mockResults;
      }
    }
  };
  
  // Employee utility methods
  private findEmployeeByNumber(employeeNumber: number): Employee | undefined {
    return this.allEmployees.find(emp => emp.employeeNumber === employeeNumber) || 
           this.searchResults.find(emp => emp.employeeNumber === employeeNumber);
  }

  // Mock data (fallback) - updated return type
  getMockEmployees(): Employee[] {
    return [
      { employeeNumber: 1001, name: 'Smith, John', title: 'Site Supervisor', division: 'FIELD - Field Operations', department: 'Commercial Construction', positionCode: 'SITESUPR', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
      { employeeNumber: 1002, name: 'Johnson, Sarah', title: 'Project Manager', division: 'PROJ - Project Management', department: 'Residential Projects', positionCode: 'PROJMGR', company: 'MTHY - Mathy Construction', companyCode: 200 },
      { employeeNumber: 1003, name: 'Chen, Mike', title: 'Heavy Equipment Operator', division: 'FIELD - Field Operations', department: 'Infrastructure', positionCode: 'EQPOPER', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
      { employeeNumber: 1004, name: 'Davis, Emily', title: 'Safety Coordinator', division: 'SAFE - Safety & Compliance', department: 'Job Site Safety', positionCode: 'SFTYCOORD', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
      { employeeNumber: 1005, name: 'Rodriguez, David', title: 'Electrician', division: 'TRADE - Trades', department: 'Electrical Services', positionCode: 'ELECT', company: 'MTHY - Mathy Construction', companyCode: 200 },
      { employeeNumber: 1006, name: 'Wang, Lisa', title: 'CAD Technician', division: 'ENG - Engineering', department: 'Design & Drafting', positionCode: 'CADTECH', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
      { employeeNumber: 1007, name: 'Wilson, James', title: 'Cost Estimator', division: 'PRECON - Pre-Construction', department: 'Estimating', positionCode: 'COSTESTIM', company: 'MTHY - Mathy Construction', companyCode: 200 },
      { employeeNumber: 1008, name: 'Thompson, Emma', title: 'Carpenter', division: 'TRADE - Trades', department: 'Framing & Finish', positionCode: 'CARPNTR', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
      { employeeNumber: 1009, name: 'Martinez, Robert', title: 'Crane Operator', division: 'FIELD - Field Operations', department: 'Heavy Equipment', positionCode: 'CRANEOPR', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
      { employeeNumber: 1010, name: 'Lee, Jennifer', title: 'Concrete Finisher', division: 'TRADE - Trades', department: 'Concrete & Masonry', positionCode: 'CONCRFIN', company: 'MTHY - Mathy Construction', companyCode: 200 },
      { employeeNumber: 1011, name: 'Turner, Alex', title: 'Foreman', division: 'FIELD - Field Operations', department: 'Commercial Construction', positionCode: 'FOREMAN', company: 'MTS - Mathy Transportation Systems', companyCode: 100 },
      { employeeNumber: 1012, name: 'Garcia, Maria', title: 'Plumber', division: 'TRADE - Trades', department: 'Plumbing Services', positionCode: 'PLUMBER', company: 'MTHY - Mathy Construction', companyCode: 200 }
    ];
  }

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private location: Location,
    private authService: AuthService,
    private toasterService: ToasterService,
    private referenceDataService: ReferenceDataService,
    private hrRequestService: HRRequestService,
    private employeeService: EmployeeService
  ) {}

  async ngOnInit(): Promise<void> {
    await this.checkUserAuthorization();
    
    // Load layoff request type
    await this.loadLayoffRequestType();
    
    // Check for parentId parameter
    this.route.queryParams.subscribe(params => {
      if (params['parentId']) {
        this.parentId = parseInt(params['parentId'], 10);
        this.isEditMode = true;
        console.log('Layoff-request component loaded in edit mode with parentId:', this.parentId);
        this.loadExistingRequest();
      } else {
        // Initialize search component mode — default to division
        this.isEditMode = false;
        this.showSearchResults = false;
        this.showEmployeeGrid = true;
        this.loadActiveEmployees();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }


  private async checkUserAuthorization(): Promise<void> {
    const isAuthorized = await this.authService.checkUserAuthorization();
    if (!isAuthorized) {
      console.log('User not authorized for layoff request');
      this.router.navigate(['/unauthorized']);
    }
  }

  private async loadLayoffRequestType(): Promise<void> {
    try {
      const response = await this.referenceDataService.getRequestTypes(undefined, 'Layoff').toPromise();
      if (response?.success && response.data && response.data.length > 0) {
        this.layoffRequestType = response.data[0];
      }
    } catch (error) {
      console.error('Error loading layoff request type:', error);
    }
  }

  private async loadExistingRequest(): Promise<void> {
    if (!this.parentId) return;

    this.isLoading = true;
    try {
      // Load the main HR request data
      const detailsResponse = await this.hrRequestService.getHRRequestDetailsByParentId(this.parentId).toPromise();
      
      if (detailsResponse?.success && detailsResponse.data && detailsResponse.data.length > 0) {
        // Get the first request detail to extract common data
        const firstDetail = detailsResponse.data[0];
        
        console.log('Raw API response detail:', firstDetail);
        console.log('Available properties:', Object.keys(firstDetail));
        console.log('effectiveDate value:', firstDetail.effectiveDate);
        console.log('processingNotes value:', firstDetail.processingNotes);
        
        // Populate form fields from loaded data
        // Extract YYYY-MM-DD from the date string without timezone conversion
        // This prevents the date from shifting due to UTC conversion
        this.effectiveDate = firstDetail.effectiveDate ? firstDetail.effectiveDate.split('T')[0] : '';
        this.originalEffectiveDate = this.effectiveDate;
        console.log('Effective date set to:', this.effectiveDate);
        
        this.notes = firstDetail.processingNotes || '';
        
        console.log('Final effectiveDate set to:', this.effectiveDate);
        console.log('Final notes set to:', this.notes);
        
        // Convert API response to Employee interface format for compatibility
        this.selectedEmployees = detailsResponse.data.map(detail => ({
          employeeNumber: detail.employeeId,
          name: detail.employeeName || 'Unknown Employee',
          title: detail.requestTypeName || 'Unknown Position',
          division: 'REQ - From Request',
          department: 'From Request',
          positionCode: 'REQ',
          company: 'REQ - From Request',
          companyCode: 0
        }));
        
        // Check if any of the request details have 'Cancelled' status
        const hasCancelledStatus = detailsResponse.data.some(detail => 
          detail.requestStatusName && detail.requestStatusName.toLowerCase() === 'cancelled'
        );
        
        this.isCancelledRequest = hasCancelledStatus;
        this.requestStatus = hasCancelledStatus ? 'Cancelled' : (detailsResponse.data[0]?.requestStatusName || 'Unknown');
        
        // Capture the request status ID for cancel button visibility
        this.requestStatusId = detailsResponse.data[0]?.requestStatusId || null;
        
        console.log('Final loaded data check:', {
          effectiveDate: this.effectiveDate,
          notes: this.notes,
          selectedEmployeeCount: this.selectedEmployees.length,
          requestStatus: this.requestStatus,
          isCancelledRequest: this.isCancelledRequest
        });
        
        // Force change detection to update the UI
        setTimeout(() => {
          console.log('After timeout - effectiveDate:', this.effectiveDate);
        }, 100);
      } else {
        console.error('No request details found for parentId:', this.parentId);
        // Fall back to normal create mode
        this.isEditMode = false;
        this.parentId = null;
      }
    } catch (error) {
      console.error('Error loading existing layoff request:', error);
      // Fall back to normal create mode
      this.isEditMode = false;
      this.parentId = null;
    } finally {
      this.isLoading = false;
    }
  }

  private async loadActiveEmployees(divisionSearchTerm?: string): Promise<void> {
    this.isLoading = true;
    try {
      // Map frontend field name to backend field name for API call
      const backendField = this.mapFrontendFieldToBackend(this.currentSort.field);
      const orderBy = backendField || undefined;
      const orderByDesc = this.currentSort.direction === 'desc';
      
      const response = await this.employeeService.getEmployeesByHRRequest(
        'layoff-request',
        this.currentPage,
        this.pageSize,
        orderBy,
        orderByDesc,
        divisionSearchTerm // Pass division search term for API filtering
      ).toPromise();

      if (response) {
        // Convert EmployeeDto to Employee interface
        this.allEmployees = response.data.map(emp => ({
          employeeNumber: parseInt(emp.employeeNumber),
          name: emp.employeeName,
          title: emp.position,
          division: emp.divisionCode && emp.divisionName 
            ? `${emp.divisionCode} - ${emp.divisionName}` 
            : emp.divisionName || emp.divisionCode || '',
          department: emp.department || '',
          positionCode: emp.position || '',
          company: emp.companyCode && emp.companyName 
            ? `${emp.companyCode} - ${emp.companyName}` 
            : emp.companyName || emp.companyCode || '',
          companyCode: parseInt(emp.companyCode) || 0,
          hasExistingHRRequest: emp.hasExistingHRRequest
        }));
        
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.currentPage = response.currentPage;
      }
    } catch (error) {
      console.error('Error loading active employees:', error);
      // Fallback to mock data
      this.allEmployees = this.getMockEmployees();
      this.totalCount = this.allEmployees.length;
      this.totalPages = Math.ceil(this.totalCount / this.pageSize);
    } finally {
      this.isLoading = false;
      this.displayEmployeeGrid();
    }
  }




  displayEmployeeGrid(): void {
    // Pagination is handled by the API, so we just ensure current page is valid
    if (this.currentPage > this.totalPages && this.totalPages > 0) {
      this.currentPage = 1;
    }
  }

  get paginatedEmployees(): Employee[] {
    // For API pagination, return all filtered employees (already paginated by API)
    return this.filteredEmployees;
  }

  get gridInfo(): string {
    const startIndex = (this.currentPage - 1) * this.pageSize + 1;
    const endIndex = Math.min(this.currentPage * this.pageSize, this.totalCount);
    const total = this.totalCount;

    if (total === 0) {
      return 'No employees found';
    }
    return `Showing ${startIndex}-${endIndex} of ${total} employees`;
  }

  get pageInfo(): string {
    return `Page ${this.currentPage} of ${this.totalPages}`;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadActiveEmployees();
    }
  }


  changePageSize(newSize: number): void {
    this.pageSize = newSize;
    this.currentPage = 1;
    this.loadActiveEmployees();
  }

  sortGrid(field: string): void {
    if (this.currentSort.field === field) {
      this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
    } else {
      this.currentSort.field = field;
      this.currentSort.direction = 'asc';
    }

    this.currentPage = 1;
    this.loadActiveEmployees();
  }

  getSortIndicator(field: string): string {
    if (this.currentSort.field === field) {
      return this.currentSort.direction === 'asc' ? '↑' : '↓';
    }
    return '↕';
  }

  isSortActive(field: string): boolean {
    return this.currentSort.field === field;
  }

  toggleEmployee(employeeNumber: number): void {
    const employee = this.findEmployeeByNumber(employeeNumber);
    if (!employee) return;

    // Don't allow toggling disabled employees
    if (this.isEmployeeDisabled(employee)) return;

    const existingIndex = this.selectedEmployees.findIndex(sel => sel.employeeNumber === employeeNumber);

    if (existingIndex > -1) {
      this.selectedEmployees.splice(existingIndex, 1);
    } else {
      this.selectedEmployees.push(employee);
    }

    // No need to reload data, just update UI state
  }

  isEmployeeSelected(employeeNumber: number): boolean {
    return this.selectedEmployees.some(emp => emp.employeeNumber === employeeNumber);
  }


  // Method for shared Employee interface (grid compatibility)
  isEmployeeDisabled(employee: Employee): boolean {
    return employee.hasExistingHRRequest === true;
  }

  removeEmployee(employeeNumber: number): void {
    const index = this.selectedEmployees.findIndex(emp => emp.employeeNumber === employeeNumber);
    if (index > -1) {
      this.selectedEmployees.splice(index, 1);
    }
    
    // No need to reload data, just update UI state
    console.log('Selected employees after removal:', this.selectedEmployees);
  }

  isFormValid(): boolean {
    return this.selectedEmployees.length > 0 && this.effectiveDate !== '';
  }

  async submitLayoff(): Promise<void> {
    if (!this.isFormValid()) {
      this.toasterService.showWarning('Please select at least one employee and provide the last day of employment', 'Form Validation');
      return;
    }

    if (!this.layoffRequestType) {
      this.toasterService.showError('Unable to submit request: Layoff request type not loaded', 'System Error');
      return;
    }

    this.isLoading = true;
    try {
      const employeeIds = this.selectedEmployees.map(emp => emp.employeeNumber);
      
      const requestData: CreateMultiEmployeeHRRequestDto = {
        requestTypeId: this.layoffRequestType.id,
        employeeIds: employeeIds,
        effectiveDate: this.effectiveDate,
        processingNotes: this.notes ?? "",
        notes: this.notes ?? "",
        requestTitle: `Layoff Request - ${employeeIds.length} employee(s)`,
        requestDescription: `Layoff request for ${employeeIds.length} employee(s) effective ${this.effectiveDate}`,
        requestedBy: 1, // TODO: Get from user context
        companyId: undefined,
        payrollGroupId: undefined
      };

      const response = await this.hrRequestService.createLayoffRequest(requestData).toPromise();
      
      if (response?.success) {
        console.log('Layoff Request submitted successfully:', response.data);
        this.toasterService.showSuccess(
          `Layoff request submitted successfully for ${this.selectedEmployees.length} employee(s)`,
          'Request Submitted'
        );
        
        // Add delay to allow toaster to display before navigation
        setTimeout(() => {
          this.goBack();
        }, 500);
      } else {
        throw new Error(response?.message || 'Request submission failed');
      }
    } catch (error: any) {
      console.error('Error submitting layoff request:', error);
      
      let errorMessage = 'Error submitting layoff request. Please try again.';
      
      if (error.error && error.error.errors) {
        const validationErrors = Object.keys(error.error.errors).map(key => 
          `${key}: ${error.error.errors[key].join(', ')}`
        ).join('\n');
        errorMessage = `Validation errors:\n${validationErrors}`;
      } else if (error.error?.message) {
        errorMessage = error.error.message;
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      this.toasterService.showError(errorMessage, 'Submission Error');
    } finally {
      this.isLoading = false;
    }
  }

  // Event handlers for the employee grid component
  onEmployeeToggle(employeeNumber: number): void {
    this.toggleEmployee(employeeNumber); // Use employeeNumber which is mapped from id
  }

  onPageChange(page: number): void {
    this.goToPage(page);
  }

  onPageSizeChange(newSize: number): void {
    this.changePageSize(newSize);
  }

  onSortChange(sortConfig: SortConfig): void {
    // Keep the frontend field name for the grid component's comparison logic
    this.currentSort = {
      field: sortConfig.field,  // Keep original frontend field name
      direction: sortConfig.direction
    };
    
    // Apply the sorting by reloading active employees with new sort parameters
    this.currentPage = 1;
    this.loadActiveEmployees();
  }

  /**
   * Map frontend field names to backend field names for sorting
   */
  private mapFrontendFieldToBackend(field: string | null): string | null {
    if (!field) return null;
    
    const fieldMapping: { [key: string]: string } = {
      'name': 'employeename',
      'employeeNumber': 'employeenumber', 
      'company': 'companycode',
      'division': 'division',
      'positionCode': 'position'  // Map positionCode to position
    };
    
    return fieldMapping[field] || field;
  }

  // New event handlers for autonomous search component
  onSearchResultsChanged(results: Employee[]): void {
    this.searchResults = results;
  }

  onSearchModeChanged(mode: 'employee' | 'division'): void {
    console.log('Search mode changed to:', mode);
    // The search component handles mode switching internally
    // We just need to make sure the right views are shown
  }

  onShowGridChanged(showGrid: boolean): void {
    this.showEmployeeGrid = showGrid;
    this.showSearchResults = !showGrid;
    
    if (showGrid) {
      // Load employees for grid view (division mode)
      this.loadActiveEmployees();
    }
  }

  onDivisionSearchChanged(searchTerm: string): void {
    console.log('Division search changed to:', searchTerm);
    // Update the loadActiveEmployees to filter by division/company
    this.loadActiveEmployeesWithDivisionFilter(searchTerm);
  }

  private async loadActiveEmployeesWithDivisionFilter(searchTerm: string): Promise<void> {
    // Reset to first page when searching
    this.currentPage = 1;
    await this.loadActiveEmployees(searchTerm);
  }

  goBack(): void {
    this.location.back();
  }

  goBackToDashboard(): void {
    this.location.back();
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
    if (!this.parentId || this.isUpdatingDate) return;

    if (!this.effectiveDate) {
      this.toasterService.showError('Please select a date');
      return;
    }

    this.isUpdatingDate = true;
    this.hrRequestService.updateEffectiveDate(this.parentId, this.effectiveDate).subscribe({
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