import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  ApiHRRequestDto,
  ApiHRRequestDetailDto,
  CreateHRRequestDto,
  CreateMultiEmployeeHRRequestDto,
  CreateSingleEmployeeHRRequestDto,
  UpdateHRRequestDto,
  UpdateHRRequestDetailDto,
  ApiResponse,
  PagedResponse
} from '../../models/api-hr-request.model';
import { CreateNewHireRequest, NewHireRequestViewDto } from '../../models/new-hire-request.model';
import { CreatePromotionRequestDto } from '../../models/promotion-request.model';

// Re-export types for easier imports
export { CreateMultiEmployeeHRRequestDto, CreateSingleEmployeeHRRequestDto, CreateTerminationRequestDto, ApiResponse, PagedResponse } from '../../models/api-hr-request.model';
export { CreateNewHireRequest } from '../../models/new-hire-request.model';
export { CreatePromotionRequestDto } from '../../models/promotion-request.model';

@Injectable({
  providedIn: 'root'
})
export class HRRequestService {
  private readonly baseUrl = `${environment.apiUrl}/hrrequests`;

  constructor(private http: HttpClient) {}

  /**
   * Get all HR requests with optional filtering and pagination
   */
  getHRRequests(
    page: number = 1,
    pageSize: number = 25,
    requestTypeId?: number,
    statusId?: number,
    submittedBy?: number
  ): Observable<PagedResponse<ApiHRRequestDto[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (requestTypeId) {
      params = params.set('requestTypeId', requestTypeId.toString());
    }
    
    if (statusId) {
      params = params.set('statusId', statusId.toString());
    }
    
    if (submittedBy) {
      params = params.set('submittedBy', submittedBy.toString());
    }

    return this.http.get<PagedResponse<ApiHRRequestDto[]>>(this.baseUrl, { params });
  }

  /**
   * Get HR request by ID
   */
  getHRRequestById(id: number): Observable<ApiResponse<ApiHRRequestDto>> {
    return this.http.get<ApiResponse<ApiHRRequestDto>>(`${this.baseUrl}/${id}`);
  }

  /**
   * Create a new HR request
   */
  createHRRequest(request: CreateHRRequestDto): Observable<ApiResponse<ApiHRRequestDto>> {
    return this.http.post<ApiResponse<ApiHRRequestDto>>(this.baseUrl, request);
  }

  /**
   * Create a new multi-employee HR request
   */
  createMultiEmployeeHRRequest(request: CreateMultiEmployeeHRRequestDto): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/multi-employee`, request);
  }

  /**
   * Create a layoff request
   */
  createLayoffRequest(request: CreateMultiEmployeeHRRequestDto): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/layoffrequests/CreateLayoffRequest`, request);
  }

  /**
   * Create a termination request
   */
  createTerminationRequest(request: CreateSingleEmployeeHRRequestDto): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/terminationrequests/CreateTerminationRequest`, request);
  }

  /**
   * Create a promotion request
   */
  createPromotionRequest(request: CreatePromotionRequestDto): Observable<ApiResponse<any>> {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/PromotionRequests/CreatePromotionRequest`, request, { headers });
  }

  /**
   * Get termination request details by parent ID
   */
  getTerminationRequestDetails(parentId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${environment.apiUrl}/terminationrequests/GetTerminationDetailsByParentId/${parentId}`);
  }

  /**
   * Update an existing HR request
   */
  updateHRRequest(id: number, request: UpdateHRRequestDto): Observable<ApiResponse<ApiHRRequestDto>> {
    return this.http.put<ApiResponse<ApiHRRequestDto>>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Delete an HR request (soft delete)
   */
  deleteHRRequest(id: number): Observable<ApiResponse<boolean>> {
    return this.http.delete<ApiResponse<boolean>>(`${this.baseUrl}/${id}`);
  }

  /**
   * Get HR requests by status
   */
  getHRRequestsByStatus(
    statusId: number,
    page: number = 1,
    pageSize: number = 25
  ): Observable<PagedResponse<ApiHRRequestDto[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResponse<ApiHRRequestDto[]>>(`${this.baseUrl}/status/${statusId}`, { params });
  }

  /**
   * Get HR requests by submitter
   */
  getHRRequestsBySubmitter(
    submittedBy: number,
    page: number = 1,
    pageSize: number = 25
  ): Observable<PagedResponse<ApiHRRequestDto[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResponse<ApiHRRequestDto[]>>(`${this.baseUrl}/submitter/${submittedBy}`, { params });
  }

  /**
   * Get HR requests by type
   */
  getHRRequestsByType(
    requestTypeId: number,
    page: number = 1,
    pageSize: number = 25
  ): Observable<PagedResponse<ApiHRRequestDto[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResponse<ApiHRRequestDto[]>>(`${this.baseUrl}/type/${requestTypeId}`, { params });
  }

  /**
   * Get current user's HR requests
   */
  getMyHRRequests(
    page: number = 1,
    pageSize: number = 25
  ): Observable<PagedResponse<ApiHRRequestDto[]>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResponse<ApiHRRequestDto[]>>(`${this.baseUrl}/my-requests`, { params });
  }

  /**
   * Update HR request detail status
   */
  updateHRRequestDetail(
    detailId: number, 
    updateDto: UpdateHRRequestDetailDto
  ): Observable<ApiResponse<any>> {
    return this.http.put<ApiResponse<any>>(`${this.baseUrl}/details/${detailId}`, updateDto);
  }

  /**
   * Process HR request detail with Viewpoint integration
   */
  processHRRequestDetail(detailId: number): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/details/${detailId}/process`, {});
  }

  /**
   * Get all HR request details across all request types with optional filtering, pagination, and sorting
   */
  getAllHRRequestDetails(
    page: number = 1,
    pageSize: number = 25,
    requestTypeId?: number,
    statusId?: number,
    employeeId?: number,
    submittedBy?: number,
    searchTerm?: string,
    sortField?: string,
    sortDirection?: 'asc' | 'desc'
  ): Observable<PagedResponse<ApiHRRequestDetailDto[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (requestTypeId) {
      params = params.set('requestTypeId', requestTypeId.toString());
    }
    
    if (statusId) {
      params = params.set('statusId', statusId.toString());
    }
    
    if (employeeId) {
      params = params.set('employeeId', employeeId.toString());
    }
    
    if (submittedBy) {
      params = params.set('submittedBy', submittedBy.toString());
    }

    if (searchTerm && searchTerm.trim()) {
      params = params.set('searchTerm', searchTerm.trim());
    }
    
    if (sortField) {
      params = params.set('sortField', sortField);
    }
    
    if (sortDirection) {
      params = params.set('sortDirection', sortDirection);
    }

    return this.http.get<PagedResponse<ApiHRRequestDetailDto[]>>(`${this.baseUrl}/details`, { params });
  }

  /**
   * Get HR request details by parent HR request ID
   */
  getHRRequestDetailsByParentId(parentId: number): Observable<ApiResponse<ApiHRRequestDetailDto[]>> {
    return this.http.get<ApiResponse<ApiHRRequestDetailDto[]>>(`${this.baseUrl}/${parentId}/details`);
  }

  /**
   * Retry a failed HR request detail
   */
  retryHRRequestDetail(detailId: number): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/details/${detailId}/retry`, {});
  }

  /**
   * Cancel an HR request detail
   */
  cancelHRRequestDetail(detailId: number): Observable<ApiResponse<ApiHRRequestDetailDto>> {
    return this.http.post<ApiResponse<ApiHRRequestDetailDto>>(`${this.baseUrl}/details/${detailId}/cancel`, {});
  }

  /**
   * Create a new hire request
   */
  createNewHireRequest(request: CreateNewHireRequest): Observable<ApiResponse<any>> {
    console.log('Sending new hire request:', JSON.stringify(request, null, 2));
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/NewHireRequests/CreateNewHireRequest`, request, { headers });
  }

  /**
   * Save a new hire request as draft
   */
  saveNewHireRequestAsDraft(request: CreateNewHireRequest): Observable<ApiResponse<any>> {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/NewHireRequests/SaveNewHireRequestAsDraft`, request, { headers });
  }

  /**
   * Update existing new hire request as draft
   */
  updateNewHireRequestAsDraft(parentId: number, request: CreateNewHireRequest): Observable<ApiResponse<any>> {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/NewHireRequests/UpdateNewHireRequestAsDraft/${parentId}`, request, { headers });
  }

  /**
   * Update existing new hire request and submit (change status to pending)
   */
  updateNewHireRequest(parentId: number, request: CreateNewHireRequest): Observable<ApiResponse<any>> {
    console.log('Updating and submitting new hire request:', parentId, JSON.stringify(request, null, 2));
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/NewHireRequests/UpdateNewHireRequest/${parentId}`, request, { headers });
  }

  /**
   * Update effective date for all details under a parent HR request
   */
  updateEffectiveDate(parentId: number, effectiveDate: string): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${this.baseUrl}/${parentId}/effective-date`, { effectiveDate });
  }

  /**
   * Update Work Phone Number and Work Extension for a new hire request (ECM_ADMIN only)
   */
  updateNewHirePhoneInfo(parentId: number, phoneInfo: { workPhoneNumber?: string; workExtension?: string }): Observable<ApiResponse<any>> {
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });
    return this.http.put<ApiResponse<any>>(`${environment.apiUrl}/NewHireRequests/UpdatePhoneInfo/${parentId}`, phoneInfo, { headers });
  }

  /**
   * Get comprehensive new hire request details by parent ID for viewing
   * Falls back to generic HR request details if specific endpoint doesn't exist
   */
  getNewHireRequestDetails(parentId: number): Observable<ApiResponse<NewHireRequestViewDto>> {
    // Try the specific new hire endpoint first
    return this.http.get<ApiResponse<NewHireRequestViewDto>>(`${environment.apiUrl}/newhirerequests/GetNewHireDetailsByParentId/${parentId}`)
      .pipe(
        catchError((error) => {
          console.warn('Specific new hire endpoint failed, falling back to generic HR request details:', error);
          // Fallback to generic HR request details endpoint
          return this.getHRRequestDetailsByParentId(parentId).pipe(
            map((response) => {
              // Transform the generic response to match NewHireRequestViewDto structure
              if (response.success && response.data && response.data.length > 0) {
                const hrRequest = response.data[0];
                const transformedData: NewHireRequestViewDto = this.transformToNewHireViewDto(hrRequest, parentId);
                return {
                  success: response.success,
                  message: response.message,
                  data: transformedData
                } as ApiResponse<NewHireRequestViewDto>;
              }
              // If no data, return an error response
              return {
                success: false,
                message: 'No new hire request data found',
                data: null as any
              } as ApiResponse<NewHireRequestViewDto>;
            })
          );
        })
      );
  }

  /**
   * Get promotion request details by parent HR request ID
   */
  getPromotionRequestDetails(parentId: number): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${environment.apiUrl}/promotionrequests/GetPromotionDetailsByParentId/${parentId}`);
  }

  /**
   * Transform generic HR request detail to NewHireRequestViewDto
   * This is a temporary solution until proper backend endpoints are implemented
   */
  private transformToNewHireViewDto(hrRequest: any, parentId: number): NewHireRequestViewDto {
    // Create a robust transformation that handles missing or incomplete data
    const transformedData: NewHireRequestViewDto = {
      // HR Request Information
      parentRequestId: parentId,
      requestTitle: hrRequest.requestTitle || 'New Hire Request',
      requestDescription: hrRequest.requestDescription || '',
      effectiveDate: this.safeParseDate(hrRequest.effectiveDate) || new Date(),
      notes: hrRequest.notes || '',
      createdDate: this.safeParseDate(hrRequest.createdDate) || new Date(),
      requestStatusName: hrRequest.requestStatusName || 'Unknown',
      submittedByName: hrRequest.submittedByName || 'Unknown',

      // HR Request Detail Information
      requestDetailId: hrRequest.id || 0,
      employeeId: hrRequest.employeeId || undefined,
      employeeNetworkId: hrRequest.employeeNetworkId || '',
      employeePositionCode: hrRequest.positionCode || '',

      // Personal Information - Enhanced fallback handling
      firstName: this.getStringValue(hrRequest.firstName, 'First Name'),
      lastName: this.getStringValue(hrRequest.lastName, 'Last Name'),
      suffix: hrRequest.suffix || undefined,
      preferredFirstName: hrRequest.preferredFirstName || undefined,
      firstDayEmployment: this.safeParseDateForNewHire(hrRequest.firstDayEmployment) || this.safeParseDateForNewHire(hrRequest.effectiveDate) || new Date(),
      referredBy: hrRequest.referredBy || undefined,
      rehire: this.safeParseBool(hrRequest.rehire, false) ?? false,

      // Position Information with Display Names - Enhanced fallback
      companyCode: this.getNumberValue(hrRequest.companyCode),
      companyName: this.getStringValue(hrRequest.companyName, 'Company'),
      locationCode: this.getNumberValue(hrRequest.locationCode),
      locationName: this.getStringValue(hrRequest.locationName, 'Location'),
      employmentStatus: this.getStringValue(hrRequest.employmentStatus, 'Employment Status'),
      isUnion: this.safeParseBool(hrRequest.isUnion),
      unionCraftId: this.getNumberValue(hrRequest.unionCraftId),
      unionCraftDescription: hrRequest.unionCraftDescription || undefined,
      isApprentice: this.safeParseBool(hrRequest.isApprentice),
      isUnionWage: this.safeParseBool(hrRequest.isUnionWage),
      salaryCode: this.getNumberValue(hrRequest.salaryCode),
      positionCode: hrRequest.positionCode || '',
      positionName: this.getStringValue(hrRequest.positionName, 'Position'),
      payrollDeptCode: this.getNumberValue(hrRequest.payrollDeptCode),
      payrollDeptName: this.getStringValue(hrRequest.payrollDeptName, 'Payroll Department'),
      supervisorId: this.getNumberValue(hrRequest.supervisorId),
      supervisorName: this.getStringValue(hrRequest.supervisorName, 'Supervisor'),
      appPercentage: hrRequest.appPercentage || '',

      // Related Information - Initialize with empty arrays but provide placeholders if data exists
      creditCardInfo: this.transformCreditCardInfo(hrRequest),
      vehicleInfo: this.transformVehicleInfo(hrRequest),
      itInfo: this.transformITInfo(hrRequest),
      phoneInfo: this.transformPhoneInfo(hrRequest),
      applications: hrRequest.applications || [],
      folders: hrRequest.folders || [],
      tabletProfiles: hrRequest.tabletProfiles || [],
      computerRequirements: hrRequest.computerRequirements || [],
      buildingAccess: hrRequest.buildingAccess || []
    };

    return transformedData;
  }

  // Helper methods for safe data transformation
  private safeParseDate(dateValue: any): Date | null {
    if (!dateValue) return null;
    const parsed = new Date(dateValue);
    return isNaN(parsed.getTime()) ? null : parsed;
  }

  // Timezone-safe date parsing specifically for New Hire requests
  private safeParseDateForNewHire(dateValue: any): Date | null {
    if (!dateValue) return null;

    // If it's already a Date object, return it
    if (dateValue instanceof Date) return dateValue;

    // Parse ISO date string without timezone conversion
    const dateStr = dateValue.toString();
    if (dateStr.includes('T')) {
      const datePart = dateStr.split('T')[0];
      const parts = datePart.split('-');
      if (parts.length === 3) {
        const year = parseInt(parts[0], 10);
        const month = parseInt(parts[1], 10) - 1; // Month is 0-indexed
        const day = parseInt(parts[2], 10);
        return new Date(year, month, day);
      }
    }

    // Fallback to original behavior for other date formats
    return this.safeParseDate(dateValue);
  }

  private safeParseBool(value: any, defaultValue?: boolean): boolean | undefined {
    if (value === null || value === undefined) return defaultValue;
    if (typeof value === 'boolean') return value;
    if (typeof value === 'string') {
      const lower = value.toLowerCase();
      if (lower === 'true' || lower === 'yes' || lower === '1') return true;
      if (lower === 'false' || lower === 'no' || lower === '0') return false;
    }
    if (typeof value === 'number') return value !== 0;
    return defaultValue;
  }

  private getStringValue(value: any, placeholder?: string): string {
    if (value && typeof value === 'string' && value.trim()) {
      return value.trim();
    }
    return placeholder ? `[${placeholder}]` : '';
  }

  private getNumberValue(value: any): number {
    if (typeof value === 'number' && !isNaN(value)) return value;
    if (typeof value === 'string') {
      const parsed = parseInt(value, 10);
      return isNaN(parsed) ? 0 : parsed;
    }
    return 0;
  }

  // Transform nested object info with safe fallbacks
  private transformCreditCardInfo(hrRequest: any): any | undefined {
    if (hrRequest.creditCardInfo) return hrRequest.creditCardInfo;
    // Create minimal structure if any credit card related fields exist
    if (hrRequest.kwikTripCard !== undefined || hrRequest.companyExpenseCard !== undefined) {
      return {
        kwikTripCard: this.safeParseBool(hrRequest.kwikTripCard, false),
        companyExpenseCard: this.safeParseBool(hrRequest.companyExpenseCard, false),
        creditExpenseType: hrRequest.creditExpenseType || undefined,
        weeklyLimit: this.getNumberValue(hrRequest.weeklyLimit),
        fuelCardlockAccess: this.safeParseBool(hrRequest.fuelCardlockAccess, false),
        fuelCardlockAddress: hrRequest.fuelCardlockAddress || undefined
      };
    }
    return undefined;
  }

  private transformVehicleInfo(hrRequest: any): any | undefined {
    if (hrRequest.vehicleInfo) return hrRequest.vehicleInfo;
    // Create minimal structure if any vehicle related fields exist
    if (hrRequest.isApprovedToOperate !== undefined || hrRequest.needCompanyCar !== undefined) {
      return {
        isApprovedToOperate: this.safeParseBool(hrRequest.isApprovedToOperate, false),
        driverClassification: hrRequest.driverClassification || undefined,
        drugAndAlcoholProfile: hrRequest.drugAndAlcoholProfile || undefined,
        needCompanyCar: this.safeParseBool(hrRequest.needCompanyCar, false),
        isApplicationPart2Complete: this.safeParseBool(hrRequest.isApplicationPart2Complete, false)
      };
    }
    return undefined;
  }

  private transformITInfo(hrRequest: any): any | undefined {
    if (hrRequest.itInfo) return hrRequest.itInfo;
    // Create minimal structure if any IT related fields exist
    if (hrRequest.emailRequired !== undefined || hrRequest.mSOfficeLicenseE5 !== undefined) {
      return {
        emailRequired: this.safeParseBool(hrRequest.emailRequired, false),
        alternateDeliveryLocation: hrRequest.alternateDeliveryLocation || undefined,
        mSOfficeLicenseE5: this.safeParseBool(hrRequest.mSOfficeLicenseE5, false),
        mSOfficeLicenseF3: this.safeParseBool(hrRequest.mSOfficeLicenseF3, false)
      };
    }
    return undefined;
  }

  private transformPhoneInfo(hrRequest: any): any | undefined {
    if (hrRequest.phoneInfo) return hrRequest.phoneInfo;
    // Create minimal structure if any phone related fields exist
    if (hrRequest.deskPhone !== undefined || hrRequest.companyCellphone !== undefined || hrRequest.workPhoneNumber) {
      return {
        deskPhone: this.safeParseBool(hrRequest.deskPhone, false),
        companyCellphone: this.safeParseBool(hrRequest.companyCellphone, false),
        byodCellphone: this.safeParseBool(hrRequest.byodCellphone, false),
        workPhoneNumber: hrRequest.workPhoneNumber || undefined,
        workExtension: hrRequest.workExtension || undefined,
        reusingExistingPhone: this.safeParseBool(hrRequest.reusingExistingPhone, false)
      };
    }
    return undefined;
  }

  /**
   * Create Active Directory user
   */
  createADUser(request: CreateADUserRequest): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${environment.apiUrl}/newhirerequests/create-ad-user`, request);
  }

  /**
   * Send test email notification to Azure Service Bus queue
   */
  sendTestEmail(request: EmailNotificationDto): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${environment.apiUrl}/emailtest/send-single`, request);
  }

  /**
   * Send multiple test email notifications to Azure Service Bus queue
   */
  sendBulkTestEmails(requests: EmailNotificationDto[]): Observable<ApiResponse<number>> {
    return this.http.post<ApiResponse<number>>(`${environment.apiUrl}/emailtest/send-bulk`, requests);
  }

  /**
   * Check Azure Service Bus queue status
   */
  checkEmailQueueStatus(): Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(`${environment.apiUrl}/emailtest/queue-status`);
  }
}

interface CreateADUserRequest {
  companyCode: number;
  payrollDeptCode: number;
  preferredFirstName?: string;
  firstName: string;
  lastName: string;
  title?: string;
  department?: string;
}

interface EmailNotificationDto {
  toEmail: string;
  ccEmail?: string;
  subject: string;
  body: string;
  requestId?: number;
  templateId?: number;
  notificationType: string;
  priority: number;
  templateData?: { [key: string]: string };
  module?: string;
  trigger?: string;
}