import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  ApiEmployee, 
  ReturnToWorkFormData, 
  EmployeeSearchParams, 
  EmployeeSearchResult, 
  ViewpointEmployeeDto, 
  ReturnToWorkSubmissionData 
} from '../../models/return-to-work.model';
import { Employee } from '../../shared/employee-grid/employee-grid.interface';
import { 
  CreateMultiEmployeeHRRequestDto, 
  ApiHRRequestDetailDto, 
  ApiResponse 
} from '../../models/api-hr-request.model';
import { HRRequestService } from './hr-request.service';
import { ReferenceDataService } from './reference-data.service';
import { EmployeeService } from './employee.service';

@Injectable({
  providedIn: 'root'
})
export class ReturnToWorkService {
  private readonly apiUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private hrRequestService: HRRequestService,
    private referenceDataService: ReferenceDataService,
    private employeeService: EmployeeService
  ) {}

  /**
   * Get the request type ID for return-to-work requests
   */
  getReturnToWorkRequestTypeId(): number | null {
    const cachedRequestTypes = this.referenceDataService.getCachedRequestTypes();
    const returnToWorkType = cachedRequestTypes.find(rt => 
      rt.requestTypeName.toLowerCase().includes('returntowork') || 
      rt.requestTypeName.toLowerCase().includes('return to work') ||
      rt.requestTypeName === 'ReturnToWork'
    );
    return returnToWorkType ? returnToWorkType.id : null;
  }

  /**
   * Get request type ID with fallback to API call
   */
  async getReturnToWorkRequestTypeIdWithFallback(): Promise<number> {
    let requestTypeId = this.getReturnToWorkRequestTypeId();
    
    if (!requestTypeId) {
      console.log('RequestTypeId not found in cache, fetching from API...');
      const response = await this.referenceDataService.getRequestTypeByNameWithCache('ReturnToWork').toPromise();
      if (response?.success && response.data && response.data.length > 0) {
        requestTypeId = response.data[0].id;
      }
    }

    if (!requestTypeId) {
      throw new Error('Could not find RequestTypeId for ReturnToWork request type');
    }

    return requestTypeId;
  }

  /**
   * Load existing return-to-work request for edit mode using hrrequests/{id}
   */
  async loadExistingRequest(parentId: number): Promise<{ 
    effectiveDate: string; 
    notes: string; 
    selectedEmployees: Employee[];
    isCancelled: boolean;
    status: string;
    requestStatusId: number | null;
  }> {
    console.log('Loading existing HR request with parentId:', parentId);
    
    // Get the HR request with details using hrrequests/{id}
    const hrRequest = await this.hrRequestService.getHRRequestById(parentId).toPromise();
    
    if (!hrRequest?.success || !hrRequest.data) {
      throw new Error('Failed to load HR request');
    }

    // Pre-populate form fields from existing request
    const notes = hrRequest.data.notes || '';
    
    console.log('Loaded existing HR request:', hrRequest.data);

    // Use the details directly from the HR request response
    const hrRequestDetails = hrRequest.data.details;
    
    if (!hrRequestDetails || hrRequestDetails.length === 0) {
      throw new Error('No employee details found in HR request');
    }

    // Get effective date and status ID from the first HR request detail (all should have the same date and status)
    const firstDetail = hrRequestDetails[0];
    // Extract date directly from string without timezone conversion to avoid date shift
    // The effectiveDate from API is typically in ISO format (e.g., "2025-12-29T00:00:00")
    // Using toISOString() would convert to UTC and shift the date backwards
    const effectiveDate = firstDetail.effectiveDate ?
      firstDetail.effectiveDate.toString().substring(0, 10) : '';
    const requestStatusId = firstDetail.requestStatusId || null;
    
    // Extract employee IDs from HR request details
    const employeeIds = hrRequestDetails.map(detail => detail.employeeId);
    console.log('Employee IDs to enrich:', employeeIds);
    
    // Enrich employee data from Viewpoint API
    const enrichedEmployeeMap = await this.enrichEmployeeData(employeeIds);
    
    // Convert HR request details to Employee objects, using enriched data where available
    const selectedEmployees = hrRequestDetails.map(detail => {
      const enrichedEmployee = enrichedEmployeeMap.get(detail.employeeId);
      
      if (enrichedEmployee) {
        // Use enriched employee data from Viewpoint API
        console.log(`Using enriched data for employee ${detail.employeeId}:`, enrichedEmployee);
        return enrichedEmployee;
      } else {
        // Fall back to HR request detail data if enrichment failed
        console.log(`Using HR request detail data for employee ${detail.employeeId} (enrichment failed)`);
        return {
          employeeNumber: detail.employeeId,
          name: detail.employeeName || `Employee ${detail.employeeId}`,
          title: detail.employeePositonCode || 'Employee',
          division: this.formatDivisionDisplay(detail),
          department: detail.departmentName || 'Unknown Department',
          positionCode: detail.employeePositonCode || 'UNKNOWN',
          company: this.formatCompanyDisplay(detail),
          companyCode: detail.employeeCompanyCode || 0
        };
      }
    });

    // Check if any of the request details have 'Cancelled' status
    const hasCancelledStatus = hrRequestDetails.some(detail => 
      detail.requestStatusName && detail.requestStatusName.toLowerCase() === 'cancelled'
    );
    
    const status = hasCancelledStatus ? 'Cancelled' : (hrRequestDetails[0]?.requestStatusName || 'Unknown');

    console.log('Loaded selected employees with enriched data:', selectedEmployees);
    console.log('Loaded effective date:', effectiveDate);
    console.log('Request status:', status, 'isCancelled:', hasCancelledStatus);

    return {
      effectiveDate,
      notes,
      selectedEmployees,
      isCancelled: hasCancelledStatus,
      status: status,
      requestStatusId: requestStatusId
    };
  }

  /**
   * Enrich employee data by fetching current information from Viewpoint API
   */
  async enrichEmployeeData(employeeIds: number[]): Promise<Map<number, Employee>> {
    const enrichedEmployeeMap = new Map<number, Employee>();
    
    try {
      // Use the employee service to get laid-off employees for return-to-work
      const response = await this.employeeService.getEmployeesByHRRequest(
        'return-to-work',
        1,
        25, // Large page size to get all employees
        undefined, // orderBy
        false, // orderByDesc
        undefined, // search
        true, // isEditMode - Enable edit mode to get employees regardless of status
        employeeIds // Pass employee IDs for efficient server-side filtering
      ).toPromise();
      
      if (response && response.data && Array.isArray(response.data)) {
        const apiEmployees: ApiEmployee[] = response.data.map(emp => this.mapEmployeeDtoToApiEmployee(emp));
        
        // Map employees (server already filtered by employee IDs)
        apiEmployees.forEach(apiEmp => {
          const mappedEmployee = this.mapApiEmployeeToEmployee(apiEmp);
          enrichedEmployeeMap.set(Number(mappedEmployee.employeeNumber), mappedEmployee);
        });
        
        console.log(`Enriched ${enrichedEmployeeMap.size} employees from Viewpoint API`);
      }
    } catch (error) {
      console.error('Error enriching employee data from Viewpoint:', error);
    }
    
    return enrichedEmployeeMap;
  }

  /**
   * Search for laid-off employees
   */
  async searchLaidOffEmployees(searchParams: EmployeeSearchParams): Promise<EmployeeSearchResult> {
    try {
      const response = await this.employeeService.getEmployeesByHRRequest(
        'return-to-work',
        searchParams.page,
        searchParams.pageSize,
        searchParams.orderBy ? this.mapFieldToApiField(searchParams.orderBy) : undefined,
        searchParams.orderByDesc || false,
        searchParams.searchQuery && searchParams.searchQuery.trim() ? searchParams.searchQuery.trim() : undefined,
        searchParams.isEditMode || false
      ).toPromise();
      
      let apiEmployees: ApiEmployee[] = [];
      
      // Handle the PagedResponse structure from the endpoint
      if (response && response.data && Array.isArray(response.data)) {
        apiEmployees = response.data.map(emp => this.mapEmployeeDtoToApiEmployee(emp));
      }
      
      const employees = apiEmployees.map(emp => this.mapApiEmployeeToEmployee(emp));
      
      // Extract pagination info from PagedResponse
      const totalCount = response?.totalCount || employees.length;
      const totalPages = Math.ceil(totalCount / searchParams.pageSize);
      
      return {
        employees,
        totalCount,
        totalPages,
        currentPage: searchParams.page,
        pageSize: searchParams.pageSize
      };
      
    } catch (error) {
      console.error('Error searching employees:', error);
      return {
        employees: [],
        totalCount: 0,
        totalPages: 1,
        currentPage: searchParams.page,
        pageSize: searchParams.pageSize
      };
    }
  }

  /**
   * Submit return-to-work request with complete 4-step process including ReturnToWorkRequestDetail
   */
  async submitReturnToWorkRequest(formData: ReturnToWorkFormData): Promise<ReturnToWorkSubmissionData> {
    console.log('Submitting return-to-work request:', formData);

    // Use new CreateReturnToWorkRequest endpoint that handles all steps with rollback
    const apiUrl = `${this.apiUrl}/ReturnToWorkRequests/CreateReturnToWorkRequest`;
    console.log('Calling create return-to-work endpoint:', apiUrl);
    
    // Prepare employee list for Viewpoint API call
    const employeeList: ViewpointEmployeeDto[] = formData.selectedEmployees.map(employee => ({
      HRCo: employee.companyCode,  // Use numeric company code
      PREmp: employee.employeeNumber,
      HRRef: employee.employeeNumber,          // Add HRRef field (same as PREmp for employee reference)
      FirstName: employee.name.split(' ')[0] || '',
      LastName: employee.name.split(' ').slice(-1)[0] || '',
      Status: employee.status
    }));

    // Prepare HR request data
    const hrRequestDto: CreateMultiEmployeeHRRequestDto = {
      requestTypeId: formData.requestTypeId,
      employeeIds: formData.selectedEmployees.map(emp => emp.employeeNumber),
      effectiveDate: formData.effectiveDate,
      processingNotes: formData.notes ?? "",
      notes: formData.notes ?? "",
      requestTitle: `Return to Work Request - ${formData.selectedEmployees.length} employee(s)`,
      requestDescription: `Return to work request for ${formData.selectedEmployees.length} employee(s)`,
      requestedBy: 1, // TODO: Get from user context
      companyId: undefined,
      payrollGroupId: undefined
    };

    // Combined request payload
    const completeRequestPayload = {
      employees: employeeList,
      hrRequest: hrRequestDto
    };

    console.log('Complete return-to-work request payload:', completeRequestPayload);
    
    const response = await this.http.post(apiUrl, completeRequestPayload).toPromise() as any;
    
    if (!response?.success) {
      throw new Error(response?.message || 'Return-to-work request submission failed');
    }

    console.log('Complete return-to-work request completed successfully:', response);

    // Success - all steps completed with ReturnToWorkRequestDetails saved
    const submissionData: ReturnToWorkSubmissionData = {
      employees: formData.selectedEmployees,
      effectiveDate: formData.effectiveDate,
      notes: formData.notes,
      submittedBy: 'Current User',
      submittedDate: new Date().toISOString()
    };

    console.log('Return to Work Request submitted successfully with all details:', submissionData);
    return submissionData;
  }

  /**
   * Map EmployeeDto to ApiEmployee interface for backward compatibility
   */
  private mapEmployeeDtoToApiEmployee(employeeDto: any): ApiEmployee {
    return {
      employeeId: parseInt(employeeDto.employeeNumber) || 0,
      employeeNumber: parseInt(employeeDto.employeeNumber) || 0,
      employeeName: employeeDto.employeeName || '',
      companyCode: employeeDto.companyCode || '',
      companyName: employeeDto.companyName || '',
      divisionCode: employeeDto.divisionCode || '',
      divisionName: employeeDto.divisionName || '',
      position: employeeDto.position || '',
      department: employeeDto.department || '',
      email: employeeDto.email || '',
      isActive: employeeDto.isActive || false,
      hasExistingHRRequest: employeeDto.hasExistingHRRequest || false
    };
  }

  /**
   * Map API employee data to internal Employee interface
   */
  mapApiEmployeeToEmployee(apiEmp: ApiEmployee): Employee {
    return {
      employeeNumber: apiEmp.employeeNumber || 0,
      name: apiEmp.employeeName || 'Unknown Name',
      title: apiEmp.status || apiEmp.position || 'Employee',
      division: apiEmp.divisionCode && apiEmp.divisionName 
        ? `${apiEmp.divisionCode} - ${apiEmp.divisionName}` 
        : apiEmp.divisionName || apiEmp.divisionCode || 'Unknown Division',
      department: apiEmp.department || 'Unknown Department',
      positionCode: apiEmp.position || 'UNKNOWN',
      company: apiEmp.companyCode && apiEmp.companyName 
        ? `${apiEmp.companyCode} - ${apiEmp.companyName}` 
        : apiEmp.companyName || apiEmp.companyCode || 'Unknown Company',
      companyCode: parseInt(apiEmp.companyCode || '0') || 0,
      hasExistingHRRequest: apiEmp.hasExistingHRRequest,
      status: apiEmp.status
    };
  }

  /**
   * Map UI field names to API field names for sorting
   */
  private mapFieldToApiField(field: string): string {
    const fieldMapping: { [key: string]: string } = {
      'name': 'employeeName',
      'company': 'companyCode',
      'division': 'division',
      'employeeNumber': 'employeeNumber',
      'positionCode': 'position'
    };
    
    return fieldMapping[field] || field;
  }

  /**
   * Format division display from HR request detail data
   */
  private formatDivisionDisplay(detail: ApiHRRequestDetailDto): string {
    // Use department name as division if available, otherwise use a formatted approach
    if (detail.departmentName) {
      return detail.departmentName;
    }
    
    // If we have employee department code, format it
    if (detail.employeeDepartmentCode) {
      return `Dept ${detail.employeeDepartmentCode}`;
    }
    
    return 'Unknown Division';
  }

  /**
   * Format company display from HR request detail data
   */
  private formatCompanyDisplay(detail: ApiHRRequestDetailDto): string {
    // If we have both company name and code, show both
    if (detail.companyName && detail.employeeCompanyCode) {
      return `${detail.employeeCompanyCode} - ${detail.companyName}`;
    }
    
    // If we only have company name
    if (detail.companyName) {
      return detail.companyName;
    }
    
    // If we only have company code
    if (detail.employeeCompanyCode) {
      return `Company ${detail.employeeCompanyCode}`;
    }
    
    return 'Unknown Company';
  }
}