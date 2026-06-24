import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Location } from '@angular/common';
import { AppHeaderComponent } from '../../../shared/app-header/app-header.component';
import { BackToHomepageButtonComponent } from '../../../shared/back-to-homepage-button/back-to-homepage-button.component';
import { CancelRequestButtonComponent } from '../../../shared/cancel-request-button/cancel-request-button.component';
import { SingleEmployeeSearchComponent, SelectedEmployee, SingleEmployeeSearchConfig } from '../../../shared/single-employee-search';
import { AuthService } from '../../../core/services/auth.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { EmployeeService, EmployeeDto } from '../../../core/services/employee.service';
import { ReferenceDataService, TerminationReasonDto } from '../../../core/services/reference-data.service';
import { HRRequestService, CreateSingleEmployeeHRRequestDto, CreateTerminationRequestDto } from '../../../core/services/hr-request.service';
import { SearchableDropdownComponent, SearchableDropdownConfig } from '../../../shared/searchable-dropdown';
import { Employee } from '../../../shared/employee-grid/employee-grid.interface';

@Component({
  selector: 'app-termination-request',
  standalone: true,
  imports: [CommonModule, FormsModule, AppHeaderComponent, BackToHomepageButtonComponent, CancelRequestButtonComponent, SingleEmployeeSearchComponent, SearchableDropdownComponent],
  templateUrl: './termination-request.component.html',
  styleUrls: ['../../../shared/styles/common.css', './termination-request.component.css']
})
export class TerminationRequestComponent implements OnInit {
  // State properties for edit mode
  isEditMode: boolean = false;
  parentId: number | null = null;
  requestDetailId: number | null = null;
  
  // Cancel request state
  isCancelledRequest: boolean = false;
  requestStatusId: number | null = null;
  requestStatusName: string = '';
  
  selectedEmployee: SelectedEmployee | null = null;
  
  // Employee search configuration
  searchConfig: SingleEmployeeSearchConfig = {
    placeholder: 'Search for employee to terminate...',
    minSearchLength: 2,
    debounceTime: 300,
    theme: 'danger'
  };
  
  // API search service
  searchService = {
    searchEmployees: async (searchTerm: string): Promise<SelectedEmployee[]> => {
      console.log('TerminationSearchService.searchEmployees called with:', searchTerm);
      
      try {
        const response = await this.employeeService.getEmployeesByHRRequest(
          'termination-request',
          1,
          25,
          undefined,
          false,
          searchTerm
        ).toPromise();

        console.log('TerminationSearchService API response:', response);
        if (response && response.data) {
          const mappedResults = response.data.map(emp => ({
            id: parseInt(emp.employeeNumber),
            name: emp.employeeName,
            title: emp.position || '',
            division: emp.divisionCode && emp.divisionName 
              ? `${emp.divisionCode} - ${emp.divisionName}` 
              : emp.divisionName || emp.divisionCode || '',
            department: emp.department || '',
            employeeNumber: parseInt(emp.employeeNumber),
            hasExistingHRRequest: emp.hasExistingHRRequest,
            currentData: emp
          }));
          console.log('TerminationSearchService mapped results:', mappedResults);
          return mappedResults;
        }
        console.log('TerminationSearchService: No response data, returning empty array');
        return [];
      } catch (error) {
        console.error('TerminationSearchService error:', error);
        
        // Fallback to mock data if API fails
        console.log('Using mock data as fallback');
        const mockResults = this.getMockEmployees().filter(emp => 
          emp.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
          emp.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
          emp.division.toLowerCase().includes(searchTerm.toLowerCase())
        );
        
        console.log('TerminationSearchService mock results (fallback):', mockResults);
        return mockResults;
      }
    }
  };
  
  // Form fields
  reason: string = '';
  effectiveDate: string = '';
  originalEffectiveDate: string = '';
  forwardEmail: string = '';
  forwardDeskPhone: string = '';
  forwardCellPhone: string = '';
  autoReply: string = '';
  oneDriveAccess: string = '';
  withKwikTripCard: boolean = false;
  kwikCard4DigitNo: string = '';
  notes: string = '';
  isSubmitting: boolean = false;

  // Searchable dropdown configuration for termination reasons
  terminationReasonConfig: SearchableDropdownConfig<any> = {
    placeholder: 'Please select employee first..',
    displayProperty: 'displayText',
    valueProperty: 'reasonCode',
    noResultsText: 'No matching termination reasons found',
    minSearchLength: 0
  };

  // Termination reasons data
  terminationReasons: TerminationReasonDto[] = [];
  
  
  // Mock data (fallback) - moved to method to match layoff component pattern
  getMockEmployees(): SelectedEmployee[] {
    return [
      { id: 1, name: 'Smith, John', title: 'Site Supervisor', division: 'FIELD - Field Operations', department: 'Commercial Construction', employeeNumber: 1001 },
      { id: 2, name: 'Johnson, Sarah', title: 'Project Manager', division: 'PROJ - Project Management', department: 'Residential Projects', employeeNumber: 1002 },
      { id: 3, name: 'Chen, Mike', title: 'Heavy Equipment Operator', division: 'FIELD - Field Operations', department: 'Infrastructure', employeeNumber: 1003 },
      { id: 4, name: 'Davis, Emily', title: 'Safety Coordinator', division: 'SAFE - Safety & Compliance', department: 'Job Site Safety', employeeNumber: 1004 },
      { id: 5, name: 'Rodriguez, David', title: 'Electrician', division: 'TRADE - Trades', department: 'Electrical Services', employeeNumber: 1005 },
      { id: 6, name: 'Wang, Lisa', title: 'CAD Technician', division: 'ENG - Engineering', department: 'Design & Drafting', employeeNumber: 1006 },
      { id: 7, name: 'Wilson, James', title: 'Cost Estimator', division: 'PRECON - Pre-Construction', department: 'Estimating', employeeNumber: 1007 },
      { id: 8, name: 'Thompson, Emma', title: 'Carpenter', division: 'TRADE - Trades', department: 'Framing & Finish', employeeNumber: 1008 },
      { id: 9, name: 'Martinez, Robert', title: 'Crane Operator', division: 'FIELD - Field Operations', department: 'Heavy Equipment', employeeNumber: 1009 },
      { id: 10, name: 'Lee, Jennifer', title: 'Concrete Finisher', division: 'TRADE - Trades', department: 'Concrete & Masonry', employeeNumber: 1010 },
      { id: 11, name: 'Turner, Alex', title: 'Foreman', division: 'FIELD - Field Operations', department: 'Commercial Construction', employeeNumber: 1011 },
      { id: 12, name: 'Garcia, Maria', title: 'Plumber', division: 'TRADE - Trades', department: 'Plumbing Services', employeeNumber: 1012 }
    ];
  }

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private location: Location,
    private authService: AuthService,
    private toasterService: ToasterService,
    private employeeService: EmployeeService,
    private referenceDataService: ReferenceDataService,
    private hrRequestService: HRRequestService
  ) {}

  async ngOnInit(): Promise<void> {
    // Initialize route parameters
    this.initializeRouteParameters();

    await this.checkUserAuthorization();

    if (this.isEditMode && this.parentId) {
      await this.loadExistingRequest();
    }
  }

  onEmployeeSelected(employee: SelectedEmployee): void {
    this.selectedEmployee = employee;

    // Clear the selected reason since the available reasons may change
    this.reason = '';

    // Update placeholder to indicate reason selection is now available
    this.terminationReasonConfig = {
      ...this.terminationReasonConfig,
      placeholder: 'Select a reason...'
    };

    // Reload termination reasons filtered by the selected employee's company
    if (employee.currentData?.companyCode) {
      const companyCode = parseInt(employee.currentData.companyCode);
      this.loadTerminationReasons(companyCode);
    } else {
      // Fallback to loading all termination reasons if no company code available
      this.loadTerminationReasons();
    }
  }

  onEmployeeCleared(): void {
    this.selectedEmployee = null;
    this.resetForm();

    // Clear termination reasons when employee is cleared
    this.terminationReasons = [];

    // Reset placeholder to indicate employee selection is required
    this.terminationReasonConfig = {
      ...this.terminationReasonConfig,
      placeholder: 'Please select employee first..'
    };
  }


  isFormValid(): boolean {
    // In edit mode, form validation is not needed for submission
    if (this.isEditMode) {
      return false; // No submission in edit mode
    }
    return !!(this.selectedEmployee && this.reason && this.effectiveDate);
  }

  async submitTermination(): Promise<void> {
    if (!this.isFormValid()) {
      this.toasterService.showWarning('Please fill in all required fields', 'Form Validation');
      return;
    }

    this.isSubmitting = true;

    try {
      // Prepare termination-specific details
      const terminationDetails: CreateTerminationRequestDto = {
        reasonCode: this.reason,
        forwardEmail: this.forwardEmail || undefined,
        forwardDeskPhone: this.forwardDeskPhone || undefined,
        forwardCellPhone: this.forwardCellPhone || undefined,
        autoReply: this.autoReply || undefined,
        giveOneDriveAccessTo: this.oneDriveAccess || undefined,
        withKwikTripCard: this.withKwikTripCard,
        kwikCard4DigitNo: this.withKwikTripCard ? (this.kwikCard4DigitNo || undefined) : undefined
      };

      // Create the API request payload
      const request: CreateSingleEmployeeHRRequestDto = {
        requestTypeId: 3, // Termination request type ID
        employeeId: this.selectedEmployee!.id,
        effectiveDate: this.effectiveDate,
        processingNotes: this.notes || '',
        notes: this.notes || '',
        requestTitle: `Termination Request - ${this.selectedEmployee!.name}`,
        requestDescription: `Termination request for employee ${this.selectedEmployee!.name} (${this.selectedEmployee!.employeeNumber}) with reason: ${this.reason}`,
        requestedBy: 1, // TODO: Get from user context
        companyId: undefined, // TODO: Get from employee data or context
        payrollGroupId: undefined, // TODO: Get from employee data or context
        terminationDetails: terminationDetails
      };

      console.log('Submitting termination request:', request);

      const response = await this.hrRequestService.createTerminationRequest(request).toPromise();

      if (response?.success) {
        this.toasterService.showSuccess(
          `Termination request submitted successfully for ${this.selectedEmployee!.name}`,
          'Request Submitted'
        );

        // Add delay to allow toaster to display before navigation
        setTimeout(() => {
          this.goBack();
        }, 1500);
      } else {
        const errorMessage = response?.message || 'Failed to submit termination request';
        this.toasterService.showError(errorMessage, 'Submission Failed');
      }
    } catch (error) {
      console.error('Error submitting termination request:', error);
      this.toasterService.showError(
        'An unexpected error occurred while submitting the termination request',
        'Submission Failed'
      );
    } finally {
      this.isSubmitting = false;
    }
  }

  goBack(): void {
    this.location.back();
  }

  onRequestCancelled(): void {
    // Handle any post-cancellation logic if needed
    console.log('Request has been cancelled');
  }

  private async loadTerminationReasons(companyCode?: number): Promise<void> {
    try {
      const response = await this.referenceDataService.getTerminationReasonsWithCache(companyCode).toPromise();
      if (response?.success && response.data) {
        // Display only the description in the dropdown (reasonCode is still used as the bound value)
        this.terminationReasons = response.data.map(reason => ({
          ...reason,
          displayText: reason.reasonDescription
        }));
      }
    } catch (error) {
      console.error('Error loading termination reasons:', error);
      this.toasterService.showError('Failed to load termination reasons', 'Error');
    }
  }

  private async checkUserAuthorization(): Promise<void> {
    const isAuthorized = await this.authService.checkUserAuthorization();
    if (!isAuthorized) {
      console.log('User not authorized for termination request');
      this.router.navigate(['/unauthorized']);
    }
  }

  private resetForm(): void {
    this.reason = '';
    this.effectiveDate = '';
    this.forwardEmail = '';
    this.forwardDeskPhone = '';
    this.forwardCellPhone = '';
    this.autoReply = '';
    this.oneDriveAccess = '';
    this.withKwikTripCard = false;
    this.kwikCard4DigitNo = '';
    this.notes = '';
  }

  private initializeRouteParameters(): void {
    // Check for parentId parameter from route
    const parentIdParam = this.route.snapshot.paramMap.get('parentId');
    if (parentIdParam) {
      this.parentId = parseInt(parentIdParam, 10);
      this.isEditMode = true;
      console.log('Termination component loaded in edit mode with parentId:', this.parentId);
      return;
    }

    // Also check query parameters as an alternative
    const parentIdQuery = this.route.snapshot.queryParamMap.get('parentId');
    if (parentIdQuery) {
      this.parentId = parseInt(parentIdQuery, 10);
      this.isEditMode = true;
      console.log('Termination component loaded with parentId from query params:', this.parentId);
      return;
    }

    // Check for requestDetailId parameter for direct editing
    const requestDetailIdParam = this.route.snapshot.paramMap.get('requestDetailId');
    if (requestDetailIdParam) {
      this.requestDetailId = parseInt(requestDetailIdParam, 10);
      this.isEditMode = true;
      console.log('Termination component loaded in edit mode with requestDetailId:', this.requestDetailId);
      return;
    }

    // If no parentId or requestDetailId found, we're in create mode
    console.log('Termination component loaded in create mode');
    this.isEditMode = false;
    this.parentId = null;
    this.requestDetailId = null;
  }

  private async loadExistingRequest(): Promise<void> {
    if (!this.parentId) {
      console.error('No parentId available for loading existing request');
      return;
    }

    try {
      const response = await this.hrRequestService.getTerminationRequestDetails(this.parentId).toPromise();
      
      if (!response?.success || !response.data) {
        throw new Error(response?.message || 'Failed to load termination request data');
      }

      const { hrRequest, hrRequestDetail, terminationDetail, employeeDetail } = response.data;
      
      // Populate HR request data
      if (hrRequestDetail) {
        // Use camelCase property names from API response
        this.effectiveDate = hrRequestDetail.effectiveDate ? hrRequestDetail.effectiveDate.split('T')[0] : '';
        this.originalEffectiveDate = this.effectiveDate;
        // Load notes from parent HR request instead of request detail
        this.notes = hrRequest?.notes || '';
        
        // Use properly formatted employee data from EmployeeDetail if available, fallback to HR detail
        if (employeeDetail) {
          // Build division display format: "DivisionCode - DivisionName"
          let divisionDisplay = '';
          if (employeeDetail.divisionCode && employeeDetail.divisionName) {
            divisionDisplay = `${employeeDetail.divisionCode} - ${employeeDetail.divisionName}`;
          } else if (employeeDetail.divisionCode) {
            divisionDisplay = employeeDetail.divisionCode;
          } else if (employeeDetail.divisionName) {
            divisionDisplay = employeeDetail.divisionName;
          }

          this.selectedEmployee = {
            id: employeeDetail.employeeNumber,
            name: employeeDetail.employeeName || 'Unknown Employee',
            title: employeeDetail.position || '',
            division: divisionDisplay,
            department: employeeDetail.department || '',
            employeeNumber: employeeDetail.employeeNumber
          };
        } else {
          // Fallback to HR request detail data
          this.selectedEmployee = {
            id: hrRequestDetail.employeeId,
            name: hrRequestDetail.employeeName || 'Unknown Employee',
            title: hrRequestDetail.employeePositonCode || '',
            division: hrRequestDetail.companyName || '',
            department: hrRequestDetail.departmentName || '',
            employeeNumber: hrRequestDetail.employeeId
          };
        }
      }
      
      // Populate termination-specific data
      if (terminationDetail) {
        // Use camelCase property names from API response
        this.reason = terminationDetail.reasonCode || '';
        this.forwardEmail = terminationDetail.forwardEmail || '';
        this.forwardDeskPhone = terminationDetail.forwardDeskPhone || '';
        this.forwardCellPhone = terminationDetail.forwardCellPhone || '';
        this.autoReply = terminationDetail.autoReply || '';
        this.oneDriveAccess = terminationDetail.giveOneDriveAccessTo || '';
        this.withKwikTripCard = terminationDetail.withKwikTripCard || false;
        this.kwikCard4DigitNo = terminationDetail.kwikCard4DigitNo || '';
      }

      // Check if the request is cancelled and capture status ID
      if (hrRequestDetail) {
        // Check if the request status is 'Cancelled'
        this.isCancelledRequest = hrRequestDetail.requestStatusName?.toLowerCase().includes('cancelled') || false;

        // Capture the request status ID and name for cancel button and date editing
        this.requestStatusId = hrRequestDetail.requestStatusId || null;
        this.requestStatusName = hrRequestDetail.requestStatusName || '';
      }

      // Load termination reasons for edit mode (so the selected reason displays correctly)
      const companyCode = employeeDetail?.companyCode ? parseInt(employeeDetail.companyCode) : undefined;
      await this.loadTerminationReasons(companyCode);

    } catch (error) {
      console.error('Error loading existing termination request:', error);
      this.toasterService.showError('Failed to load termination request data', 'Error');
      
      // Fallback to create mode
      this.isEditMode = false;
      this.parentId = null;
      this.requestDetailId = null;
    }
  }

  get canUpdateDate(): boolean {
    if (!this.isEditMode) return false;
    const status = this.requestStatusName?.toLowerCase() || '';
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