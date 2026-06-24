import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface TestSignalRStatusUpdateRequest {
  userId: string;
  hrRequestDetailId: number;
  status: string;
  employeeName: string;
  message: string;
}

export interface TestSignalRCompletionRequest {
  userId: string;
  hrRequestDetailId: number;
  employeeName: string;
  isSuccess: boolean;
  message: string;
}

export interface TestSignalRResponse {
  success: boolean;
  message: string;
  data?: any;
  error?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TestService {
  private readonly baseUrl = `${environment.apiUrl}/test`;

  constructor(private http: HttpClient) {}

  /**
   * Test SignalR status update notification
   */
  testSignalRStatusUpdate(request: TestSignalRStatusUpdateRequest): Observable<TestSignalRResponse> {
    return this.http.post<TestSignalRResponse>(`${this.baseUrl}/signalr/status-update`, request);
  }

  /**
   * Test SignalR completion notification
   */
  testSignalRCompletion(request: TestSignalRCompletionRequest): Observable<TestSignalRResponse> {
    return this.http.post<TestSignalRResponse>(`${this.baseUrl}/signalr/completion`, request);
  }
}