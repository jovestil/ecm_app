import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { EmployeeService, EmployeeSyncResult, SyncOptions } from '../../../core/services/employee.service';
import { ReferenceDataService, CompanySyncResultDto, DepartmentSyncResultDto, PositionSyncResultDto, PayrollGroupSyncResultDto, UnionCraftSyncResultDto, EmploymentStatusSyncResultDto, EmployeeSalaryTypeSyncResultDto, ViewpointSyncStatusDto, SyncScheduleConfigDto, SyncScheduleResultDto } from '../../../core/services/reference-data.service';
import { AuthService } from '../../../core/services/auth.service';
import { AppHeaderComponent } from '../../../shared/app-header/app-header.component';

interface SyncProgress {
  isRunning: boolean;
  totalPages: number;
  currentPage: number;
  totalProcessed: number;
  totalInserted: number;
  totalUpdated: number;
  totalDeleted: number;
  totalErrors: number;
  allErrors: string[];
  startTime?: Date;
  endTime?: Date;
  hasMore: boolean;
}

@Component({
  selector: 'app-viewpoint-sync',
  standalone: true,
  imports: [CommonModule, FormsModule, AppHeaderComponent],
  templateUrl: './viewpoint-sync.component.html',
  styleUrl: './viewpoint-sync.component.css'
})
export class ViewpointSyncComponent implements OnInit {
  userName = '';
  
  // Sync configuration
  syncOptions: SyncOptions = {
    pageSize: 100,
    filter: ''
  };

  // Progress tracking
  progress: SyncProgress = {
    isRunning: false,
    totalPages: 0,
    currentPage: 0,
    totalProcessed: 0,
    totalInserted: 0,
    totalUpdated: 0,
    totalDeleted: 0,
    totalErrors: 0,
    allErrors: [],
    hasMore: false
  };

  // UI state
  showResults = false;
  showErrors = false;
  errorMessage = '';

  // Company sync state
  companySyncRunning = false;
  companySyncResult: CompanySyncResultDto | null = null;
  companySyncError = '';

  // Department sync state
  departmentSyncRunning = false;
  departmentSyncResult: DepartmentSyncResultDto | null = null;
  departmentSyncError = '';

  // Position sync state
  positionSyncRunning = false;
  positionSyncResult: PositionSyncResultDto | null = null;
  positionSyncError = '';

  // Payroll group sync state
  payrollGroupSyncRunning = false;
  payrollGroupSyncResult: PayrollGroupSyncResultDto | null = null;
  payrollGroupSyncError = '';

  // Union craft sync state
  unionCraftSyncRunning = false;
  unionCraftSyncResult: UnionCraftSyncResultDto | null = null;
  unionCraftSyncError = '';

  // Employment status sync state
  employmentStatusSyncRunning = false;
  employmentStatusSyncResult: EmploymentStatusSyncResultDto | null = null;
  employmentStatusSyncError = '';

  // Employee salary type sync state
  employeeSalaryTypeSyncRunning = false;
  employeeSalaryTypeSyncResult: EmployeeSalaryTypeSyncResultDto | null = null;
  employeeSalaryTypeSyncError = '';

  // Viewpoint sync status state
  syncStatusLoading = false;
  syncStatus: ViewpointSyncStatusDto | null = null;
  syncStatusError = '';

  // Schedule settings state
  showScheduleSettings = false;
  savingSchedule = false;
  scheduleSettings = {
    companies: 'disabled',
    departments: 'disabled',
    positions: 'disabled',
    payrollGroups: 'disabled',
    unionCrafts: 'disabled',
    employmentStatuses: 'disabled',
    employeeSalaryTypes: 'disabled',
    employees: 'disabled'
  };

  constructor(
    private employeeService: EmployeeService,
    private referenceDataService: ReferenceDataService,
    private authService: AuthService
  ) {}

  async ngOnInit() {
    await this.checkUserAuthorization();
    this.userName = this.authService.getUserDisplayName();
    await this.loadSyncStatus();
  }

  private async checkUserAuthorization(): Promise<void> {
    const isAuthorized = await this.authService.checkUserAuthorization();
    if (!isAuthorized) {
      console.log('User not authorized for viewpoint sync');
      // Note: This component already handles 403 errors, but we add pre-check for consistency
      this.errorMessage = 'You do not have permission to access this page. Please contact your administrator.';
      return;
    }
  }

  /**
   * Start the sync process — single call to the full-sweep endpoint.
   * The backend fetches every Viewpoint employee in one pass, upserts them,
   * and soft-deletes (IsDeleted=true) any ECM row absent from the payload.
   */
  async startSync() {
    if (this.progress.isRunning) {
      return;
    }

    // Reset progress
    this.progress = {
      isRunning: true,
      totalPages: 1,
      currentPage: 1,
      totalProcessed: 0,
      totalInserted: 0,
      totalUpdated: 0,
      totalDeleted: 0,
      totalErrors: 0,
      allErrors: [],
      hasMore: false,
      startTime: new Date()
    };

    this.showResults = false;
    this.showErrors = false;
    this.errorMessage = '';

    try {
      const syncResult = await this.runFullSync();

      if (syncResult) {
        this.progress.totalProcessed = syncResult.totalProcessed;
        this.progress.totalInserted = syncResult.insertedCount;
        this.progress.totalUpdated = syncResult.updatedCount;
        this.progress.totalDeleted = syncResult.deletedCount;
        this.progress.totalErrors = syncResult.errorCount;
        this.progress.allErrors.push(...syncResult.errors);
      }

      this.progress.endTime = new Date();
      this.progress.isRunning = false;
      this.showResults = true;
      // Refresh sync status after successful employee sync
      this.loadSyncStatus();

    } catch (error) {
      this.handleSyncError(error);
    }
  }

  /**
   * Run the full-sweep sync (single call, no pagination loop).
   */
  private async runFullSync(): Promise<EmployeeSyncResult | null> {
    return new Promise((resolve) => {
      this.employeeService.syncAllEmployeesFromViewpoint().subscribe({
        next: (response) => {
          if (response.success && response.data) {
            resolve(response.data);
          } else {
            this.errorMessage = response.message || 'Sync failed';
            resolve(null);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.handleSyncError(error);
          resolve(null);
        }
      });
    });
  }

  /**
   * Stop the sync process
   */
  stopSync() {
    this.progress.isRunning = false;
    this.progress.endTime = new Date();
    this.showResults = true;
  }

  /**
   * Handle sync errors
   */
  private handleSyncError(error: any) {
    console.error('Sync error:', error);
    this.progress.isRunning = false;
    this.progress.endTime = new Date();
    
    if (error instanceof HttpErrorResponse) {
      if (error.status === 403) {
        this.errorMessage = 'You do not have permission to sync employees. Please contact your administrator.';
      } else if (error.status === 401) {
        this.errorMessage = 'Authentication failed. Please log in again.';
      } else {
        this.errorMessage = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
      }
    } else {
      this.errorMessage = error.message || 'An unexpected error occurred during sync.';
    }
    
    this.showResults = true;
  }

  /**
   * Get progress percentage
   */
  get progressPercentage(): number {
    if (this.progress.isRunning) {
      // Full-sweep sync completes in one server call; show an indeterminate half-bar.
      return 50;
    }
    if (this.showResults) {
      return 100;
    }
    return 0;
  }

  /**
   * Get sync duration in human readable format
   */
  get syncDuration(): string {
    if (!this.progress.startTime) {
      return '0s';
    }
    
    const endTime = this.progress.endTime || new Date();
    const duration = endTime.getTime() - this.progress.startTime.getTime();
    
    const seconds = Math.floor(duration / 1000);
    const minutes = Math.floor(seconds / 60);
    
    if (minutes > 0) {
      return `${minutes}m ${seconds % 60}s`;
    }
    return `${seconds}s`;
  }

  /**
   * Toggle error details
   */
  toggleErrors() {
    this.showErrors = !this.showErrors;
  }

  /**
   * Reset the sync state
   */
  resetSync() {
    this.progress = {
      isRunning: false,
      totalPages: 0,
      currentPage: 0,
      totalProcessed: 0,
      totalInserted: 0,
      totalUpdated: 0,
      totalDeleted: 0,
      totalErrors: 0,
      allErrors: [],
      hasMore: false
    };
    this.showResults = false;
    this.showErrors = false;
    this.errorMessage = '';
  }

  /**
   * Start company sync from Viewpoint
   */
  async startCompanySync() {
    if (this.companySyncRunning) {
      return;
    }

    this.companySyncRunning = true;
    this.companySyncResult = null;
    this.companySyncError = '';

    try {
      this.referenceDataService.syncCompaniesFromViewpoint().subscribe({
        next: (response) => {
          this.companySyncRunning = false;
          if (response.success && response.data) {
            this.companySyncResult = response.data;
            console.log('Company sync completed successfully:', this.companySyncResult);
            // Refresh sync status after successful sync
            this.loadSyncStatus();
          } else {
            this.companySyncError = response.message || 'Company sync failed';
            console.error('Company sync failed:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.companySyncRunning = false;
          console.error('Company sync error:', error);
          
          if (error.status === 403) {
            this.companySyncError = 'You do not have permission to sync companies. Please contact your administrator.';
          } else if (error.status === 401) {
            this.companySyncError = 'Authentication failed. Please log in again.';
          } else {
            this.companySyncError = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
          }
        }
      });
    } catch (error: any) {
      this.companySyncRunning = false;
      this.companySyncError = error.message || 'An unexpected error occurred during company sync.';
      console.error('Unexpected company sync error:', error);
    }
  }

  /**
   * Reset company sync state
   */
  resetCompanySync() {
    this.companySyncResult = null;
    this.companySyncError = '';
  }

  /**
   * Start department sync from Viewpoint
   */
  async startDepartmentSync() {
    if (this.departmentSyncRunning) {
      return;
    }

    this.departmentSyncRunning = true;
    this.departmentSyncResult = null;
    this.departmentSyncError = '';

    try {
      this.referenceDataService.syncDepartmentsFromViewpoint().subscribe({
        next: (response) => {
          this.departmentSyncRunning = false;
          if (response.success && response.data) {
            this.departmentSyncResult = response.data;
            console.log('Department sync completed successfully:', this.departmentSyncResult);
            // Refresh sync status after successful sync
            this.loadSyncStatus();
          } else {
            this.departmentSyncError = response.message || 'Department sync failed';
            console.error('Department sync failed:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.departmentSyncRunning = false;
          console.error('Department sync error:', error);
          
          if (error.status === 403) {
            this.departmentSyncError = 'You do not have permission to sync departments. Please contact your administrator.';
          } else if (error.status === 401) {
            this.departmentSyncError = 'Authentication failed. Please log in again.';
          } else {
            this.departmentSyncError = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
          }
        }
      });
    } catch (error: any) {
      this.departmentSyncRunning = false;
      this.departmentSyncError = error.message || 'An unexpected error occurred during department sync.';
      console.error('Unexpected department sync error:', error);
    }
  }

  /**
   * Reset department sync state
   */
  resetDepartmentSync() {
    this.departmentSyncResult = null;
    this.departmentSyncError = '';
  }

  /**
   * Start position sync from Viewpoint
   */
  async startPositionSync() {
    if (this.positionSyncRunning) {
      return;
    }

    this.positionSyncRunning = true;
    this.positionSyncResult = null;
    this.positionSyncError = '';

    try {
      this.referenceDataService.syncPositionsFromViewpoint().subscribe({
        next: (response) => {
          this.positionSyncRunning = false;
          if (response.success && response.data) {
            this.positionSyncResult = response.data;
            console.log('Position sync completed successfully:', this.positionSyncResult);
            // Refresh sync status after successful sync
            this.loadSyncStatus();
          } else {
            this.positionSyncError = response.message || 'Position sync failed';
            console.error('Position sync failed:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.positionSyncRunning = false;
          console.error('Position sync error:', error);
          
          if (error.status === 403) {
            this.positionSyncError = 'You do not have permission to sync positions. Please contact your administrator.';
          } else if (error.status === 401) {
            this.positionSyncError = 'Authentication failed. Please log in again.';
          } else {
            this.positionSyncError = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
          }
        }
      });
    } catch (error: any) {
      this.positionSyncRunning = false;
      this.positionSyncError = error.message || 'An unexpected error occurred during position sync.';
      console.error('Unexpected position sync error:', error);
    }
  }

  /**
   * Reset position sync state
   */
  resetPositionSync() {
    this.positionSyncResult = null;
    this.positionSyncError = '';
  }

  /**
   * Start payroll group sync from Viewpoint
   */
  async startPayrollGroupSync() {
    if (this.payrollGroupSyncRunning) {
      return;
    }

    this.payrollGroupSyncRunning = true;
    this.payrollGroupSyncResult = null;
    this.payrollGroupSyncError = '';

    try {
      this.referenceDataService.syncPayrollGroupsFromViewpoint().subscribe({
        next: (response) => {
          this.payrollGroupSyncRunning = false;
          if (response.success && response.data) {
            this.payrollGroupSyncResult = response.data;
            console.log('Payroll group sync completed successfully:', this.payrollGroupSyncResult);
            // Refresh sync status after successful sync
            this.loadSyncStatus();
          } else {
            this.payrollGroupSyncError = response.message || 'Payroll group sync failed';
            console.error('Payroll group sync failed:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.payrollGroupSyncRunning = false;
          console.error('Payroll group sync error:', error);
          
          if (error.status === 403) {
            this.payrollGroupSyncError = 'You do not have permission to sync payroll groups. Please contact your administrator.';
          } else if (error.status === 401) {
            this.payrollGroupSyncError = 'Authentication failed. Please log in again.';
          } else {
            this.payrollGroupSyncError = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
          }
        }
      });
    } catch (error: any) {
      this.payrollGroupSyncRunning = false;
      this.payrollGroupSyncError = error.message || 'An unexpected error occurred during payroll group sync.';
      console.error('Unexpected payroll group sync error:', error);
    }
  }

  /**
   * Reset payroll group sync state
   */
  resetPayrollGroupSync() {
    this.payrollGroupSyncResult = null;
    this.payrollGroupSyncError = '';
  }

  /**
   * Start union craft sync from Viewpoint
   */
  async startUnionCraftSync() {
    if (this.unionCraftSyncRunning) {
      return;
    }

    this.unionCraftSyncRunning = true;
    this.unionCraftSyncResult = null;
    this.unionCraftSyncError = '';

    try {
      this.referenceDataService.syncUnionCraftsFromViewpoint().subscribe({
        next: (response) => {
          this.unionCraftSyncRunning = false;
          if (response.success && response.data) {
            this.unionCraftSyncResult = response.data;
            console.log('Union craft sync completed successfully:', this.unionCraftSyncResult);
            // Refresh sync status after successful sync
            this.loadSyncStatus();
          } else {
            this.unionCraftSyncError = response.message || 'Union craft sync failed';
            console.error('Union craft sync failed:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.unionCraftSyncRunning = false;
          console.error('Union craft sync error:', error);

          if (error.status === 403) {
            this.unionCraftSyncError = 'You do not have permission to sync union crafts. Please contact your administrator.';
          } else if (error.status === 401) {
            this.unionCraftSyncError = 'Authentication failed. Please log in again.';
          } else {
            this.unionCraftSyncError = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
          }
        }
      });
    } catch (error: any) {
      this.unionCraftSyncRunning = false;
      this.unionCraftSyncError = error.message || 'An unexpected error occurred during union craft sync.';
      console.error('Unexpected union craft sync error:', error);
    }
  }

  /**
   * Reset union craft sync state
   */
  resetUnionCraftSync() {
    this.unionCraftSyncResult = null;
    this.unionCraftSyncError = '';
  }

  /**
   * Start employment status sync from Viewpoint
   */
  async startEmploymentStatusSync() {
    if (this.employmentStatusSyncRunning) {
      return;
    }

    this.employmentStatusSyncRunning = true;
    this.employmentStatusSyncResult = null;
    this.employmentStatusSyncError = '';

    try {
      this.referenceDataService.syncEmploymentStatusesFromViewpoint().subscribe({
        next: (response) => {
          this.employmentStatusSyncRunning = false;
          if (response.success && response.data) {
            this.employmentStatusSyncResult = response.data;
            console.log('Employment status sync completed successfully:', this.employmentStatusSyncResult);
            // Refresh sync status after successful sync
            this.loadSyncStatus();
          } else {
            this.employmentStatusSyncError = response.message || 'Employment status sync failed';
            console.error('Employment status sync failed:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.employmentStatusSyncRunning = false;
          console.error('Employment status sync error:', error);

          if (error.status === 403) {
            this.employmentStatusSyncError = 'You do not have permission to sync employment statuses. Please contact your administrator.';
          } else if (error.status === 401) {
            this.employmentStatusSyncError = 'Authentication failed. Please log in again.';
          } else {
            this.employmentStatusSyncError = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
          }
        }
      });
    } catch (error: any) {
      this.employmentStatusSyncRunning = false;
      this.employmentStatusSyncError = error.message || 'An unexpected error occurred during employment status sync.';
      console.error('Unexpected employment status sync error:', error);
    }
  }

  /**
   * Reset employment status sync state
   */
  resetEmploymentStatusSync() {
    this.employmentStatusSyncResult = null;
    this.employmentStatusSyncError = '';
  }

  /**
   * Start employee salary type sync from Viewpoint
   */
  async startEmployeeSalaryTypeSync() {
    if (this.employeeSalaryTypeSyncRunning) {
      return;
    }

    this.employeeSalaryTypeSyncRunning = true;
    this.employeeSalaryTypeSyncResult = null;
    this.employeeSalaryTypeSyncError = '';

    try {
      this.referenceDataService.syncEmployeeSalaryTypesFromViewpoint().subscribe({
        next: (response) => {
          this.employeeSalaryTypeSyncRunning = false;
          if (response.success && response.data) {
            this.employeeSalaryTypeSyncResult = response.data;
            console.log('Employee salary type sync completed successfully:', this.employeeSalaryTypeSyncResult);
            // Refresh sync status after successful sync
            this.loadSyncStatus();
          } else {
            this.employeeSalaryTypeSyncError = response.message || 'Employee salary type sync failed';
            console.error('Employee salary type sync failed:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.employeeSalaryTypeSyncRunning = false;
          console.error('Employee salary type sync error:', error);

          if (error.status === 403) {
            this.employeeSalaryTypeSyncError = 'You do not have permission to sync employee salary types. Please contact your administrator.';
          } else if (error.status === 401) {
            this.employeeSalaryTypeSyncError = 'Authentication failed. Please log in again.';
          } else {
            this.employeeSalaryTypeSyncError = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
          }
        }
      });
    } catch (error: any) {
      this.employeeSalaryTypeSyncRunning = false;
      this.employeeSalaryTypeSyncError = error.message || 'An unexpected error occurred during employee salary type sync.';
      console.error('Unexpected employee salary type sync error:', error);
    }
  }

  /**
   * Reset employee salary type sync state
   */
  resetEmployeeSalaryTypeSync() {
    this.employeeSalaryTypeSyncResult = null;
    this.employeeSalaryTypeSyncError = '';
  }

  /**
   * Load Viewpoint sync status with last sync dates
   */
  async loadSyncStatus() {
    if (this.syncStatusLoading) {
      return;
    }

    this.syncStatusLoading = true;
    this.syncStatusError = '';

    try {
      this.referenceDataService.getViewpointSyncStatus().subscribe({
        next: (response) => {
          this.syncStatusLoading = false;
          if (response.success && response.data) {
            this.syncStatus = response.data;
            console.log('Sync status loaded successfully:', this.syncStatus);
          } else {
            this.syncStatusError = response.message || 'Failed to load sync status';
            console.error('Failed to load sync status:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.syncStatusLoading = false;
          console.error('Error loading sync status:', error);
          
          if (error.status === 403) {
            this.syncStatusError = 'You do not have permission to view sync status. Please contact your administrator.';
          } else if (error.status === 401) {
            this.syncStatusError = 'Authentication failed. Please log in again.';
          } else {
            this.syncStatusError = error.error?.message || `HTTP ${error.status}: ${error.statusText}`;
          }
        }
      });
    } catch (error: any) {
      this.syncStatusLoading = false;
      this.syncStatusError = error.message || 'An unexpected error occurred while loading sync status.';
      console.error('Unexpected sync status error:', error);
    }
  }

  /**
   * Refresh sync status
   */
  async refreshSyncStatus() {
    await this.loadSyncStatus();
  }

  /**
   * Get CSS class based on sync status text for dynamic styling
   */
  getStatusClass(statusText: string): string {
    if (!statusText) return 'status-unknown';
    
    const text = statusText.toLowerCase();
    
    if (text.includes('never synced')) {
      return 'status-never';
    } else if (text.includes('today')) {
      return 'status-recent';
    } else if (text.includes('days ago') || text.includes('1 week')) {
      return 'status-moderate';
    } else if (text.includes('weeks ago') || text.includes('months ago')) {
      return 'status-old';
    }
    
    return 'status-unknown';
  }

  /**
   * Toggle schedule settings visibility
   */
  toggleScheduleSettings() {
    this.showScheduleSettings = !this.showScheduleSettings;
    if (this.showScheduleSettings) {
      this.loadScheduleSettings();
    }
  }

  /**
   * Load current schedule settings
   */
  async loadScheduleSettings() {
    try {
      this.referenceDataService.getSyncScheduleConfig().subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.scheduleSettings = {
              companies: response.data.companies,
              departments: response.data.departments,
              positions: response.data.positions,
              payrollGroups: response.data.payrollGroups,
              unionCrafts: response.data.unionCrafts,
              employmentStatuses: response.data.employmentStatuses || 'disabled',
              employeeSalaryTypes: response.data.employeeSalaryTypes || 'disabled',
              employees: response.data.employees
            };
            console.log('Schedule settings loaded:', this.scheduleSettings);
          } else {
            console.error('Failed to load schedule settings:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          console.error('Error loading schedule settings:', error);
          // Fallback to localStorage for backwards compatibility
          const saved = localStorage.getItem('viewpoint-sync-schedule');
          if (saved) {
            this.scheduleSettings = JSON.parse(saved);
          }
        }
      });
    } catch (error) {
      console.error('Error loading schedule settings:', error);
    }
  }

  /**
   * Save schedule settings
   */
  async saveScheduleSettings() {
    if (this.savingSchedule) {
      return;
    }

    this.savingSchedule = true;

    try {
      const configDto: SyncScheduleConfigDto = {
        companies: this.scheduleSettings.companies,
        departments: this.scheduleSettings.departments,
        positions: this.scheduleSettings.positions,
        payrollGroups: this.scheduleSettings.payrollGroups,
        unionCrafts: this.scheduleSettings.unionCrafts,
        employmentStatuses: this.scheduleSettings.employmentStatuses,
        employeeSalaryTypes: this.scheduleSettings.employeeSalaryTypes,
        employees: this.scheduleSettings.employees,
        lastUpdated: new Date().toISOString(),
        updatedBy: this.userName || 'current-user'
      };

      this.referenceDataService.updateSyncScheduleConfig(configDto).subscribe({
        next: (response) => {
          this.savingSchedule = false;
          if (response.success && response.data) {
            console.log('Schedule settings saved successfully:', response.data);
            console.log('Scheduled jobs:', response.data.scheduledJobs);
            
            // Also save to localStorage as backup
            localStorage.setItem('viewpoint-sync-schedule', JSON.stringify(this.scheduleSettings));
            
            // You could show a success message here with scheduled jobs info
            if (response.data.scheduledJobs.length > 0) {
              console.log('✅ Hangfire jobs scheduled:', response.data.scheduledJobs);
            }
          } else {
            console.error('Failed to save schedule settings:', response);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.savingSchedule = false;
          console.error('Error saving schedule settings:', error);
          
          // Fallback to localStorage
          localStorage.setItem('viewpoint-sync-schedule', JSON.stringify(this.scheduleSettings));
          console.log('Settings saved to localStorage as fallback');
        }
      });
    } catch (error) {
      this.savingSchedule = false;
      console.error('Error saving schedule settings:', error);
    }
  }

  /**
   * Reset schedule settings to default
   */
  resetScheduleSettings() {
    this.scheduleSettings = {
      companies: 'disabled',
      departments: 'disabled',
      positions: 'disabled',
      payrollGroups: 'disabled',
      unionCrafts: 'disabled',
      employmentStatuses: 'disabled',
      employeeSalaryTypes: 'disabled',
      employees: 'disabled'
    };
  }
}