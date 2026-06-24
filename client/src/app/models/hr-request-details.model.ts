export interface HRRequestDetails {
  requestDetailId?: number;
  parentRequestId: number;
  requestTypeId: number;
  requestStatusId: number;
  employeeId: number;
  employeeNetworkId?: string;
  employeePositionCode?: string;
  employeeCompanyCode?: number;
  employeeDepartmentCode?: string;
  effectiveDate?: Date;
  processingNotes?: string;
  viewpointProcessed: boolean;
  viewpointProcessedDate?: Date;
  viewpointErrorMessage?: string;
  createdBy: number;
  createdDate: Date;
  modifiedBy?: number;
  modifiedDate?: Date;
  isDeleted: boolean;
}