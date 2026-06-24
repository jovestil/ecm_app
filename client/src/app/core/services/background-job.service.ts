import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ScheduleViewpointStatusUpdateRequest {
  effectiveDate: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
}

export interface HRRequestDetail {
  id?: number;
  requestStatusName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BackgroundJobService {
  private readonly baseUrl = `${environment.apiUrl}/backgroundjobs`;

  constructor(private http: HttpClient) {}

  /**
   * Check if a request status is completed
   */
  private isRequestCompleted(requestStatusName?: string): boolean {
    if (!requestStatusName) return false;
    
    const completedStatuses = ['Completed', 'Cancelled', 'Failed'];
    return completedStatuses.some(status => 
      requestStatusName.toLowerCase().includes(status.toLowerCase())
    );
  }

  /**
   * Schedule a Viewpoint status update job for an HR request detail (only for non-completed requests)
   */
  scheduleViewpointStatusUpdate(hrRequestDetailId: number, effectiveDate: Date, requestStatusName?: string): Observable<ApiResponse<string>> {
    // Validate that the request is not completed
    if (this.isRequestCompleted(requestStatusName)) {
      throw new Error(`Cannot schedule Viewpoint status update for completed request with status: ${requestStatusName}`);
    }

    const request: ScheduleViewpointStatusUpdateRequest = {
      effectiveDate: effectiveDate.toISOString()
    };
    
    return this.http.post<ApiResponse<string>>(
      `${this.baseUrl}/viewpoint-status-update/${hrRequestDetailId}`,
      request
    );
  }
}