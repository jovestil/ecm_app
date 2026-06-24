import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface HRRequestNotification {
  type: 'HRRequestStatusUpdate' | 'HRRequestCompletion';
  hrRequestDetailId: number;
  status?: string;
  displayStatusName?: string;
  employeeName: string;
  isSuccess?: boolean;
  message?: string;
  timestamp: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: HubConnection | null = null;
  private connectionState = new BehaviorSubject<'Connected' | 'Disconnected' | 'Connecting'>('Disconnected');
  private notifications = new Subject<HRRequestNotification>(); // Changed from BehaviorSubject to Subject

  constructor() {}

  /**
   * Start the SignalR connection
   */
  public async startConnection(accessToken: string): Promise<void> {
    if (this.hubConnection?.state === 'Connected') {
      return;
    }

    this.connectionState.next('Connecting');

    // Remove '/api/v1' from the URL for SignalR hub connection
    const baseUrl = environment.apiUrl.replace('/api/v1', '');
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/hr-request-status`)
      .withAutomaticReconnect([0, 1000, 5000, 10000, 30000]) // Retry pattern
      .configureLogging(LogLevel.Debug)
      .build();

    // Handle connection events
    this.hubConnection.onreconnecting(() => {
      this.connectionState.next('Connecting');
    });

    this.hubConnection.onreconnected(() => {
      this.connectionState.next('Connected');
    });

    this.hubConnection.onclose(() => {
      this.connectionState.next('Disconnected');
    });

    // Register notification handlers
    this.hubConnection.on('HRRequestStatusUpdate', (...args: any[]) => {
      console.log('🔥 RAW SignalR HRRequestStatusUpdate received with args:', args);
      console.log('🔢 Number of arguments:', args.length);
      
      if (args.length > 0) {
        const notification = args[0];
        console.log('🔍 First arg type:', typeof notification);
        console.log('🔑 First arg keys:', notification ? Object.keys(notification) : 'null');
        console.log('📝 First arg values:', notification ? Object.values(notification) : 'null');
        console.log('🧪 First arg JSON:', JSON.stringify(notification, null, 2));
        
        // Try all arguments
        args.forEach((arg, index) => {
          console.log(`📦 Arg ${index}:`, arg);
        });
        
        this.notifications.next(notification);
      }
    });

    this.hubConnection.on('HRRequestCompletion', (...args: any[]) => {
      console.log('🔥 RAW SignalR HRRequestCompletion received with args:', args);
      console.log('🔢 Number of arguments:', args.length);
      
      if (args.length > 0) {
        const notification = args[0];
        console.log('🔍 First arg type:', typeof notification);
        console.log('🔑 First arg keys:', notification ? Object.keys(notification) : 'null');
        console.log('📝 First arg values:', notification ? Object.values(notification) : 'null');
        console.log('🧪 First arg JSON:', JSON.stringify(notification, null, 2));
        
        // Try all arguments
        args.forEach((arg, index) => {
          console.log(`📦 Arg ${index}:`, arg);
        });
        
        this.notifications.next(notification);
      }
    });

    try {
      console.log(`Attempting to connect to SignalR hub at: ${baseUrl}/hubs/hr-request-status`);
      await this.hubConnection.start();
      this.connectionState.next('Connected');
      console.log('SignalR connection established successfully');
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      console.error('Connection URL was:', `${baseUrl}/hubs/hr-request-status`);
      this.connectionState.next('Disconnected');
      throw error;
    }
  }

  /**
   * Stop the SignalR connection
   */
  public async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
      this.connectionState.next('Disconnected');
      console.log('SignalR connection stopped');
    }
  }

  /**
   * Join a user group to receive notifications
   */
  public async joinUserGroup(userId: string): Promise<void> {
    if (this.hubConnection?.state === 'Connected') {
      try {
        await this.hubConnection.invoke('JoinUserGroup', userId);
        console.log(`Joined user group: ${userId}`);
      } catch (error) {
        console.error('Error joining user group:', error);
      }
    }
  }

  /**
   * Leave a user group
   */
  public async leaveUserGroup(userId: string): Promise<void> {
    if (this.hubConnection?.state === 'Connected') {
      try {
        await this.hubConnection.invoke('LeaveUserGroup', userId);
        console.log(`Left user group: ${userId}`);
      } catch (error) {
        console.error('Error leaving user group:', error);
      }
    }
  }

  /**
   * Get connection state observable
   */
  public getConnectionState(): Observable<string> {
    return this.connectionState.asObservable();
  }

  /**
   * Get notifications observable
   */
  public getNotifications(): Observable<HRRequestNotification> {
    return this.notifications.asObservable();
  }

  /**
   * Check if connected
   */
  public isConnected(): boolean {
    return this.hubConnection?.state === 'Connected';
  }

  /**
   * Clear all pending notifications (useful when navigating away)
   */
  public clearNotifications(): void {
    // Since we're using Subject, there's no stored state to clear
    // but we can add logging for debugging
    console.log('SignalR notifications cleared');
  }
}