import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';
import { MsalService, MsalBroadcastService } from '@azure/msal-angular';
import { EventMessage, EventType, InteractionStatus } from '@azure/msal-browser';
import { AuthService } from '../../core/services/auth.service';
import { ReferenceDataService, PayrollDepartmentShortNameDto } from '../../core/services/reference-data.service';

// Interface for role selection in header (separate from PayrollDepartmentDto)
interface RoleDto {
  deptShortName: string;
  fullName: string;
}

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app-header.component.html',
  styleUrls: ['./app-header.component.css']
})
export class AppHeaderComponent implements OnInit, OnDestroy {
  @Input() userName: string = '';
  userEmail: string = '';
  userRoles: string[] = [];
  isAuthenticated = false;
  isEcmAdmin = false;
  availableRoles: RoleDto[] = [];
  selectedRole: string = '';
  selectedRoles: string[] = [];
  isRoleDropdownOpen = false;
  private readonly _destroying$ = new Subject<void>();

  constructor(
    private router: Router,
    private authService: AuthService,
    private msalService: MsalService,
    private msalBroadcastService: MsalBroadcastService,
    private referenceDataService: ReferenceDataService
  ) {}

  ngOnInit(): void {
    // Listen for authentication status changes
    this.msalBroadcastService.inProgress$
      .pipe(
        filter((status: InteractionStatus) => status === InteractionStatus.None),
        takeUntil(this._destroying$)
      )
      .subscribe(() => {
        this.updateUserInfo();
      });

    // Listen for login success events
    this.msalBroadcastService.msalSubject$
      .pipe(
        filter((msg: EventMessage) => msg.eventType === EventType.LOGIN_SUCCESS),
        takeUntil(this._destroying$)
      )
      .subscribe(() => {
        this.updateUserInfo();
      });

    // Initial check
    this.updateUserInfo();
  }

  ngOnDestroy(): void {
    this._destroying$.next();
    this._destroying$.complete();
  }

  private async updateUserInfo(): Promise<void> {
    this.isAuthenticated = this.authService.isAuthenticated();
    
    if (this.isAuthenticated) {
      const account = this.msalService.instance.getActiveAccount();
      if (account) {
        // Get user display name (preferred name)
        this.userName = account.name || 
                       account.idTokenClaims?.['preferred_username'] ||
                       account.idTokenClaims?.['name'] ||
                       account.username ||
                       'User';

        // Get user email
        this.userEmail = (account.username || 
                         account.idTokenClaims?.['email'] ||
                         account.idTokenClaims?.['preferred_username'] ||
                         '') as string;

        // Get user roles
        try {
          this.userRoles = await this.authService.getUserRoles();
          this.isEcmAdmin = this.userRoles.some(role => role.toLowerCase() === 'ecm_admin');
          
          // If user is ECM_ADMIN, load available roles for dropdown
          if (this.isEcmAdmin) {
            this.loadAvailableRoles();
            
            // For ECM_ADMIN users, load selected roles from localStorage
            const savedRoles = this.authService.getSelectedRolesFromLocalStorage();
            if (savedRoles.length > 0) {
              this.selectedRoles = savedRoles;
            } else {
              // Default to ECM_ADMIN if no saved roles
              this.selectedRoles = ['ECM_ADMIN'];
            }
          }
        } catch (error) {
          console.error('Error getting user roles:', error);
          this.userRoles = [];
        }

        console.log('🧑‍💼 Authenticated user:', {
          name: this.userName,
          email: this.userEmail,
          roles: this.userRoles,
          isEcmAdmin: this.isEcmAdmin,
          accountInfo: account
        });
      }
    } else {
      this.userName = '';
      this.userEmail = '';
      this.userRoles = [];
      this.isEcmAdmin = false;
      this.selectedRole = '';
      this.selectedRoles = [];
      this.availableRoles = [];
    }
  }

  handleLogin(): void {
    this.authService.loginRedirect();
  }

  handleLogout(event?: Event): void {
    this.router.navigate(['/logout']);
  }

  navigateToMe(): void {
    this.router.navigate(['/me']);
  }

  private loadAvailableRoles(): void {
    this.referenceDataService.getPayrollDepartmentShortNamesWithCache().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          // Map PayrollDepartmentShortNameDto to RoleDto format
          this.availableRoles = response.data.map(dept => ({
            deptShortName: dept.deptShortName,
            fullName: `Company ${dept.companyCode} - Dept ${dept.deptCode}`
          }));
        }
      },
      error: (error) => {
        console.error('Error loading available roles:', error);
        this.availableRoles = [];
      }
    });
  }

  toggleRoleDropdown(): void {
    this.isRoleDropdownOpen = !this.isRoleDropdownOpen;
  }

  closeRoleDropdown(): void {
    this.isRoleDropdownOpen = false;
  }

  selectRole(role: RoleDto): void {
    this.selectedRole = role.deptShortName;
    this.isRoleDropdownOpen = false;
    
    // Save selected role to localStorage
    this.authService.saveSelectedRoleToLocalStorage(role.deptShortName);
    
    console.log('Role selected and saved to localStorage:', role);
    
    // Reload the page to apply the new role context
    window.location.reload();
  }

  isRoleSelected(roleName: string): boolean {
    return this.selectedRoles.includes(roleName);
  }

  toggleRoleSelection(role: RoleDto): void {
    const roleName = role.deptShortName;
    const index = this.selectedRoles.indexOf(roleName);
    
    if (index > -1) {
      // Remove role
      this.selectedRoles.splice(index, 1);
    } else {
      // Add role
      this.selectedRoles.push(roleName);
      
      // If ECM_ADMIN is selected, remove all other roles
      if (roleName === 'ECM_ADMIN') {
        this.selectedRoles = ['ECM_ADMIN'];
      } else {
        // If any other role is selected, remove ECM_ADMIN
        const ecmAdminIndex = this.selectedRoles.indexOf('ECM_ADMIN');
        if (ecmAdminIndex > -1) {
          this.selectedRoles.splice(ecmAdminIndex, 1);
        }
      }
    }
    
    // Save and refresh immediately
    this.saveRolesAndRefresh();
  }

  clearAllRoles(): void {
    this.selectedRoles = [];
    this.saveRolesAndRefresh();
  }

  private saveRolesAndRefresh(): void {
    // Save selected roles to localStorage
    this.authService.saveSelectedRolesToLocalStorage(this.selectedRoles);
    
    console.log('Roles updated and saved to localStorage:', this.selectedRoles);
    
    // Close dropdown and reload page to apply new role context
    this.isRoleDropdownOpen = false;
    window.location.reload();
  }
}