import { ApiHRRequestDto } from './api-hr-request.model';

export interface HRRequest {
  id: string;
  type: RequestType;
  employeeName: string;
  effectiveDate: string;
  status: RequestStatus;
  submittedBy: string;
  submittedDate?: string;
  notes?: string;
  details?: HRRequestDetail[];
}

export interface HRRequestDetail {
  id: number;
  requestTypeName?: string;
  requestStatusName?: string;
  employeeName?: string;
  effectiveDate?: string;
  processingNotes?: string;
}

export type RequestType = 
  | 'new-hire' 
  | 'promotion' 
  | 'transfer' 
  | 'termination' 
  | 'layoff' 
  | 'return';

export type RequestStatus = 
  | 'draft' 
  | 'submitted' 
  | 'approved' 
  | 'rejected';

export interface RequestTypeOption {
  type: RequestType;
  title: string;
  description: string;
  icon: string;
  route: string;
}

// Utility functions to transform API data to component models
export function transformApiHRRequestToHRRequest(apiRequest: ApiHRRequestDto): HRRequest {
  // Get the primary detail (first one) to extract main request info
  const primaryDetail = apiRequest.details[0];
  
  return {
    id: apiRequest.id.toString(),
    type: mapRequestTypeNameToType(primaryDetail?.requestTypeName),
    employeeName: primaryDetail?.employeeName || 'Unknown Employee',
    effectiveDate: primaryDetail?.effectiveDate ? formatDate(primaryDetail.effectiveDate) : 'Not specified',
    status: mapRequestStatusNameToStatus(primaryDetail?.requestStatusName),
    submittedBy: apiRequest.submittedByName || 'Unknown',
    submittedDate: apiRequest.submittedDate ? formatDate(apiRequest.submittedDate) : undefined,
    notes: apiRequest.notes,
    details: apiRequest.details.map(detail => ({
      id: detail.id,
      requestTypeName: detail.requestTypeName,
      requestStatusName: detail.requestStatusName,
      employeeName: detail.employeeName,
      effectiveDate: detail.effectiveDate,
      processingNotes: detail.processingNotes
    }))
  };
}

function mapRequestTypeNameToType(typeName?: string): RequestType {
  if (!typeName) return 'promotion';
  
  const lowerTypeName = typeName.toLowerCase();
  
  if (lowerTypeName.includes('promotion')) return 'promotion';
  if (lowerTypeName.includes('layoff')) return 'layoff';
  if (lowerTypeName.includes('termination')) return 'termination';
  if (lowerTypeName.includes('return')) return 'return';
  if (lowerTypeName.includes('transfer')) return 'transfer';
  if (lowerTypeName.includes('hire')) return 'new-hire';
  
  return 'promotion'; // default
}

function mapRequestStatusNameToStatus(statusName?: string): RequestStatus {
  if (!statusName) return 'draft';
  
  const lowerStatusName = statusName.toLowerCase();
  
  if (lowerStatusName.includes('submit')) return 'submitted';
  if (lowerStatusName.includes('approv')) return 'approved';
  if (lowerStatusName.includes('reject')) return 'rejected';
  if (lowerStatusName.includes('draft')) return 'draft';
  
  return 'draft'; // default
}

function formatDate(dateString: string): string {
  try {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'long', 
      day: 'numeric' 
    });
  } catch {
    return dateString;
  }
}