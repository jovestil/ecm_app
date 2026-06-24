import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { AuthenticationResult, EventMessage, EventType, InteractionStatus } from '@azure/msal-browser';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly _destroying$ = new Subject<void>();

  constructor(
    private msalService: MsalService,
    private msalBroadcastService: MsalBroadcastService
  ) {
    // Initialize MSAL service
    this.msalBroadcastService.inProgress$
      .pipe(
        filter((status: InteractionStatus) => status === InteractionStatus.None),
        takeUntil(this._destroying$)
      )
      .subscribe(() => {
        this.checkAndSetActiveAccount();
      });
  }

  /**
   * Initialize MSAL service
   */
  async initialize(): Promise<void> {
    try {
      await this.msalService.instance.initialize();
    } catch (error) {
      console.error('Error initializing MSAL:', error);
      throw error;
    }
  }

  /**
   * Checks if there are any active accounts and sets the first one as active
   */
  checkAndSetActiveAccount(): void {
    const activeAccount = this.msalService.instance.getActiveAccount();

    if (!activeAccount && this.msalService.instance.getAllAccounts().length > 0) {
      const accounts = this.msalService.instance.getAllAccounts();
      this.msalService.instance.setActiveAccount(accounts[0]);
    }
  }

  /**
   * Login using redirect
   */
  loginRedirect(): void {
    this.msalService.loginRedirect({
      scopes: environment.msal.scopes
    });
  }

  /**
   * Login using popup
   */
  loginPopup(): Observable<AuthenticationResult> {
    return this.msalService.loginPopup({
      scopes: environment.msal.scopes
    });
  }

  /**
   * Logout
   */
  logout(): void {
    try {
      // Clear user roles, access token, and selected role(s) from localStorage
      this.clearRolesFromLocalStorage();
      this.clearAccessTokenFromLocalStorage();
      this.clearSelectedRoleFromLocalStorage();
      this.clearSelectedRolesFromLocalStorage();
      
      // Check if MSAL instance is initialized
      if (this.msalService.instance) {
        this.msalService.logoutRedirect({
          postLogoutRedirectUri: '/'
        });
      } else {
        console.warn('MSAL instance not initialized, redirecting to root');
        window.location.href = '/';
      }
    } catch (error) {
      console.error('Error during logout:', error);
      // Fallback to manual redirect
      window.location.href = '/';
    }
  }

  /**
   * Get access token silently
   */
  getAccessToken(): Observable<AuthenticationResult> {
    return this.msalService.acquireTokenSilent({
      scopes: environment.msal.scopes,
      account: this.msalService.instance.getActiveAccount()!
    });
  }

  /**
   * Get access token as Promise (for SignalR)
   */
  async getAccessTokenPromise(): Promise<string | null> {
    try {
      const result = await this.msalService.acquireTokenSilent({
        scopes: environment.msal.scopes,
        account: this.msalService.instance.getActiveAccount()!
      }).toPromise();
      
      return result?.accessToken || null;
    } catch (error) {
      console.error('Error getting access token:', error);
      return null;
    }
  }

  /**
   * Get current user ID
   */
  getCurrentUserId(): string | null {
    const account = this.msalService.instance.getActiveAccount();
    return account?.localAccountId || account?.homeAccountId || null;
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return this.msalService.instance.getActiveAccount() !== null;
  }

  /**
   * Get current user info
   */
  getCurrentUser() {
    return this.msalService.instance.getActiveAccount();
  }

  /**
   * Get user's display name
   */
  getUserDisplayName(): string {
    const account = this.msalService.instance.getActiveAccount();
    return account?.name || account?.username || 'User';
  }

  /**
   * Get user's email
   */
  getUserEmail(): string {
    const account = this.msalService.instance.getActiveAccount();
    return account?.username || '';
  }

  /**
   * Get user's roles from localStorage or access token claims
   * @param forceRefresh If true, bypasses localStorage cache and fetches fresh roles from token
   */
  async getUserRoles(forceRefresh: boolean = false): Promise<string[]> {
    // First check localStorage for cached roles (unless force refresh is requested)
    if (!forceRefresh) {
      const cachedRoles = this.getRolesFromLocalStorage();
      if (cachedRoles.length > 0) {
        return cachedRoles;
      }
    }

    // Fetch fresh roles from token
    const account = this.msalService.instance.getActiveAccount();
    if (!account) {
      return [];
    }

    try {
      // Try to get access token silently - force refresh if requested
      const request = {
        scopes: environment.msal.scopes,
        account: account,
        forceRefresh: forceRefresh
      };

      // Use acquireTokenSilent to get access token
      var response = await this.msalService.instance.acquireTokenSilent(request);
      // Check if response is synchronous (from cache)
      if (response && 'accessToken' in response) {
        const accessToken = response.accessToken;
        if (typeof accessToken === 'string') {
          const tokenPayload = this.decodeJWT(accessToken);
          const roles = tokenPayload.roles || tokenPayload.role || tokenPayload['extension_Role'] || [];

          let userRoles: string[] = [];
          // Ensure we return an array
          if (Array.isArray(roles)) {
            userRoles = roles;
          } else if (typeof roles === 'string') {
            userRoles = [roles];
          }

          // Cache the roles in localStorage
          this.saveRolesToLocalStorage(userRoles);

          // Validate and clean up selected roles against fresh roles
          if (forceRefresh) {
            this.validateAndCleanSelectedRoles(userRoles);
          }

          return userRoles;
        }
      }
    } catch (error) {
      console.warn('Access token not available in cache, falling back to ID token');
    }

    return [];
  }

  /**
   * Force refresh user roles from Azure AD token
   * This clears cached roles and fetches fresh ones from the token
   */
  async refreshUserRoles(): Promise<string[]> {
    console.log('Force refreshing user roles from Azure AD...');
    this.clearRolesFromLocalStorage();
    return await this.getUserRoles(true);
  }

  /**
   * Validate selected roles against current user roles and remove invalid ones
   */
  validateAndCleanSelectedRoles(currentUserRoles: string[]): void {
    const selectedRoles = this.getSelectedRolesFromLocalStorage();
    const selectedRole = this.getSelectedRoleFromLocalStorage();

    // Normalize roles to lowercase for comparison
    const normalizedUserRoles = currentUserRoles.map(r => r.toLowerCase());

    // Check if user is ECM_ADMIN - they can have any role selected
    const isAdmin = normalizedUserRoles.includes('ecm_admin');

    if (!isAdmin) {
      // Validate multi-role selection
      if (selectedRoles.length > 0) {
        const validSelectedRoles = selectedRoles.filter(role =>
          normalizedUserRoles.includes(role.toLowerCase())
        );

        if (validSelectedRoles.length !== selectedRoles.length) {
          console.warn('Some selected roles are no longer valid. Cleaning up...', {
            original: selectedRoles,
            valid: validSelectedRoles,
            currentUserRoles: currentUserRoles
          });

          if (validSelectedRoles.length > 0) {
            this.saveSelectedRolesToLocalStorage(validSelectedRoles);
          } else {
            // No valid roles remain, clear selection
            this.clearSelectedRolesFromLocalStorage();
          }
        }
      }

      // Validate single role selection
      if (selectedRole) {
        const isValidRole = normalizedUserRoles.includes(selectedRole.toLowerCase());
        if (!isValidRole) {
          console.warn('Selected role is no longer valid. Clearing...', {
            selectedRole: selectedRole,
            currentUserRoles: currentUserRoles
          });
          this.clearSelectedRoleFromLocalStorage();
        }
      }
    }
  }

  /**
   * Get validated selected roles - only returns roles that the user currently has
   */
  async getValidatedSelectedRoles(): Promise<string[]> {
    const selectedRoles = this.getSelectedRolesFromLocalStorage();

    if (selectedRoles.length === 0) {
      return [];
    }

    // Get current user roles (from cache first, then token if needed)
    const currentUserRoles = await this.getUserRoles();
    const normalizedUserRoles = currentUserRoles.map(r => r.toLowerCase());

    // Check if user is ECM_ADMIN - they can use any role
    const isAdmin = normalizedUserRoles.includes('ecm_admin');
    if (isAdmin) {
      return selectedRoles;
    }

    // Filter to only valid roles
    const validRoles = selectedRoles.filter(role =>
      normalizedUserRoles.includes(role.toLowerCase())
    );

    // If some roles were invalid, update localStorage
    if (validRoles.length !== selectedRoles.length) {
      console.warn('Filtered out invalid selected roles', {
        original: selectedRoles,
        valid: validRoles
      });

      if (validRoles.length > 0) {
        this.saveSelectedRolesToLocalStorage(validRoles);
      } else {
        this.clearSelectedRolesFromLocalStorage();
      }
    }

    return validRoles;
  }

  /**
   * Get validated selected role (single) - only returns if user currently has this role
   */
  async getValidatedSelectedRole(): Promise<string | null> {
    const selectedRole = this.getSelectedRoleFromLocalStorage();

    if (!selectedRole) {
      return null;
    }

    // Get current user roles
    const currentUserRoles = await this.getUserRoles();
    const normalizedUserRoles = currentUserRoles.map(r => r.toLowerCase());

    // Check if user is ECM_ADMIN
    const isAdmin = normalizedUserRoles.includes('ecm_admin');
    if (isAdmin) {
      return selectedRole;
    }

    // Check if selected role is valid
    const isValid = normalizedUserRoles.includes(selectedRole.toLowerCase());

    if (!isValid) {
      console.warn('Selected role is no longer valid, clearing', {
        selectedRole: selectedRole,
        currentUserRoles: currentUserRoles
      });
      this.clearSelectedRoleFromLocalStorage();
      return null;
    }

    return selectedRole;
  }

  /**
   * Clear selected roles (multi-select) from localStorage
   */
  clearSelectedRolesFromLocalStorage(): void {
    try {
      localStorage.removeItem('selectedRoles');
    } catch (error) {
      console.warn('Error clearing selected roles from localStorage:', error);
    }
  }

  /**
   * Get user roles from localStorage
   */
  private getRolesFromLocalStorage(): string[] {
    try {
      const storedRoles = localStorage.getItem('userRoles');
      if (storedRoles) {
        return JSON.parse(storedRoles);
      }
    } catch (error) {
      console.warn('Error reading roles from localStorage:', error);
    }
    return [];
  }

  /**
   * Save user roles to localStorage
   */
  private saveRolesToLocalStorage(roles: string[]): void {
    try {
      localStorage.setItem('userRoles', JSON.stringify(roles));
    } catch (error) {
      console.warn('Error saving roles to localStorage:', error);
    }
  }

  /**
   * Clear user roles from localStorage
   */
  clearRolesFromLocalStorage(): void {
    try {
      localStorage.removeItem('userRoles');
    } catch (error) {
      console.warn('Error clearing roles from localStorage:', error);
    }
  }

  /**
   * Clear access token from localStorage
   */
  clearAccessTokenFromLocalStorage(): void {
    try {
      localStorage.removeItem('accessToken');
    } catch (error) {
      console.warn('Error clearing access token from localStorage:', error);
    }
  }

  /**
   * Get selected role from localStorage
   */
  getSelectedRoleFromLocalStorage(): string | null {
    try {
      return localStorage.getItem('selectedRole');
    } catch (error) {
      console.warn('Error reading selected role from localStorage:', error);
      return null;
    }
  }

  /**
   * Save selected role to localStorage
   */
  saveSelectedRoleToLocalStorage(role: string): void {
    try {
      localStorage.setItem('selectedRole', role);
    } catch (error) {
      console.warn('Error saving selected role to localStorage:', error);
    }
  }

  /**
   * Get selected roles from localStorage for multi-role selection
   */
  getSelectedRolesFromLocalStorage(): string[] {
    try {
      const rolesJson = localStorage.getItem('selectedRoles');
      return rolesJson ? JSON.parse(rolesJson) : [];
    } catch (error) {
      console.warn('Error reading selected roles from localStorage:', error);
      return [];
    }
  }

  /**
   * Save selected roles to localStorage for multi-role selection
   */
  saveSelectedRolesToLocalStorage(roles: string[]): void {
    try {
      localStorage.setItem('selectedRoles', JSON.stringify(roles));
    } catch (error) {
      console.warn('Error saving selected roles to localStorage:', error);
    }
  }

  /**
   * Clear selected role from localStorage
   */
  clearSelectedRoleFromLocalStorage(): void {
    try {
      localStorage.removeItem('selectedRole');
    } catch (error) {
      console.warn('Error clearing selected role from localStorage:', error);
    }
  }

  /**
   * Get the currently selected role for use in API calls or UI logic
   */
  getCurrentSelectedRole(): string | null {
    return this.getSelectedRoleFromLocalStorage();
  }

  /**
   * Decode JWT token to get payload
   */
  private decodeJWT(token: string): any {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Error decoding JWT:', error);
      return {};
    }
  }

  /**
   * Check if user has a specific role
   */
  async hasRole(role: string): Promise<boolean> {
    const userRoles = await this.getUserRoles();
    return userRoles.map(x => x.toLocaleLowerCase()).includes(role);
  }

  /**
   * Check if user is authorized (not guest and has roles)
   * This method now uses localStorage for faster authorization checks
   */
  isAuthorized(): boolean {
    // First try to get roles from localStorage for fast check
    const cachedRoles = this.getRolesFromLocalStorage();
    
    if (cachedRoles.length > 0) {
      return this.checkRoleAuthorization(cachedRoles);
    }
    
    // If no cached roles, user is not authorized
    // The calling component should handle fetching roles if needed
    return false;
  }

  /**
   * Check if user is authorized (async version for components that need to fetch roles)
   */
  async isAuthorizedAsync(): Promise<boolean> {
    const roles = await this.getUserRoles();
    return this.checkRoleAuthorization(roles);
  }

  /**
   * Helper method to check role-based authorization
   */
  private checkRoleAuthorization(roles: string[]): boolean {
    // If user has "ECM_ADMIN" role, they are always authorized
    if (roles.map(x => x.toLowerCase()).includes('ecm_admin')) {
      return true;
    }
    
    // If no roles, user is not authorized
    if (roles.length === 0) {
      return false;
    }
    
    // If user has "guest" role, they are not authorized
    if (roles.map(x => x.toLowerCase()).includes('guest')) {
      return false;
    }
    
    return true;
  }

  /**
   * Centralized user authorization check for components
   * This method provides a consistent way for all components to check authorization
   * using localStorage for fast checks and falling back to async token validation
   * @param forceRefresh If true, forces a refresh of roles from Azure AD token
   */
  async checkUserAuthorization(forceRefresh: boolean = false): Promise<boolean> {
    try {
      // Initialize MSAL first
      await this.initialize();

      // If force refresh is requested, refresh roles from Azure AD
      if (forceRefresh) {
        console.log('Force refreshing user roles from Azure AD...');
        const freshRoles = await this.refreshUserRoles();
        console.log('Fresh roles from Azure AD:', freshRoles);
        return this.checkRoleAuthorization(freshRoles);
      }

      // First try fast localStorage check
      if (this.isAuthorized()) {
        console.log('User authorized from localStorage');
        // Validate selected roles in background (non-blocking)
        this.getValidatedSelectedRoles().catch(err =>
          console.warn('Error validating selected roles:', err)
        );
        return true;
      }

      // If not authorized from localStorage, fetch roles and check again
      const isAuthorized = await this.isAuthorizedAsync();
      if (isAuthorized) {
        console.log('User authorized after fetching roles');
        // Validate selected roles
        await this.getValidatedSelectedRoles();
        return true;
      } else {
        const userRoles = await this.getUserRoles();
        console.log('User not authorized. Roles:', userRoles);
        return false;
      }
    } catch (error) {
      console.error('Error checking user authorization:', error);
      return false;
    }
  }

  /**
   * Force refresh all cached data (roles, token) and validate selected roles
   * Call this when you suspect roles may have changed in Azure AD
   */
  async forceRefreshAndValidate(): Promise<{
    userRoles: string[];
    validSelectedRoles: string[];
  }> {
    console.log('Force refreshing all cached data...');

    // Clear all cached data
    this.clearRolesFromLocalStorage();
    this.clearAccessTokenFromLocalStorage();

    // Fetch fresh roles from Azure AD
    const userRoles = await this.getUserRoles(true);

    // Validate selected roles against fresh user roles
    const validSelectedRoles = await this.getValidatedSelectedRoles();

    console.log('Force refresh complete:', {
      userRoles,
      validSelectedRoles
    });

    return { userRoles, validSelectedRoles };
  }

  /**
   * Clean up subscriptions
   */
  ngOnDestroy(): void {
    this._destroying$.next();
    this._destroying$.complete();
  }
}