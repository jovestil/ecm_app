import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../models/api-hr-request.model';

export interface RequestTypeDto {
  id: number;
  requestTypeName: string;
  requestTypeDescription?: string;
  isActive: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface RequestStatusDto {
  id: number;
  requestStatusName: string;
  requestStatusDescription?: string;
  isActive: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface TerminationReasonDto {
  id: number;
  reasonCode: string;
  reasonDescription: string;
  companyCode: number;
  isActive: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface PayrollDepartmentDto {
  id: number;
  companyCode: number;
  deptCode: number;
  deptName: string;
  emailDomain?: string;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface PayrollDepartmentShortNameDto {
  id: number;
  companyCode: number;
  deptCode: number;
  deptShortName: string;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface PayrollGroupDto {
  id: number;
  companyCode: number;
  groupCode: number;
  groupName: string;
  isActive: boolean;
}

export interface CompanyDto {
  id: number;
  companyCode: number;
  companyName: string;
  isActive: boolean;
  viewpointSyncDate?: string;
}

export interface CompanyTypeLocationDto {
  id: number;
  companyCode: number;
  locationType: string;
  isUnion: boolean;
}

export interface PhysicalLocationDto {
  id: number;
  locationCode: number;
  locationName: string;
  isActive: boolean;
  viewpointSyncDate?: string;
}

export interface EmploymentStatusDto {
  id: number;
  companyCode: number;
  status: string;
  description: string;
  isActive: boolean;
  viewpointSyncDate?: string;
}

export interface UnionCraftDto {
  id: number;
  companyCode: number;
  craftCode: string;
  description: string;
  isActive: boolean;
  viewpointSyncDate?: string;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface EmployeeSalaryTypeDto {
  id: number;
  companyCode: number;
  salaryCode: string;
  description: string;
  isActive: boolean;
  viewpointSyncDate?: string;
}

export interface ApprenticePercentageDto {
  id: number;
  appPercentage: string;
  appDescription: string;
  isActive: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface PositionDto {
  id: number;
  companyCode: number;
  positionCode: string;
  positionName: string;
  type?: string;
  isActive: boolean;
  viewpointSyncDate?: string;
}

export interface SupervisorDto {
  id: number;
  employeeNumber: number;
  firstName: string;
  lastName: string;
  fullName: string;
  companyCode: number;
  payrollDeptCode?: number;
  employmentStatus?: string;
}

export interface BuildingAccessRequirementDto {
  id: number;
  companyCode: number;
  description: string;
  locationType: string;
}

export interface TabletProfileDto {
  id: number;
  locationType: string;
  profileName: string;
  isActive: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface ApplicationDto {
  id: number;
  locationType: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface EmployeeLicenseClassDto {
  id: number;
  licenseClass: string;
  description?: string;
  isUnion: boolean;
  viewpointSyncDate?: string;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}

export interface ComputerRequirementDto {
  id: number;
  description: string;
  isChild?: boolean | number;
  parentId?: number;
  isActive: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
}


export interface CompanySyncResultDto {
  totalViewpointCompanies: number;
  newCompaniesAdded: number;
  existingCompaniesUpdated: number;
  companiesDeactivated: number;
  syncDate: string;
  syncDuration: string;
  errors: string[];
  success: boolean;
  summary: string;
}

export interface DepartmentSyncResultDto {
  totalViewpointDepartments: number;
  newDepartmentsAdded: number;
  existingDepartmentsUpdated: number;
  departmentsDeactivated: number;
  syncDate: string;
  syncDuration: string;
  errors: string[];
  success: boolean;
  summary: string;
}

export interface PositionSyncResultDto {
  totalViewpointPositions: number;
  newPositionsAdded: number;
  existingPositionsUpdated: number;
  positionsDeactivated: number;
  syncDate: string;
  syncDuration: string;
  errors: string[];
  success: boolean;
  summary: string;
}

export interface PayrollGroupSyncResultDto {
  totalViewpointPayrollGroups: number;
  newPayrollGroupsAdded: number;
  existingPayrollGroupsUpdated: number;
  payrollGroupsDeactivated: number;
  syncDate: string;
  syncDuration: string;
  errors: string[];
  success: boolean;
  summary: string;
}

export interface UnionCraftSyncResultDto {
  totalViewpointUnionCrafts: number;
  newUnionCraftsAdded: number;
  existingUnionCraftsUpdated: number;
  unionCraftsDeactivated: number;
  syncDate: string;
  syncDuration: string;
  errors: string[];
  success: boolean;
  summary: string;
}

export interface EmploymentStatusSyncResultDto {
  totalViewpointEmploymentStatuses: number;
  newEmploymentStatusesAdded: number;
  existingEmploymentStatusesUpdated: number;
  employmentStatusesDeactivated: number;
  syncDate: string;
  syncDuration: string;
  errors: string[];
  success: boolean;
  summary: string;
}

export interface EmployeeSalaryTypeSyncResultDto {
  totalViewpointSalaryTypes: number;
  newSalaryTypesAdded: number;
  existingSalaryTypesUpdated: number;
  salaryTypesDeactivated: number;
  syncDate: string;
  syncDuration: string;
  errors: string[];
  success: boolean;
  summary: string;
}

export interface ViewpointSyncStatusDto {
  lastCompanySync?: string;
  lastDepartmentSync?: string;
  lastPositionSync?: string;
  lastPayrollGroupSync?: string;
  lastUnionCraftSync?: string;
  lastEmploymentStatusSync?: string;
  lastEmployeeSalaryTypeSync?: string;
  lastEmployeeSync?: string;
  totalCompanies: number;
  totalDepartments: number;
  totalPositions: number;
  totalPayrollGroups: number;
  totalUnionCrafts: number;
  totalEmploymentStatuses: number;
  totalEmployeeSalaryTypes: number;
  totalEmployees: number;
  companySyncStatus: string;
  departmentSyncStatus: string;
  positionSyncStatus: string;
  payrollGroupSyncStatus: string;
  unionCraftSyncStatus: string;
  employmentStatusSyncStatus: string;
  employeeSalaryTypeSyncStatus: string;
  employeeSyncStatus: string;
}

export interface SyncScheduleConfigDto {
  companies: string;
  departments: string;
  positions: string;
  payrollGroups: string;
  unionCrafts: string;
  employmentStatuses: string;
  employeeSalaryTypes: string;
  employees: string;
  lastUpdated: string;
  updatedBy: string;
}

export interface SyncScheduleResultDto {
  success: boolean;
  message: string;
  scheduledJobs: string[];
}

@Injectable({
  providedIn: 'root'
})
export class ReferenceDataService {
  private readonly baseUrl = `${environment.apiUrl}/referencedata`;
  
  // Cache for request types
  private requestTypesCache$ = new BehaviorSubject<RequestTypeDto[]>([]);
  private isCacheInitialized = false;

  // Cache for request statuses
  private requestStatusesCache$ = new BehaviorSubject<RequestStatusDto[]>([]);
  private isStatusCacheInitialized = false;

  // Cache for termination reasons
  private terminationReasonsCache$ = new BehaviorSubject<TerminationReasonDto[]>([]);
  private isTerminationReasonsCacheInitialized = false;

  // Cache for payroll departments (global - kept for backward compatibility)
  private payrollDepartmentsCache$ = new BehaviorSubject<PayrollDepartmentDto[]>([]);
  private isPayrollDepartmentsCacheInitialized = false;

  // Cache for payroll departments (per company)
  private payrollDepartmentsByCompanyCache$ = new Map<number, BehaviorSubject<PayrollDepartmentDto[]>>();
  private isPayrollDepartmentsByCompanyCacheInitialized = new Map<number, boolean>();

  // Cache for payroll department short names (for role dropdown)
  private payrollDepartmentShortNamesCache$ = new BehaviorSubject<PayrollDepartmentShortNameDto[]>([]);
  private isPayrollDepartmentShortNamesCacheInitialized = false;

  // Cache for companies
  private companiesCache$ = new BehaviorSubject<CompanyDto[]>([]);
  private isCompaniesCacheInitialized = false;

  // Cache for physical locations
  private physicalLocationsCache$ = new BehaviorSubject<PhysicalLocationDto[]>([]);
  private isPhysicalLocationsCacheInitialized = false;

  // Cache for employment statuses
  private employmentStatusesCache$ = new BehaviorSubject<EmploymentStatusDto[]>([]);
  private isEmploymentStatusesCacheInitialized = false;

  // Cache for union crafts (per company)
  private unionCraftsCache$ = new Map<number, BehaviorSubject<UnionCraftDto[]>>();
  private isUnionCraftsCacheInitialized = new Map<number, boolean>();

  // Cache for employee salary types (per company)
  private employeeSalaryTypesCache$ = new Map<number, BehaviorSubject<EmployeeSalaryTypeDto[]>>();
  private isEmployeeSalaryTypesCacheInitialized = new Map<number, boolean>();

  // Cache for apprentice percentages
  private apprenticePercentagesCache$ = new BehaviorSubject<ApprenticePercentageDto[]>([]);
  private isApprenticePercentagesCacheInitialized = false;

  // Cache for positions (per company)
  private positionsCache$ = new Map<number, BehaviorSubject<PositionDto[]>>();
  private isPositionsCacheInitialized = new Map<number, boolean>();

  // Cache for supervisors (per company and payroll department combination)
  private supervisorsCache$ = new Map<string, BehaviorSubject<SupervisorDto[]>>();
  private isSupervisorsCacheInitialized = new Map<string, boolean>();

  // Cache for building access requirements (per company)
  private buildingAccessRequirementsCache$ = new Map<number, BehaviorSubject<BuildingAccessRequirementDto[]>>();
  private isBuildingAccessRequirementsCacheInitialized = new Map<number, boolean>();

  // Cache for tablet profiles (per company)
  private tabletProfilesCache$ = new Map<number, BehaviorSubject<TabletProfileDto[]>>();
  private isTabletProfilesCacheInitialized = new Map<number, boolean>();

  // Cache for applications (per company)
  private applicationsCache$ = new Map<number, BehaviorSubject<ApplicationDto[]>>();
  private isApplicationsCacheInitialized = new Map<number, boolean>();

  // Cache for employee license classes
  private employeeLicenseClassesCache$ = new BehaviorSubject<EmployeeLicenseClassDto[]>([]);
  private isEmployeeLicenseClassesCacheInitialized = false;


  constructor(private http: HttpClient) {}

  /**
   * Get request types with optional filtering
   */
  getRequestTypes(
    requestTypeId?: number,
    requestTypeName?: string
  ): Observable<ApiResponse<RequestTypeDto[]>> {
    let params = new HttpParams();
    
    if (requestTypeId) {
      params = params.set('requestTypeId', requestTypeId.toString());
    }
    
    if (requestTypeName) {
      params = params.set('requestTypeName', requestTypeName);
    }

    return this.http.get<ApiResponse<RequestTypeDto[]>>(`${this.baseUrl}/request-types`, { params });
  }

  /**
   * Get request types with caching - loads all active request types and caches them
   */
  getRequestTypesWithCache(): Observable<RequestTypeDto[]> {
    if (!this.isCacheInitialized) {
      this.loadAndCacheRequestTypes();
    }
    return this.requestTypesCache$.asObservable();
  }

  /**
   * Get specific request type by name with caching (e.g., 'ReturnToWork')
   */
  getRequestTypeByNameWithCache(requestTypeName: string): Observable<ApiResponse<RequestTypeDto[]>> {
    return this.getRequestTypes(undefined, requestTypeName).pipe(
      tap(response => {
        if (response.success && response.data) {
          // Update cache with the filtered results
          const currentCache = this.requestTypesCache$.value;
          const newItems = response.data.filter(newItem => 
            !currentCache.some(cached => cached.id === newItem.id)
          );
          if (newItems.length > 0) {
            this.requestTypesCache$.next([...currentCache, ...newItems]);
          }
        }
      })
    );
  }

  /**
   * Get all request statuses with caching
   */
  getRequestStatusesWithCache(): Observable<ApiResponse<RequestStatusDto[]>> {
    if (this.isStatusCacheInitialized && this.requestStatusesCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.requestStatusesCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getRequestStatuses().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.requestStatusesCache$.next(response.data);
            this.isStatusCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Load all active request types and cache them
   */
  private loadAndCacheRequestTypes(): void {
    this.getRequestTypes().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.requestTypesCache$.next(response.data);
          this.isCacheInitialized = true;
        }
      },
      error: (error) => {
        console.error('Error loading request types:', error);
      }
    });
  }

  /**
   * Clear the cache (useful for refreshing data)
   */
  clearCache(): void {
    this.requestTypesCache$.next([]);
    this.isCacheInitialized = false;
  }

  /**
   * Clear the status cache
   */
  clearStatusCache(): void {
    this.requestStatusesCache$.next([]);
    this.isStatusCacheInitialized = false;
  }

  /**
   * Get cached request types without making API call
   */
  getCachedRequestTypes(): RequestTypeDto[] {
    return this.requestTypesCache$.value;
  }

  /**
   * Get cached request statuses without making API call
   */
  getCachedRequestStatuses(): RequestStatusDto[] {
    return this.requestStatusesCache$.value;
  }

  /**
   * Check if cache is initialized
   */
  isCacheReady(): boolean {
    return this.isCacheInitialized;
  }

  /**
   * Check if status cache is initialized
   */
  isStatusCacheReady(): boolean {
    return this.isStatusCacheInitialized;
  }


  /**
   * Get request statuses with optional filtering
   */
  getRequestStatuses(
    requestStatusId?: number,
    requestStatusName?: string
  ): Observable<ApiResponse<RequestStatusDto[]>> {
    let params = new HttpParams();
    
    if (requestStatusId) {
      params = params.set('requestStatusId', requestStatusId.toString());
    }
    
    if (requestStatusName) {
      params = params.set('requestStatusName', requestStatusName);
    }

    return this.http.get<ApiResponse<RequestStatusDto[]>>(`${this.baseUrl}/request-statuses`, { params });
  }

  /**
   * Get specific request status by name (e.g., 'ReturnToWork')
   */
  getRequestStatusByName(requestStatusName: string): Observable<ApiResponse<RequestStatusDto[]>> {
    return this.getRequestStatuses(undefined, requestStatusName);
  }

  /**
   * Get companies with optional filtering
   */
  getCompanies(
    companyId?: number,
    companyCode?: number,
    companyName?: string
  ): Observable<ApiResponse<CompanyDto[]>> {
    let params = new HttpParams();
    
    if (companyId) {
      params = params.set('companyId', companyId.toString());
    }
    
    if (companyCode) {
      params = params.set('companyCode', companyCode.toString());
    }
    
    if (companyName) {
      params = params.set('companyName', companyName);
    }

    return this.http.get<ApiResponse<CompanyDto[]>>(`${this.baseUrl}/companies`, { params });
  }

  /**
   * Get all companies with caching
   */
  getCompaniesWithCache(): Observable<ApiResponse<CompanyDto[]>> {
    if (this.isCompaniesCacheInitialized && this.companiesCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.companiesCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getCompanies().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.companiesCache$.next(response.data);
            this.isCompaniesCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Get cached companies without making API call
   */
  getCachedCompanies(): CompanyDto[] {
    return this.companiesCache$.value;
  }

  /**
   * Clear the companies cache
   */
  clearCompaniesCache(): void {
    this.companiesCache$.next([]);
    this.isCompaniesCacheInitialized = false;
  }

  /**
   * Check if companies cache is initialized
   */
  isCompaniesCacheReady(): boolean {
    return this.isCompaniesCacheInitialized;
  }


  /**
   * Get termination reasons with optional filtering
   */
  getTerminationReasons(
    reasonId?: number,
    reasonCode?: string,
    companyCode?: number
  ): Observable<ApiResponse<TerminationReasonDto[]>> {
    let params = new HttpParams();
    
    if (reasonId) {
      params = params.set('reasonId', reasonId.toString());
    }
    
    if (reasonCode) {
      params = params.set('reasonCode', reasonCode);
    }
    
    if (companyCode) {
      params = params.set('companyCode', companyCode.toString());
    }

    return this.http.get<ApiResponse<TerminationReasonDto[]>>(`${this.baseUrl}/termination-reasons`, { params });
  }

  /**
   * Get all termination reasons with caching
   * Note: When companyCode is provided, caching is bypassed to ensure fresh data
   */
  getTerminationReasonsWithCache(companyCode?: number): Observable<ApiResponse<TerminationReasonDto[]>> {
    // If company filtering is requested, bypass cache and fetch fresh data
    if (companyCode) {
      return this.getTerminationReasons(undefined, undefined, companyCode);
    }
    
    if (this.isTerminationReasonsCacheInitialized && this.terminationReasonsCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.terminationReasonsCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getTerminationReasons().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.terminationReasonsCache$.next(response.data);
            this.isTerminationReasonsCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Get cached termination reasons without making API call
   */
  getCachedTerminationReasons(): TerminationReasonDto[] {
    return this.terminationReasonsCache$.value;
  }

  /**
   * Clear the termination reasons cache
   */
  clearTerminationReasonsCache(): void {
    this.terminationReasonsCache$.next([]);
    this.isTerminationReasonsCacheInitialized = false;
  }

  /**
   * Check if termination reasons cache is initialized
   */
  isTerminationReasonsCacheReady(): boolean {
    return this.isTerminationReasonsCacheInitialized;
  }

  /**
   * Get payroll departments
   */
  getPayrollDepartments(companyCode?: number, deptCode?: number): Observable<ApiResponse<PayrollDepartmentDto[]>> {
    let params = new HttpParams();

    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }

    if (deptCode !== undefined) {
      params = params.set('deptCode', deptCode.toString());
    }

    return this.http.get<ApiResponse<PayrollDepartmentDto[]>>(`${this.baseUrl}/payroll-departments`, { params });
  }

  /**
   * Get payroll groups with optional filtering
   */
  getPayrollGroups(companyCode?: number, groupCode?: number): Observable<ApiResponse<PayrollGroupDto[]>> {
    let params = new HttpParams();

    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }

    if (groupCode !== undefined) {
      params = params.set('groupCode', groupCode.toString());
    }

    return this.http.get<ApiResponse<PayrollGroupDto[]>>(`${this.baseUrl}/payroll-groups`, { params });
  }

  /**
   * Get payroll departments with caching
   */
  getPayrollDepartmentsWithCache(): Observable<ApiResponse<PayrollDepartmentDto[]>> {
    if (this.isPayrollDepartmentsCacheInitialized && this.payrollDepartmentsCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.payrollDepartmentsCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getPayrollDepartments().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.payrollDepartmentsCache$.next(response.data);
            this.isPayrollDepartmentsCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Get cached payroll departments without making API call
   */
  getCachedPayrollDepartments(): PayrollDepartmentDto[] {
    return this.payrollDepartmentsCache$.value;
  }

  /**
   * Clear the payroll departments cache
   */
  clearPayrollDepartmentsCache(): void {
    this.payrollDepartmentsCache$.next([]);
    this.isPayrollDepartmentsCacheInitialized = false;
  }

  /**
   * Check if payroll departments cache is initialized
   */
  isPayrollDepartmentsCacheReady(): boolean {
    return this.isPayrollDepartmentsCacheInitialized;
  }

  /**
   * Get payroll departments with caching for a specific company
   */
  getPayrollDepartmentsByCompanyWithCache(companyCode: number): Observable<ApiResponse<PayrollDepartmentDto[]>> {
    const isInitialized = this.isPayrollDepartmentsByCompanyCacheInitialized.get(companyCode) || false;
    let cache = this.payrollDepartmentsByCompanyCache$.get(companyCode);
    
    if (!cache) {
      cache = new BehaviorSubject<PayrollDepartmentDto[]>([]);
      this.payrollDepartmentsByCompanyCache$.set(companyCode, cache);
    }
    
    if (isInitialized && cache.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: cache!.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getPayrollDepartments(companyCode).pipe(
        tap(response => {
          if (response.success && response.data) {
            cache!.next(response.data);
            this.isPayrollDepartmentsByCompanyCacheInitialized.set(companyCode, true);
          }
        })
      );
    }
  }

  /**
   * Get cached payroll departments for a company without making API call
   */
  getCachedPayrollDepartmentsByCompany(companyCode: number): PayrollDepartmentDto[] {
    const cache = this.payrollDepartmentsByCompanyCache$.get(companyCode);
    return cache ? cache.value : [];
  }

  /**
   * Clear payroll departments cache for a specific company
   */
  clearPayrollDepartmentsCacheByCompany(companyCode: number): void {
    const cache = this.payrollDepartmentsByCompanyCache$.get(companyCode);
    if (cache) {
      cache.next([]);
    }
    this.isPayrollDepartmentsByCompanyCacheInitialized.set(companyCode, false);
  }

  /**
   * Clear all payroll departments caches (company-specific)
   */
  clearAllPayrollDepartmentsByCompanyCache(): void {
    this.payrollDepartmentsByCompanyCache$.forEach((cache, companyCode) => {
      cache.next([]);
      this.isPayrollDepartmentsByCompanyCacheInitialized.set(companyCode, false);
    });
  }

  /**
   * Check if payroll departments cache is initialized for a company
   */
  isPayrollDepartmentsByCCompanyCacheReady(companyCode: number): boolean {
    return this.isPayrollDepartmentsByCompanyCacheInitialized.get(companyCode) || false;
  }

  /**
   * Get payroll department short names
   */
  getPayrollDepartmentShortNames(companyCode?: number, deptCode?: number, deptShortName?: string): Observable<ApiResponse<PayrollDepartmentShortNameDto[]>> {
    let params = new HttpParams();
    
    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }
    
    if (deptCode !== undefined) {
      params = params.set('deptCode', deptCode.toString());
    }
    
    if (deptShortName !== undefined) {
      params = params.set('deptShortName', deptShortName);
    }

    return this.http.get<ApiResponse<PayrollDepartmentShortNameDto[]>>(`${this.baseUrl}/payroll-department-short-names`, { params });
  }

  /**
   * Get payroll department short names with caching
   */
  getPayrollDepartmentShortNamesWithCache(): Observable<ApiResponse<PayrollDepartmentShortNameDto[]>> {
    if (this.isPayrollDepartmentShortNamesCacheInitialized && this.payrollDepartmentShortNamesCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.payrollDepartmentShortNamesCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getPayrollDepartmentShortNames().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.payrollDepartmentShortNamesCache$.next(response.data);
            this.isPayrollDepartmentShortNamesCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Get cached payroll department short names without making API call
   */
  getCachedPayrollDepartmentShortNames(): PayrollDepartmentShortNameDto[] {
    return this.payrollDepartmentShortNamesCache$.value;
  }

  /**
   * Clear the payroll department short names cache
   */
  clearPayrollDepartmentShortNamesCache(): void {
    this.payrollDepartmentShortNamesCache$.next([]);
    this.isPayrollDepartmentShortNamesCacheInitialized = false;
  }

  /**
   * Check if payroll department short names cache is initialized
   */
  isPayrollDepartmentShortNamesCacheReady(): boolean {
    return this.isPayrollDepartmentShortNamesCacheInitialized;
  }

  /**
   * Manually trigger company sync from Viewpoint API
   */
  syncCompaniesFromViewpoint(): Observable<ApiResponse<CompanySyncResultDto>> {
    return this.http.post<ApiResponse<CompanySyncResultDto>>(`${this.baseUrl}/sync/companies`, {});
  }

  /**
   * Manually trigger department sync from Viewpoint API
   */
  syncDepartmentsFromViewpoint(): Observable<ApiResponse<DepartmentSyncResultDto>> {
    return this.http.post<ApiResponse<DepartmentSyncResultDto>>(`${this.baseUrl}/sync/departments`, {});
  }

  /**
   * Manually trigger position sync from Viewpoint API
   */
  syncPositionsFromViewpoint(): Observable<ApiResponse<PositionSyncResultDto>> {
    return this.http.post<ApiResponse<PositionSyncResultDto>>(`${this.baseUrl}/sync/positions`, {});
  }

  /**
   * Manually trigger payroll group sync from Viewpoint API
   */
  syncPayrollGroupsFromViewpoint(): Observable<ApiResponse<PayrollGroupSyncResultDto>> {
    return this.http.post<ApiResponse<PayrollGroupSyncResultDto>>(`${this.baseUrl}/sync/payroll-groups`, {});
  }

  /**
   * Manually trigger union craft sync from Viewpoint API
   */
  syncUnionCraftsFromViewpoint(): Observable<ApiResponse<UnionCraftSyncResultDto>> {
    return this.http.post<ApiResponse<UnionCraftSyncResultDto>>(`${this.baseUrl}/sync/union-crafts`, {});
  }

  /**
   * Manually trigger employment status sync from Viewpoint API
   */
  syncEmploymentStatusesFromViewpoint(): Observable<ApiResponse<EmploymentStatusSyncResultDto>> {
    return this.http.post<ApiResponse<EmploymentStatusSyncResultDto>>(`${this.baseUrl}/sync/employment-statuses`, {});
  }

  /**
   * Manually trigger employee salary type sync from Viewpoint API
   */
  syncEmployeeSalaryTypesFromViewpoint(): Observable<ApiResponse<EmployeeSalaryTypeSyncResultDto>> {
    return this.http.post<ApiResponse<EmployeeSalaryTypeSyncResultDto>>(`${this.baseUrl}/sync/employee-salary-types`, {});
  }

  /**
   * Get Viewpoint sync status with last sync dates for all data types
   */
  getViewpointSyncStatus(): Observable<ApiResponse<ViewpointSyncStatusDto>> {
    return this.http.get<ApiResponse<ViewpointSyncStatusDto>>(`${this.baseUrl}/viewpoint-sync-status`);
  }

  /**
   * Get current sync schedule configuration
   */
  getSyncScheduleConfig(): Observable<ApiResponse<SyncScheduleConfigDto>> {
    return this.http.get<ApiResponse<SyncScheduleConfigDto>>(`${this.baseUrl}/sync-schedule`);
  }

  /**
   * Update sync schedule configuration and set up recurring jobs
   */
  updateSyncScheduleConfig(config: SyncScheduleConfigDto): Observable<ApiResponse<SyncScheduleResultDto>> {
    return this.http.post<ApiResponse<SyncScheduleResultDto>>(`${this.baseUrl}/sync-schedule`, config);
  }

  /**
   * Get company type locations with optional filtering
   */
  getCompanyTypeLocations(
    id?: number,
    companyCode?: number,
    locationType?: string
  ): Observable<ApiResponse<CompanyTypeLocationDto[]>> {
    let params = new HttpParams();
    
    if (id) {
      params = params.set('id', id.toString());
    }
    
    if (companyCode) {
      params = params.set('companyCode', companyCode.toString());
    }
    
    if (locationType) {
      params = params.set('locationType', locationType);
    }

    return this.http.get<ApiResponse<CompanyTypeLocationDto[]>>(`${this.baseUrl}/company-type-locations`, { params });
  }

  /**
   * Get physical locations with optional filtering
   */
  getPhysicalLocations(
    locationId?: number,
    locationCode?: number,
    locationName?: string
  ): Observable<ApiResponse<PhysicalLocationDto[]>> {
    let params = new HttpParams();
    
    if (locationId) {
      params = params.set('locationId', locationId.toString());
    }
    
    if (locationCode) {
      params = params.set('locationCode', locationCode.toString());
    }
    
    if (locationName) {
      params = params.set('locationName', locationName);
    }

    return this.http.get<ApiResponse<PhysicalLocationDto[]>>(`${this.baseUrl}/physical-locations`, { params });
  }

  /**
   * Get all physical locations with caching
   */
  getPhysicalLocationsWithCache(): Observable<ApiResponse<PhysicalLocationDto[]>> {
    if (this.isPhysicalLocationsCacheInitialized && this.physicalLocationsCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.physicalLocationsCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getPhysicalLocations().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.physicalLocationsCache$.next(response.data);
            this.isPhysicalLocationsCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Get cached physical locations without making API call
   */
  getCachedPhysicalLocations(): PhysicalLocationDto[] {
    return this.physicalLocationsCache$.value;
  }

  /**
   * Clear the physical locations cache
   */
  clearPhysicalLocationsCache(): void {
    this.physicalLocationsCache$.next([]);
    this.isPhysicalLocationsCacheInitialized = false;
  }

  /**
   * Check if physical locations cache is initialized
   */
  isPhysicalLocationsCacheReady(): boolean {
    return this.isPhysicalLocationsCacheInitialized;
  }

  /**
   * Get employment statuses with optional filtering
   */
  getEmploymentStatuses(
    id?: number,
    companyCode?: number,
    status?: string
  ): Observable<ApiResponse<EmploymentStatusDto[]>> {
    let params = new HttpParams();
    
    if (id) {
      params = params.set('id', id.toString());
    }
    
    if (companyCode) {
      params = params.set('companyCode', companyCode.toString());
    }
    
    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<ApiResponse<EmploymentStatusDto[]>>(`${this.baseUrl}/employment-statuses`, { params });
  }

  /**
   * Get employment statuses with caching
   * Note: When companyCode is provided, caching is bypassed to ensure fresh company-specific data
   */
  getEmploymentStatusesWithCache(companyCode?: number): Observable<ApiResponse<EmploymentStatusDto[]>> {
    // If company filtering is requested, bypass cache and fetch fresh data
    if (companyCode) {
      return this.getEmploymentStatuses(undefined, companyCode);
    }
    
    if (this.isEmploymentStatusesCacheInitialized && this.employmentStatusesCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.employmentStatusesCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getEmploymentStatuses().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.employmentStatusesCache$.next(response.data);
            this.isEmploymentStatusesCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Get cached employment statuses without making API call
   */
  getCachedEmploymentStatuses(): EmploymentStatusDto[] {
    return this.employmentStatusesCache$.value;
  }

  /**
   * Clear the employment statuses cache
   */
  clearEmploymentStatusesCache(): void {
    this.employmentStatusesCache$.next([]);
    this.isEmploymentStatusesCacheInitialized = false;
  }

  /**
   * Check if employment statuses cache is initialized
   */
  isEmploymentStatusesCacheReady(): boolean {
    return this.isEmploymentStatusesCacheInitialized;
  }

  /**
   * Get union crafts with optional company filtering
   */
  getUnionCrafts(companyCode?: number): Observable<ApiResponse<UnionCraftDto[]>> {
    let params = new HttpParams();
    
    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }

    return this.http.get<ApiResponse<UnionCraftDto[]>>(`${this.baseUrl}/union-crafts`, { params });
  }

  /**
   * Get union crafts with caching for a specific company
   */
  getUnionCraftsWithCache(companyCode: number): Observable<ApiResponse<UnionCraftDto[]>> {
    const isInitialized = this.isUnionCraftsCacheInitialized.get(companyCode) || false;
    let cache = this.unionCraftsCache$.get(companyCode);
    
    if (!cache) {
      cache = new BehaviorSubject<UnionCraftDto[]>([]);
      this.unionCraftsCache$.set(companyCode, cache);
    }
    
    if (isInitialized && cache.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: cache!.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getUnionCrafts(companyCode).pipe(
        tap(response => {
          if (response.success && response.data) {
            cache!.next(response.data);
            this.isUnionCraftsCacheInitialized.set(companyCode, true);
          }
        })
      );
    }
  }

  /**
   * Get cached union crafts for a company without making API call
   */
  getCachedUnionCrafts(companyCode: number): UnionCraftDto[] {
    const cache = this.unionCraftsCache$.get(companyCode);
    return cache ? cache.value : [];
  }

  /**
   * Clear union crafts cache for a specific company
   */
  clearUnionCraftsCache(companyCode: number): void {
    const cache = this.unionCraftsCache$.get(companyCode);
    if (cache) {
      cache.next([]);
    }
    this.isUnionCraftsCacheInitialized.set(companyCode, false);
  }

  /**
   * Clear all union crafts caches
   */
  clearAllUnionCraftsCache(): void {
    this.unionCraftsCache$.forEach((cache, companyCode) => {
      cache.next([]);
      this.isUnionCraftsCacheInitialized.set(companyCode, false);
    });
  }

  /**
   * Check if union crafts cache is initialized for a company
   */
  isUnionCraftsCacheReady(companyCode: number): boolean {
    return this.isUnionCraftsCacheInitialized.get(companyCode) || false;
  }

  /**
   * Get employee salary types with optional company filtering
   */
  getEmployeeSalaryTypes(companyCode?: number): Observable<ApiResponse<EmployeeSalaryTypeDto[]>> {
    const params = new URLSearchParams();
    if (companyCode) {
      params.append('companyCode', companyCode.toString());
    }
    const url = `${this.baseUrl}/employee-salary-types${params.toString() ? '?' + params.toString() : ''}`;
    return this.http.get<ApiResponse<EmployeeSalaryTypeDto[]>>(url);
  }

  /**
   * Get employee salary types with caching (per company)
   */
  getEmployeeSalaryTypesWithCache(companyCode: number): Observable<ApiResponse<EmployeeSalaryTypeDto[]>> {
    const companyCache = this.employeeSalaryTypesCache$.get(companyCode);
    const isCacheInitialized = this.isEmployeeSalaryTypesCacheInitialized.get(companyCode) || false;

    if (isCacheInitialized && companyCache && companyCache.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: companyCache.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getEmployeeSalaryTypes(companyCode).pipe(
        tap(response => {
          if (response.success && response.data) {
            let cache = this.employeeSalaryTypesCache$.get(companyCode);
            if (!cache) {
              cache = new BehaviorSubject<EmployeeSalaryTypeDto[]>([]);
              this.employeeSalaryTypesCache$.set(companyCode, cache);
            }
            cache.next(response.data);
            this.isEmployeeSalaryTypesCacheInitialized.set(companyCode, true);
          }
        })
      );
    }
  }

  /**
   * Get cached employee salary types for a company without making API call
   */
  getCachedEmployeeSalaryTypes(companyCode: number): EmployeeSalaryTypeDto[] {
    const cache = this.employeeSalaryTypesCache$.get(companyCode);
    return cache ? cache.value : [];
  }

  /**
   * Clear the employee salary types cache for a specific company
   */
  clearEmployeeSalaryTypesCache(companyCode: number): void {
    const cache = this.employeeSalaryTypesCache$.get(companyCode);
    if (cache) {
      cache.next([]);
    }
    this.isEmployeeSalaryTypesCacheInitialized.set(companyCode, false);
  }

  /**
   * Clear all employee salary types caches
   */
  clearAllEmployeeSalaryTypesCache(): void {
    this.employeeSalaryTypesCache$.forEach((cache, companyCode) => {
      cache.next([]);
      this.isEmployeeSalaryTypesCacheInitialized.set(companyCode, false);
    });
  }

  /**
   * Check if employee salary types cache is initialized for a company
   */
  isEmployeeSalaryTypesCacheReady(companyCode: number): boolean {
    return this.isEmployeeSalaryTypesCacheInitialized.get(companyCode) || false;
  }

  /**
   * Get apprentice percentages with optional filtering
   */
  getApprenticePercentages(id?: number, appPercentage?: string): Observable<ApiResponse<ApprenticePercentageDto[]>> {
    let params = new HttpParams();
    
    if (id !== undefined) {
      params = params.set('id', id.toString());
    }
    
    if (appPercentage) {
      params = params.set('appPercentage', appPercentage);
    }

    return this.http.get<ApiResponse<ApprenticePercentageDto[]>>(`${this.baseUrl}/apprentice-percentages`, { params });
  }

  /**
   * Get apprentice percentages with caching - loads all active apprentice percentages and caches them
   */
  getApprenticePercentagesWithCache(): Observable<ApiResponse<ApprenticePercentageDto[]>> {
    if (this.isApprenticePercentagesCacheInitialized && this.apprenticePercentagesCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.apprenticePercentagesCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getApprenticePercentages().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.apprenticePercentagesCache$.next(response.data);
            this.isApprenticePercentagesCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Get cached apprentice percentages without making API call
   */
  getCachedApprenticePercentages(): ApprenticePercentageDto[] {
    return this.apprenticePercentagesCache$.value;
  }

  /**
   * Clear the apprentice percentages cache
   */
  clearApprenticePercentagesCache(): void {
    this.apprenticePercentagesCache$.next([]);
    this.isApprenticePercentagesCacheInitialized = false;
  }

  /**
   * Check if apprentice percentages cache is initialized
   */
  isApprenticePercentagesCacheReady(): boolean {
    return this.isApprenticePercentagesCacheInitialized;
  }

  /**
   * Get positions with optional company filtering
   */
  getPositions(companyCode?: number): Observable<ApiResponse<PositionDto[]>> {
    let params = new HttpParams();
    
    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }

    return this.http.get<ApiResponse<PositionDto[]>>(`${this.baseUrl}/positions`, { params });
  }

  /**
   * Get positions with caching for a specific company
   */
  getPositionsWithCache(companyCode: number): Observable<ApiResponse<PositionDto[]>> {
    const isInitialized = this.isPositionsCacheInitialized.get(companyCode) || false;
    let cache = this.positionsCache$.get(companyCode);
    
    if (!cache) {
      cache = new BehaviorSubject<PositionDto[]>([]);
      this.positionsCache$.set(companyCode, cache);
    }
    
    if (isInitialized && cache.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: cache!.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getPositions(companyCode).pipe(
        tap(response => {
          if (response.success && response.data) {
            cache!.next(response.data);
            this.isPositionsCacheInitialized.set(companyCode, true);
          }
        })
      );
    }
  }

  /**
   * Get cached positions for a company without making API call
   */
  getCachedPositions(companyCode: number): PositionDto[] {
    const cache = this.positionsCache$.get(companyCode);
    return cache ? cache.value : [];
  }

  /**
   * Clear positions cache for a specific company
   */
  clearPositionsCache(companyCode: number): void {
    const cache = this.positionsCache$.get(companyCode);
    if (cache) {
      cache.next([]);
    }
    this.isPositionsCacheInitialized.set(companyCode, false);
  }

  /**
   * Clear all positions caches
   */
  clearAllPositionsCache(): void {
    this.positionsCache$.forEach((cache, companyCode) => {
      cache.next([]);
      this.isPositionsCacheInitialized.set(companyCode, false);
    });
  }

  /**
   * Check if positions cache is initialized for a company
   */
  isPositionsCacheReady(companyCode: number): boolean {
    return this.isPositionsCacheInitialized.get(companyCode) || false;
  }

  /**
   * Get supervisors with optional company and payroll department filtering
   */
  getSupervisors(companyCode?: number, payrollDeptCode?: number): Observable<ApiResponse<SupervisorDto[]>> {
    let params = new HttpParams();
    
    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }
    
    if (payrollDeptCode !== undefined) {
      params = params.set('payrollDeptCode', payrollDeptCode.toString());
    }

    return this.http.get<ApiResponse<SupervisorDto[]>>(`${this.baseUrl}/supervisors`, { params });
  }

  /**
   * Get supervisors with caching for specific company and payroll department
   */
  getSupervisorsWithCache(companyCode: number, payrollDeptCode: number): Observable<ApiResponse<SupervisorDto[]>> {
    const cacheKey = `${companyCode}-${payrollDeptCode}`;
    const isInitialized = this.isSupervisorsCacheInitialized.get(cacheKey) || false;
    let cache = this.supervisorsCache$.get(cacheKey);
    
    if (!cache) {
      cache = new BehaviorSubject<SupervisorDto[]>([]);
      this.supervisorsCache$.set(cacheKey, cache);
    }
    
    if (isInitialized && cache.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: cache!.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getSupervisors(companyCode, payrollDeptCode).pipe(
        tap(response => {
          if (response.success && response.data) {
            cache!.next(response.data);
            this.isSupervisorsCacheInitialized.set(cacheKey, true);
          }
        })
      );
    }
  }

  /**
   * Get cached supervisors for a company and payroll department without making API call
   */
  getCachedSupervisors(companyCode: number, payrollDeptCode: number): SupervisorDto[] {
    const cacheKey = `${companyCode}-${payrollDeptCode}`;
    const cache = this.supervisorsCache$.get(cacheKey);
    return cache ? cache.value : [];
  }

  /**
   * Clear supervisors cache for a specific company and payroll department
   */
  clearSupervisorsCache(companyCode: number, payrollDeptCode: number): void {
    const cacheKey = `${companyCode}-${payrollDeptCode}`;
    const cache = this.supervisorsCache$.get(cacheKey);
    if (cache) {
      cache.next([]);
    }
    this.isSupervisorsCacheInitialized.set(cacheKey, false);
  }

  /**
   * Clear all supervisors caches for a specific company
   */
  clearAllSupervisorsCacheForCompany(companyCode: number): void {
    const keysToRemove: string[] = [];
    this.supervisorsCache$.forEach((cache, cacheKey) => {
      if (cacheKey.startsWith(`${companyCode}-`)) {
        cache.next([]);
        keysToRemove.push(cacheKey);
      }
    });
    keysToRemove.forEach(key => {
      this.isSupervisorsCacheInitialized.set(key, false);
    });
  }

  /**
   * Clear all supervisors caches
   */
  clearAllSupervisorsCache(): void {
    this.supervisorsCache$.forEach((cache, cacheKey) => {
      cache.next([]);
      this.isSupervisorsCacheInitialized.set(cacheKey, false);
    });
  }

  /**
   * Check if supervisors cache is initialized for a company and payroll department
   */
  isSupervisorsCacheReady(companyCode: number, payrollDeptCode: number): boolean {
    const cacheKey = `${companyCode}-${payrollDeptCode}`;
    return this.isSupervisorsCacheInitialized.get(cacheKey) || false;
  }

  /**
   * Get building access requirements with optional filtering
   */
  getBuildingAccessRequirements(
    companyCode?: number,
    description?: string,
    locationType?: string
  ): Observable<ApiResponse<BuildingAccessRequirementDto[]>> {
    let params = new HttpParams();
    
    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }
    
    if (description) {
      params = params.set('description', description);
    }
    
    if (locationType) {
      params = params.set('locationType', locationType);
    }

    return this.http.get<ApiResponse<BuildingAccessRequirementDto[]>>(`${this.baseUrl}/building-access-requirements`, { params });
  }

  /**
   * Get building access requirements with caching for a specific company
   */
  getBuildingAccessRequirementsWithCache(companyCode: number): Observable<ApiResponse<BuildingAccessRequirementDto[]>> {
    const isInitialized = this.isBuildingAccessRequirementsCacheInitialized.get(companyCode) || false;
    let cache = this.buildingAccessRequirementsCache$.get(companyCode);
    
    if (!cache) {
      cache = new BehaviorSubject<BuildingAccessRequirementDto[]>([]);
      this.buildingAccessRequirementsCache$.set(companyCode, cache);
    }
    
    if (isInitialized && cache.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: cache!.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getBuildingAccessRequirements(companyCode).pipe(
        tap(response => {
          if (response.success && response.data) {
            cache!.next(response.data);
            this.isBuildingAccessRequirementsCacheInitialized.set(companyCode, true);
          }
        })
      );
    }
  }

  /**
   * Get cached building access requirements for a company without making API call
   */
  getCachedBuildingAccessRequirements(companyCode: number): BuildingAccessRequirementDto[] {
    const cache = this.buildingAccessRequirementsCache$.get(companyCode);
    return cache ? cache.value : [];
  }

  /**
   * Clear building access requirements cache for a specific company
   */
  clearBuildingAccessRequirementsCache(companyCode: number): void {
    const cache = this.buildingAccessRequirementsCache$.get(companyCode);
    if (cache) {
      cache.next([]);
    }
    this.isBuildingAccessRequirementsCacheInitialized.set(companyCode, false);
  }

  /**
   * Clear all building access requirements caches
   */
  clearAllBuildingAccessRequirementsCache(): void {
    this.buildingAccessRequirementsCache$.forEach((cache, companyCode) => {
      cache.next([]);
      this.isBuildingAccessRequirementsCacheInitialized.set(companyCode, false);
    });
  }

  /**
   * Check if building access requirements cache is initialized for a company
   */
  isBuildingAccessRequirementsCacheReady(companyCode: number): boolean {
    return this.isBuildingAccessRequirementsCacheInitialized.get(companyCode) || false;
  }

  /**
   * Get tablet profiles with optional filtering
   */
  getTabletProfiles(
    companyCode?: number,
    locationType?: string,
    profileName?: string
  ): Observable<ApiResponse<TabletProfileDto[]>> {
    let params = new HttpParams();
    
    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }
    
    if (locationType) {
      params = params.set('locationType', locationType);
    }
    
    if (profileName) {
      params = params.set('profileName', profileName);
    }

    return this.http.get<ApiResponse<TabletProfileDto[]>>(`${this.baseUrl}/tablet-profiles`, { params });
  }

  /**
   * Get tablet profiles with caching for a specific company
   */
  getTabletProfilesWithCache(companyCode: number): Observable<ApiResponse<TabletProfileDto[]>> {
    const isInitialized = this.isTabletProfilesCacheInitialized.get(companyCode) || false;
    let cache = this.tabletProfilesCache$.get(companyCode);
    
    if (!cache) {
      cache = new BehaviorSubject<TabletProfileDto[]>([]);
      this.tabletProfilesCache$.set(companyCode, cache);
    }
    
    if (isInitialized && cache.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: cache!.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getTabletProfiles(companyCode).pipe(
        tap(response => {
          if (response.success && response.data) {
            cache!.next(response.data);
            this.isTabletProfilesCacheInitialized.set(companyCode, true);
          }
        })
      );
    }
  }

  /**
   * Get cached tablet profiles for a company without making API call
   */
  getCachedTabletProfiles(companyCode: number): TabletProfileDto[] {
    const cache = this.tabletProfilesCache$.get(companyCode);
    return cache ? cache.value : [];
  }

  /**
   * Clear tablet profiles cache for a specific company
   */
  clearTabletProfilesCache(companyCode: number): void {
    const cache = this.tabletProfilesCache$.get(companyCode);
    if (cache) {
      cache.next([]);
    }
    this.isTabletProfilesCacheInitialized.set(companyCode, false);
  }

  /**
   * Clear all tablet profiles caches
   */
  clearAllTabletProfilesCache(): void {
    this.tabletProfilesCache$.forEach((cache, companyCode) => {
      cache.next([]);
      this.isTabletProfilesCacheInitialized.set(companyCode, false);
    });
  }

  /**
   * Check if tablet profiles cache is initialized for a company
   */
  isTabletProfilesCacheReady(companyCode: number): boolean {
    return this.isTabletProfilesCacheInitialized.get(companyCode) || false;
  }

  /**
   * Get applications with optional filtering
   */
  getApplications(
    companyCode?: number,
    name?: string,
    locationType?: string
  ): Observable<ApiResponse<ApplicationDto[]>> {
    let params = new HttpParams();
    
    if (companyCode !== undefined) {
      params = params.set('companyCode', companyCode.toString());
    }
    
    if (name) {
      params = params.set('name', name);
    }
    
    if (locationType) {
      params = params.set('locationType', locationType);
    }

    return this.http.get<ApiResponse<ApplicationDto[]>>(`${this.baseUrl}/applications`, { params });
  }

  /**
   * Get applications with caching for a specific company
   */
  getApplicationsWithCache(companyCode: number): Observable<ApiResponse<ApplicationDto[]>> {
    const isInitialized = this.isApplicationsCacheInitialized.get(companyCode) || false;
    let cache = this.applicationsCache$.get(companyCode);
    
    if (!cache) {
      cache = new BehaviorSubject<ApplicationDto[]>([]);
      this.applicationsCache$.set(companyCode, cache);
    }
    
    if (isInitialized && cache.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: cache!.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getApplications(companyCode).pipe(
        tap(response => {
          if (response.success && response.data) {
            cache!.next(response.data);
            this.isApplicationsCacheInitialized.set(companyCode, true);
          }
        })
      );
    }
  }

  /**
   * Get cached applications for a company without making API call
   */
  getCachedApplications(companyCode: number): ApplicationDto[] {
    const cache = this.applicationsCache$.get(companyCode);
    return cache ? cache.value : [];
  }

  /**
   * Clear applications cache for a specific company
   */
  clearApplicationsCache(companyCode: number): void {
    const cache = this.applicationsCache$.get(companyCode);
    if (cache) {
      cache.next([]);
    }
    this.isApplicationsCacheInitialized.set(companyCode, false);
  }

  /**
   * Clear all applications caches
   */
  clearAllApplicationsCache(): void {
    this.applicationsCache$.forEach((cache, companyCode) => {
      cache.next([]);
      this.isApplicationsCacheInitialized.set(companyCode, false);
    });
  }

  /**
   * Check if applications cache is initialized for a company
   */
  isApplicationsCacheReady(companyCode: number): boolean {
    return this.isApplicationsCacheInitialized.get(companyCode) || false;
  }

  /**
   * Get employee license classes with optional filtering
   */
  getEmployeeLicenseClasses(
    id?: number,
    licenseClass?: string,
    isUnion?: boolean
  ): Observable<ApiResponse<EmployeeLicenseClassDto[]>> {
    let params = new HttpParams();
    
    if (id !== undefined) {
      params = params.set('id', id.toString());
    }
    
    if (licenseClass) {
      params = params.set('licenseClass', licenseClass);
    }
    
    if (isUnion !== undefined) {
      params = params.set('isUnion', isUnion.toString());
    }

    return this.http.get<ApiResponse<EmployeeLicenseClassDto[]>>(`${this.baseUrl}/employee-license-classes`, { params });
  }

  /**
   * Get employee license classes with caching
   */
  getEmployeeLicenseClassesWithCache(): Observable<ApiResponse<EmployeeLicenseClassDto[]>> {
    if (this.isEmployeeLicenseClassesCacheInitialized && this.employeeLicenseClassesCache$.value.length > 0) {
      // Return cached data
      return new Observable(observer => {
        observer.next({
          success: true,
          data: this.employeeLicenseClassesCache$.value,
          errors: []
        });
        observer.complete();
      });
    } else {
      // Load from API and cache
      return this.getEmployeeLicenseClasses().pipe(
        tap(response => {
          if (response.success && response.data) {
            this.employeeLicenseClassesCache$.next(response.data);
            this.isEmployeeLicenseClassesCacheInitialized = true;
          }
        })
      );
    }
  }

  /**
   * Get cached employee license classes without making API call
   */
  getCachedEmployeeLicenseClasses(): EmployeeLicenseClassDto[] {
    return this.employeeLicenseClassesCache$.value;
  }

  /**
   * Clear the employee license classes cache
   */
  clearEmployeeLicenseClassesCache(): void {
    this.employeeLicenseClassesCache$.next([]);
    this.isEmployeeLicenseClassesCacheInitialized = false;
  }

  /**
   * Get computer requirements with optional filtering
   */
  getComputerRequirements(
    id?: number,
    isChild?: boolean,
    parentId?: number,
    description?: string
  ): Observable<ApiResponse<ComputerRequirementDto[]>> {
    let params = new HttpParams();
    
    if (id !== undefined) {
      params = params.set('id', id.toString());
    }
    
    if (isChild !== undefined) {
      params = params.set('isChild', isChild.toString());
    }
    
    if (parentId !== undefined) {
      params = params.set('parentId', parentId.toString());
    }
    
    if (description) {
      params = params.set('description', description);
    }

    return this.http.get<ApiResponse<ComputerRequirementDto[]>>(`${this.baseUrl}/computer-requirements`, { params });
  }

  /**
   * Generate a unique username for AD creation based on preferred first name or first name
   * Format: [name]001, [name]002, etc.
   */
  generateUsername(firstName: string, preferredFirstName?: string): Observable<ApiResponse<string>> {
    let params = new HttpParams().set('firstName', firstName);

    if (preferredFirstName) {
      params = params.set('preferredFirstName', preferredFirstName);
    }

    return this.http.get<ApiResponse<string>>(`${this.baseUrl}/generate-username`, { params });
  }

  /**
   * Generate an email address based on employee name and payroll department's email domain.
   * If emailRequired: firstname.lastname@domain (or preferred.lastname@domain)
   * If not emailRequired: firstname001@domain (or preferred001@domain)
   */
  generateEmailAddress(firstName: string, lastName: string, companyCode: number, payrollDeptCode: number, emailRequired: boolean, preferredFirstName?: string, userId?: string): Observable<ApiResponse<string>> {
    let params = new HttpParams()
      .set('firstName', firstName)
      .set('lastName', lastName)
      .set('companyCode', companyCode.toString())
      .set('payrollDeptCode', payrollDeptCode.toString())
      .set('emailRequired', emailRequired.toString());

    if (preferredFirstName) {
      params = params.set('preferredFirstName', preferredFirstName);
    }

    if (userId) {
      params = params.set('userId', userId);
    }

    return this.http.get<ApiResponse<string>>(`${this.baseUrl}/generate-email`, { params });
  }

  /**
   * Check if employee license classes cache is initialized
   */
  isEmployeeLicenseClassesCacheReady(): boolean {
    return this.isEmployeeLicenseClassesCacheInitialized;
  }
}