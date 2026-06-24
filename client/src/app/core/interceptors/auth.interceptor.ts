import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, from, throwError } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { AuthService } from '../services/auth.service';
import { environment } from '../../../environments/environment';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(
    private msalService: MsalService,
    private router: Router,
    private authService: AuthService
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Only add token for API requests to our backend
    if (req.url.startsWith(environment.apiUrl)) {
      return from(this.getAccessTokenAndValidatedRoles()).pipe(
        switchMap(({ token, validatedRoles, validatedRole }) => {
          if (token && !token.includes('Error') && !token.includes('Unable')) {
            // Prepare headers
            const headers: { [name: string]: string } = {
              Authorization: `Bearer ${token}`
            };

            // Add selected role header - prioritize multi-role selection, fallback to single role
            // Only use validated roles that the user currently has
            if (validatedRoles.length > 0) {
              headers['X-Selected-Role'] = validatedRoles.join(',');
            } else if (validatedRole) {
              headers['X-Selected-Role'] = validatedRole;
            }

            const authReq = req.clone({
              setHeaders: headers
            });
            return next.handle(authReq);
          }
          // If no valid token, proceed without Authorization header
          return next.handle(req);
        }),
        catchError((error) => {
          console.warn('Failed to get access token in interceptor:', error);
          return next.handle(req);
        })
      ).pipe(
        catchError((error: HttpErrorResponse) => {
          // Handle 401 Unauthorized responses
          if (error.status === 401) {
            console.warn('Received 401 Unauthorized, redirecting to logout');
            this.router.navigate(['/logout']);
          }
          return throwError(() => error);
        })
      );
    }

    return next.handle(req);
  }

  /**
   * Get access token and validated selected roles
   * This ensures that selected roles are validated against the user's current roles from Azure AD
   */
  private async getAccessTokenAndValidatedRoles(): Promise<{
    token: string;
    validatedRoles: string[];
    validatedRole: string | null;
  }> {
    const token = await this.getAccessToken();

    // Get validated roles - these are filtered against the user's current Azure AD roles
    const validatedRoles = await this.authService.getValidatedSelectedRoles();
    const validatedRole = await this.authService.getValidatedSelectedRole();

    return { token, validatedRoles, validatedRole };
  }

  /**
   * Get access token from localStorage first, then fallback to MSAL if needed
   */
  private async getAccessToken(): Promise<string> {
    // First check localStorage for cached token
    const cachedToken = this.getTokenFromLocalStorage();
    if (cachedToken && !this.isTokenExpired()) {
      console.log('Using cached access token from localStorage (interceptor)');
      return cachedToken;
    }

    // If no cached token or expired, get fresh token from MSAL
    try {
      const account = this.msalService.instance.getActiveAccount();

      if (!account) {
        return 'No active account found';
      }

      const tokenResponse = await this.msalService.instance.acquireTokenSilent({
        scopes: environment.msal.scopes,
        account: account
      });

      // Cache the new token
      this.saveTokenToLocalStorage(tokenResponse.accessToken);
      console.log('Retrieved and cached fresh access token (interceptor)');
      return tokenResponse.accessToken;
    } catch (error) {
      console.error('❌ Token acquisition failed in interceptor:', error);
      return 'Unable to retrieve access token';
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
