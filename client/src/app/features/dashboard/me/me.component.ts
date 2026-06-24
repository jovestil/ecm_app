import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MsalService } from '@azure/msal-angular';
import { AuthService } from '../../../core/services/auth.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-me',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './me.component.html',
  styleUrls: ['./me.component.css']
})
export class MeComponent implements OnInit {
  userName: string = '';
  userEmail: string = '';
  accessToken: string = '';
  isAuthenticated = false;
  employeeData: any = null;
  isLoadingEmployee = false;
  employeeError: string = '';

  constructor(
    private router: Router,
    private authService: AuthService,
    private msalService: MsalService,
    private http: HttpClient,
    private toasterService: ToasterService
  ) {}

  ngOnInit(): void {
    console.log('🚀 Me Component initialized');
    console.log('🔍 Environment:', environment);
    console.log('🔍 API URL:', environment.apiUrl);
    this.loadUserInfo();
    this.loadEmployeeData();
  }

  private async loadUserInfo(): Promise<void> {
    console.log('🔍 Loading user info...');
    this.isAuthenticated = this.authService.isAuthenticated();
    console.log('🔍 Is authenticated:', this.isAuthenticated);
    
    if (this.isAuthenticated) {
      try {
        // Initialize MSAL first
        await this.authService.initialize();
        
        // Get user name and email
        this.userName = this.authService.getUserDisplayName();
        this.userEmail = this.authService.getUserEmail();

        // Get access token for display purposes
        this.accessToken = await this.getAccessTokenForDisplay();
      } catch (error) {
        console.error('Error loading user info:', error);
        this.userName = 'Unknown';
        this.userEmail = 'Unknown';
        this.accessToken = 'Error retrieving token';
      }
    }
  }


  goBack(): void {
    this.router.navigate(['/']);
  }

  logout(): void {
    if (confirm('Are you sure you want to log out?')) {
      this.router.navigate(['/logout']);
    }
  }

  copyToClipboard(text: string, label: string): void {
    if (navigator.clipboard && window.isSecureContext) {
      navigator.clipboard.writeText(text).then(() => {
        this.toasterService.showSuccess(`${label} copied to clipboard!`, 'Copied', 1500);
      }).catch(err => {
        console.error('Failed to copy to clipboard:', err);
        this.fallbackCopyToClipboard(text, label);
      });
    } else {
      this.fallbackCopyToClipboard(text, label);
    }
  }

  private fallbackCopyToClipboard(text: string, label: string): void {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    textArea.style.top = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    
    try {
      const successful = document.execCommand('copy');
      if (successful) {
        this.toasterService.showSuccess(`${label} copied to clipboard!`, 'Copied', 1500);
      } else {
        this.toasterService.showError('Failed to copy to clipboard', 'Copy Error');
      }
    } catch (err) {
      console.error('Fallback copy failed:', err);
      this.toasterService.showError('Failed to copy to clipboard', 'Copy Error');
    }
    
    document.body.removeChild(textArea);
  }

  private async loadEmployeeData(): Promise<void> {
    if (!this.isAuthenticated) {
      return;
    }

    this.isLoadingEmployee = true;
    this.employeeError = '';

    try {
      // The interceptor will automatically add the Bearer token
      const response = await this.http.get(`${environment.apiUrl}api/v1/employees/viewpoint/email`).toPromise() as any;

      if (response.success) {
        this.employeeData = response.data;
      } else {
        this.employeeError = response.message || 'Failed to load employee data';
      }
    } catch (error) {
      console.error('Error loading employee data:', error);
      this.employeeError = 'Failed to load employee data from Viewpoint';
    } finally {
      this.isLoadingEmployee = false;
    }
  }

  /**
   * Get access token for display purposes (UI only)
   */
  private async getAccessTokenForDisplay(): Promise<string> {
    // First check localStorage for cached token
    const cachedToken = this.getTokenFromLocalStorage();
    if (cachedToken && !this.isTokenExpired()) {
      console.log('Using cached access token from localStorage (display)');
      return cachedToken;
    }

    // If no cached token or expired, get fresh token from MSAL
    try {
      await this.authService.initialize();
      const account = this.msalService.instance.getActiveAccount();
      
      if (!account) {
        return 'No active account found';
      }

      const tokenResponse = await this.msalService.instance.acquireTokenSilent({
        scopes: environment.msal.scopes,
        account: account
      });

      // Cache the new token (this will also be used by the interceptor)
      this.saveTokenToLocalStorage(tokenResponse.accessToken);
      console.log('Retrieved and cached fresh access token (display)');
      return tokenResponse.accessToken;
    } catch (error) {
      console.error('❌ Token acquisition failed:', error);
      return 'Unable to retrieve access token - please re-authenticate';
    }
  }

  /**
   * Get access token from localStorage
   */
  private getTokenFromLocalStorage(): string | null {
    try {
      const tokenData = localStorage.getItem('accessToken');
      if (tokenData) {
        const parsed = JSON.parse(tokenData);
        return parsed.token;
      }
    } catch (error) {
      console.warn('Error reading access token from localStorage:', error);
    }
    return null;
  }

  /**
   * Save access token to localStorage with timestamp
   */
  private saveTokenToLocalStorage(token: string): void {
    try {
      const tokenData = {
        token: token,
        timestamp: Date.now()
      };
      localStorage.setItem('accessToken', JSON.stringify(tokenData));
    } catch (error) {
      console.warn('Error saving access token to localStorage:', error);
    }
  }

  /**
   * Check if token is expired (tokens typically expire after 1 hour)
   */
  private isTokenExpired(): boolean {
    try {
      const tokenData = localStorage.getItem('accessToken');
      if (tokenData) {
        const parsed = JSON.parse(tokenData);
        const tokenAge = Date.now() - parsed.timestamp;
        // Consider token expired after 50 minutes (3000000 ms) to be safe
        return tokenAge > 3000000;
      }
    } catch (error) {
      console.warn('Error checking token expiration:', error);
    }
    return true; // If we can't determine, consider it expired
  }
}