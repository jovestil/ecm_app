import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EmployeeSyncResult {
  totalProcessed: number;
  insertedCount: number;
  updatedCount: number;
  deletedCount: number;
  errorCount: number;
  errors: string[];
  syncStartTime: string;
  syncEndTime: string;
  syncDuration: string;
  hasMore: boolean;
  page: number;
  pageSize: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors?: string[];
}

export interface PagedResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors?: string[];
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  orderBy?: string;
  orderByDesc?: boolean;
}

export interface EmployeeDto {
  employeeNumber: string;
  employeeName: string;
  companyCode: string;
  companyName: string;
  divisionCode?: string;
  divisionName?: string;
  email: string;
  workEmail?: string;
  position: string;
  department: string;
  payrollDeptCode?: number;
  payrollCompanyCode?: number;
  payrollGroupCode?: number;
  physicalLocationCode?: number;
  isActive: boolean;
  hasExistingHRRequest: boolean;
  supervisor?: string;
  supervisorId?: number;
  hireDate?: string;
  status?: string;
  salaryCode?: number;
}

export interface SyncOptions {
  page?: number;
  pageSize?: number;
  filter?: string;
}

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private readonly apiUrl = `${environment.apiUrl}/employees`;

  constructor(private http: HttpClient) {}

  /**
   * Sync employees from Viewpoint API (paginated - legacy, use syncAllEmployeesFromViewpoint for full sync)
   */
  syncEmployeesFromViewpoint(options: SyncOptions = {}): Observable<ApiResponse<EmployeeSyncResult>> {
    let params = new HttpParams();

    if (options.page) {
      params = params.set('page', options.page.toString());
    }

    if (options.pageSize) {
      params = params.set('pageSize', options.pageSize.toString());
    }

    if (options.filter) {
      params = params.set('filter', options.filter);
    }

    return this.http.post<ApiResponse<EmployeeSyncResult>>(
      `${this.apiUrl}/sync/viewpoint`,
      {},
      { params }
    );
  }

  /**
   * Full-sweep sync: fetches every Viewpoint employee in one call, upserts them,
   * and soft-deletes any ECM employee whose (CompanyCode, EmployeeNumber) is
   * absent from the Viewpoint payload. The response's deletedCount reflects
   * the number of rows flagged IsDeleted=true in this run.
   */
  syncAllEmployeesFromViewpoint(): Observable<ApiResponse<EmployeeSyncResult>> {
    return this.http.post<ApiResponse<EmployeeSyncResult>>(
      `${this.apiUrl}/sync/viewpoint/full`,
      {}
    );
  }

  /**
   * Get all employees from Viewpoint API (for preview/testing)
   */
  getViewpointEmployees(page: number = 1, pageSize: number = 25, filter?: string): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (filter) {
      params = params.set('filter', filter);
    }

    return this.http.get<any>(`${this.apiUrl}/viewpoint/all`, { params });
  }

  /**
   * Get active employees with pagination and search
   */
  getActiveEmployees(
    page: number = 1, 
    pageSize: number = 25, 
    orderBy?: string, 
    orderByDesc: boolean = false, 
    search?: string
  ): Observable<PagedResponse<EmployeeDto[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (orderBy) {
      params = params.set('orderBy', orderBy);
    }
    
    if (orderByDesc) {
      params = params.set('orderByDesc', 'true');
    }
    
    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResponse<EmployeeDto[]>>(`${this.apiUrl}/active`, { params });
  }

  /**
   * Get employees by HR request type with pagination and search
   */
  getEmployeesByHRRequest(
    requestType: string,
    page: number = 1, 
    pageSize: number = 25, 
    orderBy?: string, 
    orderByDesc: boolean = false, 
    search?: string,
    isEditMode: boolean = false,
    employeeIds?: number[]
  ): Observable<PagedResponse<EmployeeDto[]>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (orderBy) {
      params = params.set('orderBy', orderBy);
    }
    
    if (orderByDesc) {
      params = params.set('orderByDesc', 'true');
    }
    
    if (search) {
      params = params.set('search', search);
    }

    if (isEditMode) {
      params = params.set('isEditMode', 'true');
    }

    // Add employee IDs as query parameters if provided
    if (employeeIds && employeeIds.length > 0) {
      employeeIds.forEach(id => {
        params = params.append('employeeIds', id.toString());
      });
    }

    return this.http.get<PagedResponse<EmployeeDto[]>>(`${this.apiUrl}/hr-request/${requestType}`, { params });
  }

  /**
   * Get layoff employees with pagination and search
   * @deprecated Use getEmployeesByHRRequest with requestType 'return-to-work' instead
   */
  getLayoffEmployees(
    page: number = 1, 
    pageSize: number = 25, 
    orderBy?: string, 
    orderByDesc: boolean = false, 
    search?: string,
    isEditMode: boolean = false
  ): Observable<PagedResponse<EmployeeDto[]>> {
    return this.getEmployeesByHRRequest('return-to-work', page, pageSize, orderBy, orderByDesc, search, isEditMode);
  }
}