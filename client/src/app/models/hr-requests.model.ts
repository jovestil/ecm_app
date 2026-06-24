export interface HRRequests {
  requestId?: number;
  submittedBy: number;
  submittedDate?: Date;
  notes?: string;
  createdBy: number;
  createdDate: Date;
  modifiedBy?: number;
  modifiedDate?: Date;
  isDeleted: boolean;
}