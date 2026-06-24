export interface ReturnToWorkRequestDetails {
  returnToWorkDetailId?: number;
  requestDetailId: number;
  createdBy: number;
  createdDate: Date;
  modifiedBy?: number;
  modifiedDate?: Date;
  isDeleted: boolean;
}