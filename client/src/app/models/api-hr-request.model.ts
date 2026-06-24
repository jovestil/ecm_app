// API models that match the .NET backend DTOs
export interface ApiHRRequestDto {
  id: number;
  submittedBy: number;
  submittedByName?: string;
  submittedDate?: string;
  notes?: string;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number;
  modifiedDate?: string;
  isDeleted: boolean;
  details: ApiHRRequestDetailDto[];
}

export interface ApiHRRequestDetailDto {
  id: number;
  parentRequestId: number;
  requestTypeId: number;
  requestTypeName?: string;
  requestStatusId: number;
  requestStatusName?: string;
  requestDisplayStatusName?: string;
  employeeId: number;
  employeeNetworkId?: string;
  employeePositonCode?: string;
  employeeCompanyCode?: number;
  employeeDepartmentCode?: string;
  employeeName?: string;
  companyName?: string;
  departmentName?: string;
  effectiveDate?: string;
  processingNotes?: string;
  submittedBy: number;
  submittedByName?: string;
  submittedDate?: string;
  viewpointProcessed: boolean;
  viewpointProcessedDate?: string;
  viewpointErrorMessage?: string;
  hasDeskPhone?: boolean;
}

export interface CreateHRRequestDto {
  notes?: string;
  details: CreateHRRequestDetailDto[];
}

export interface CreateHRRequestDetailDto {
  requestTypeId: number;
  employeeId: number;
  employeeNetworkId?: string;
  employeePositonCode?: string;
  employeeCompanyCode?: number;
  employeeDepartmentCode?: string;
  effectiveDate?: string;
  processingNotes?: string;
}

export interface CreateMultiEmployeeHRRequestDto {
  requestTypeId: number;
  employeeIds: number[];
  effectiveDate?: string;
  processingNotes: string;
  notes: string;
  requestTitle: string;
  requestDescription: string;
  requestedBy: number;
  companyId?: number;
  payrollGroupId?: number;
}

export interface CreateSingleEmployeeHRRequestDto {
  requestTypeId: number;
  employeeId: number;
  effectiveDate?: string;
  processingNotes: string;
  notes: string;
  requestTitle: string;
  requestDescription: string;
  requestedBy: number;
  companyId?: number;
  payrollGroupId?: number;
  terminationDetails?: CreateTerminationRequestDto;
}

export interface CreateTerminationRequestDto {
  reasonCode: string;
  forwardEmail?: string;
  forwardDeskPhone?: string;
  forwardCellPhone?: string;
  autoReply?: string;
  giveOneDriveAccessTo?: string;
  withKwikTripCard: boolean;
  kwikCard4DigitNo?: string;
}

export interface UpdateHRRequestDto {
  id: number;
  notes?: string;
}

export interface UpdateHRRequestDetailDto {
  id: number;
  requestStatusId: number;
  effectiveDate?: string;
  processingNotes?: string;
  viewpointProcessed: boolean;
  viewpointErrorMessage?: string;
}

// API Response wrapper interfaces
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors: string[];
}

export interface PagedResponse<T> extends ApiResponse<T> {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  orderBy?: string;
  orderByDesc: boolean;
}