import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { RequestType, RequestTypeOption } from '../../../models/hr-request.model';
import { ApiHRRequestDetailDto } from '../../../models/api-hr-request.model';
import { AppHeaderComponent } from '../../../shared/app-header/app-header.component';
import { ConfirmationDialogComponent, ConfirmationDialogConfig } from '../../../shared/confirmation-dialog/confirmation-dialog.component';
import { AuthService } from '../../../core/services/auth.service';
import { HRRequestService } from '../../../core/services/hr-request.service';
import { ReferenceDataService, RequestStatusDto } from '../../../core/services/reference-data.service';
import { BackgroundJobService } from '../../../core/services/background-job.service';
import { SignalRService, HRRequestNotification } from '../../../core/services/signalr.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { StatusColorHelper } from '../../../core/constants/status-colors.constants';

@Component({
  selector: 'app-hr-request-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, AppHeaderComponent, ConfirmationDialogComponent],
  templateUrl: './hr-request-dashboard.component.html',
  styleUrls: ['../../../shared/styles/common.css', './hr-request-dashboard.component.css']
})
export class HrRequestDashboardComponent implements OnInit, OnDestroy {
  requests: ApiHRRequestDetailDto[] = [];
  filteredRequests: ApiHRRequestDetailDto[] = [];
  searchTerm = '';
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  totalCount = 0;
  showModal = false;
  userName = 'John Doe';
  isLoading = false;
  errorMessage = '';
  
  // Request statuses from backend
  requestStatuses: RequestStatusDto[] = [];

  // Cancel confirmation dialog
  showCancelConfirmDialog = false;
  confirmCancelDialogConfig: ConfirmationDialogConfig = {
    title: 'Cancel New Hire Request',
    message: 'Are you sure you want to cancel this new hire request? A cancellation notification email will be sent immediately. This action cannot be undone.',
    confirmButtonText: 'Yes, Cancel Request',
    cancelButtonText: 'Keep Request',
    confirmButtonClass: 'btn-danger',
    cancelButtonClass: 'btn-secondary',
    showIcon: true,
    iconType: 'warning'
  };
  pendingCancelRequest: ApiHRRequestDetailDto | null = null;

  // Sorting configuration - default sort by creation date (newest first)
  // Available sortable fields: employeeName, companyName, departmentName, effectiveDate, requestStatusName, submittedByName, requestTypeName
  sortField: string | null = null; // Use null to trigger default CreatedDate desc sort on server
  sortDirection: 'asc' | 'desc' = 'desc';

  isEcmAdmin = false;

  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();
  private processedNotifications = new Set<string>(); // Track processed notifications

  requestTypeOptions: RequestTypeOption[] = [
    {
      type: 'new-hire',
      title: 'New Hire',
      description: 'Request to hire a new employee',
      icon: 'NH',
      route: '/new-hire'
    },
    {
      type: 'promotion',
      title: 'Promotion / Transfer',
      description: 'Request to promote or transfer an employee',
      icon: 'PT',
      route: '/promotion'
    },
    {
      type: 'layoff',
      title: 'Layoff',
      description: 'Request for employee layoff',
      icon: 'L',
      route: '/layoff'
    },
    {
      type: 'return',
      title: 'Return to Work',
      description: 'Request for employee return after layoff',
      icon: 'R',
      route: '/return-to-work'
    },
    {
      type: 'termination',
      title: 'Termination',
      description: 'Request to terminate an employee',
      icon: 'T',
      route: '/termination'
    }
  ];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private hrRequestService: HRRequestService,
    private referenceDataService: ReferenceDataService,
    private backgroundJobService: BackgroundJobService,
    private signalRService: SignalRService,
    private toasterService: ToasterService
  ) {}

  async ngOnInit(): Promise<void> {
    this.restoreStateFromQueryParams();
    window.addEventListener('beforeunload', this.onBeforeUnload);
    await this.checkUserAuthorization();
    await this.loadRequestStatuses();
    await this.loadHRRequests();
    this.setupSearchDebounce();
    await this.initializeSignalR();
    this.setupVisibilityChangeHandler();
  }

  async ngOnDestroy(): Promise<void> {
    this.destroy$.next();
    this.destroy$.complete();
    
    // Clear processed notifications to prevent memory leaks
    this.processedNotifications.clear();
    
    // Clean up event listeners
    document.removeEventListener('visibilitychange', this.visibilityChangeHandler);
    window.removeEventListener('beforeunload', this.onBeforeUnload);
    
    // Note: We don't stop SignalR connection here as it's shared across the app
    // The SignalR service manages its own lifecycle
  }

  private visibilityChangeHandler = async () => {
    if (!document.hidden) {
      console.log('📱 Tab became active - refreshing HR requests data');
      
      // Reconnect SignalR if needed
      if (!this.signalRService.isConnected()) {
        console.log('🔄 SignalR disconnected, attempting to reconnect...');
        try {
          const token = await this.authService.getAccessTokenPromise();
          if (token) {
            await this.signalRService.startConnection(token);
          }
        } catch (error) {
          console.error('Failed to reconnect SignalR:', error);
        }
      }
      
      // Refresh the data to catch any missed updates
      await this.loadHRRequests();
    } else {
      console.log('📱 Tab became inactive');
    }
  };

  private setupSearchDebounce(): void {
    this.searchSubject.pipe(
      debounceTime(300), // Wait 300ms after user stops typing
      distinctUntilChanged(), // Only emit if value is different from previous
      takeUntil(this.destroy$) // Unsubscribe when component is destroyed
    ).subscribe(searchTerm => {
      this.performSearch(searchTerm);
    });
  }

  private setupVisibilityChangeHandler(): void {
    document.addEventListener('visibilitychange', this.visibilityChangeHandler);
  }

  private onBeforeUnload = (): void => {
    // This fires only on actual page refresh/close, never on SPA navigations.
    // Set a flag so the next dashboard init knows it was a refresh.
    sessionStorage.setItem('ecm_dashboard_refreshed', 'true');
  };

  private restoreStateFromQueryParams(): void {
    const wasRefreshed = sessionStorage.getItem('ecm_dashboard_refreshed');
    sessionStorage.removeItem('ecm_dashboard_refreshed');

    if (wasRefreshed) {
      // Page was refreshed — reset to page 1 and clear query params
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {},
        replaceUrl: true
      });
      return;
    }

    // SPA back navigation — restore pagination state from query params
    const params = this.route.snapshot.queryParams;
    if (params['page']) {
      this.currentPage = parseInt(params['page'], 10) || 1;
    }
    if (params['pageSize']) {
      this.pageSize = parseInt(params['pageSize'], 10) || 10;
    }
    if (params['search']) {
      this.searchTerm = params['search'];
    }
    if (params['sortField']) {
      this.sortField = params['sortField'];
    }
    if (params['sortDirection']) {
      this.sortDirection = params['sortDirection'] === 'asc' ? 'asc' : 'desc';
    }
  }

  private updateUrlParams(): void {
    const queryParams: any = {};
    if (this.currentPage > 1) {
      queryParams.page = this.currentPage;
    }
    if (this.pageSize !== 10) {
      queryParams.pageSize = this.pageSize;
    }
    if (this.searchTerm.trim()) {
      queryParams.search = this.searchTerm.trim();
    }
    if (this.sortField) {
      queryParams.sortField = this.sortField;
      queryParams.sortDirection = this.sortDirection;
    }
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      replaceUrl: true
    });
  }

  async loadRequestStatuses(): Promise<void> {
    try {
      const response = await this.referenceDataService.getRequestStatusesWithCache().toPromise();
      
      if (response?.success && response.data) {
        this.requestStatuses = response.data;
        console.log('Request statuses loaded successfully:', this.requestStatuses);
      } else {
        console.error('Failed to load request statuses:', response?.message);
      }
    } catch (error) {
      console.error('Error loading request statuses:', error);
    }
  }

  async loadHRRequests(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';
    
    try {
      const searchTerm = this.searchTerm.trim() || undefined;
      const response = await this.hrRequestService.getAllHRRequestDetails(
        this.currentPage, 
        this.pageSize,
        undefined, // requestTypeId
        undefined, // statusId
        undefined, // employeeId
        undefined, // submittedBy
        searchTerm, // searchTerm
        this.sortField || undefined, // sortField
        this.sortDirection // sortDirection
      ).toPromise();
      
      if (response?.success && response.data) {
        // Use the HR request details directly from server
        this.requests = response.data;
        this.filteredRequests = response.data;
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        
        console.log('HR Request Details loaded successfully:', this.requests);
      } else {
        this.errorMessage = response?.message || 'Failed to load HR request details';
        this.requests = [];
        this.filteredRequests = [];
        console.error('Failed to load HR request details:', response?.message);
      }
    } catch (error) {
      this.errorMessage = 'Error loading HR request details. Please try again later.';
      this.requests = [];
      this.filteredRequests = [];
      console.error('Error loading HR request details:', error);
    } finally {
      this.isLoading = false;
    }
  }

  onSearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm = target.value;
    // Emit the search term to the debounced subject
    this.searchSubject.next(this.searchTerm);
  }

  private async performSearch(searchTerm: string): Promise<void> {
    this.currentPage = 1;
    this.updateUrlParams();
    await this.loadHRRequests();
  }

  // Note: filterRequests is no longer needed since we use server-side search
  // but keeping it for potential fallback to client-side filtering if needed
  filterRequests(): void {
    // With server-side search, filteredRequests is set directly in loadHRRequests()
    this.filteredRequests = [...this.requests];
  }

  async changePage(delta: number): Promise<void> {
    const newPage = this.currentPage + delta;
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.currentPage = newPage;
      this.updateUrlParams();
      await this.loadHRRequests();
    }
  }

  async updatePageSize(event: Event): Promise<void> {
    const target = event.target as HTMLSelectElement;
    this.pageSize = parseInt(target.value);
    this.currentPage = 1;
    this.updateUrlParams();
    await this.loadHRRequests();
  }

  updatePagination(): void {
    // Server-side pagination - totalPages is set from API response
    // No additional logic needed since all pagination is handled server-side
  }

  get paginatedRequests(): ApiHRRequestDetailDto[] {
    // All pagination is now handled server-side, so return filteredRequests directly
    return this.filteredRequests;
  }

  get canGoPrevious(): boolean {
    return this.currentPage > 1;
  }

  get canGoNext(): boolean {
    return this.currentPage < this.totalPages;
  }

  openModal(): void {
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
  }

  createRequest(type: RequestType): void {
    this.closeModal();
    
    // If Return to Work is selected, call the API to get and cache request types
    if (type === 'return') {
      this.referenceDataService.getRequestTypeByNameWithCache('ReturnToWork').subscribe({
        next: (response) => {
          if (response.success && response.data) {
            console.log('ReturnToWork request types loaded and cached:', response.data);
          } else {
            console.error('Failed to load ReturnToWork request types:', response.message);
          }
        },
        error: (error) => {
          console.error('Error loading ReturnToWork request types:', error);
        }
      });
    }
    
    const option = this.requestTypeOptions.find(opt => opt.type === type);
    if (option) {
      this.router.navigate([option.route]);
    }
  }

  /**
   * Handle view action - routes to specific pages for RTW, Layoff, Termination, New Hire, and Promotion requests, regular view for others
   */
  handleViewAction(request: ApiHRRequestDetailDto): void {
    if (this.isReturnToWorkRequest(request.requestTypeName)) {
      this.openReturnToWorkPage(request);
    } else if (this.isLayoffRequest(request.requestTypeName)) {
      this.openLayoffPage(request);
    } else if (this.isTerminationRequest(request.requestTypeName)) {
      this.openTerminationPage(request);
    } else if (this.isNewHireRequest(request.requestTypeName)) {
      this.viewNewHireRequest(request);
    } else if (this.isPromotionRequest(request.requestTypeName)) {
      this.viewPromotionRequest(request);
    } else {
      this.viewRequest(request);
    }
  }

  viewRequest(request: ApiHRRequestDetailDto): void {
    console.log('Viewing request:', request);
    // TODO: Implement detailed view logic for non-RTW requests
  }

  /**
   * Check if a request is a return to work request
   */
  isReturnToWorkRequest(requestTypeName?: string): boolean {
    if (!requestTypeName) return false;
    
    const returnToWorkTypes = ['ReturnToWork', 'Return To Work', 'Return', 'RTW'];
    return returnToWorkTypes.some(type => 
      requestTypeName.toLowerCase().includes(type.toLowerCase())
    );
  }

  /**
   * Check if a request is a layoff request
   */
  isLayoffRequest(requestTypeName?: string): boolean {
    if (!requestTypeName) return false;
    
    const layoffTypes = ['Layoff', 'Lay Off', 'Lay-Off'];
    return layoffTypes.some(type => 
      requestTypeName.toLowerCase().includes(type.toLowerCase())
    );
  }

  isTerminationRequest(requestTypeName?: string): boolean {
    if (!requestTypeName) return false;

    const terminationTypes = ['Termination', 'Terminate', 'Term'];
    return terminationTypes.some(type =>
      requestTypeName.toLowerCase().includes(type.toLowerCase())
    );
  }

  /**
   * Check if a request is a new hire request
   */
  isNewHireRequest(requestTypeName?: string): boolean {
    if (!requestTypeName) return false;

    const newHireTypes = ['NewHire', 'New Hire', 'Hire', 'Hiring'];
    return newHireTypes.some(type =>
      requestTypeName.toLowerCase().includes(type.toLowerCase())
    );
  }

  /**
   * Check if a request is a promotion request
   */
  isPromotionRequest(requestTypeName?: string): boolean {
    if (!requestTypeName) return false;

    const promotionTypes = ['Promotion', 'Transfer', 'PromotionTransfer', 'Promotion/Transfer'];
    return promotionTypes.some(type =>
      requestTypeName.toLowerCase().includes(type.toLowerCase())
    );
  }

  /**
   * Open the return to work page with the parent HR request ID
   */
  openReturnToWorkPage(request: ApiHRRequestDetailDto): void {
    if (!request.parentRequestId) {
      console.error('No parent request ID found for return to work request:', request);
      return;
    }

    console.log('Opening return to work page for parent request ID:', request.parentRequestId);

    // Navigate to return-to-work page with parentId parameter
    this.router.navigate(['/return-to-work'], {
      queryParams: { parentId: request.parentRequestId }
    });
  }

  /**
   * Open the layoff page with the parent HR request ID
   */
  openLayoffPage(request: ApiHRRequestDetailDto): void {
    if (!request.parentRequestId) {
      console.error('No parent request ID found for layoff request:', request);
      return;
    }

    console.log('Opening layoff page for parent request ID:', request.parentRequestId);

    // Navigate to layoff page with parentId parameter
    this.router.navigate(['/layoff'], {
      queryParams: { parentId: request.parentRequestId }
    });
  }

  /**
   * Open the termination request page with the parent HR request ID
   */
  openTerminationPage(request: ApiHRRequestDetailDto): void {
    if (!request.parentRequestId) {
      console.error('No parent request ID found for termination request:', request);
      return;
    }

    console.log('Opening termination page for parent request ID:', request.parentRequestId);

    // Navigate to termination page with parentId parameter
    this.router.navigate(['/termination'], {
      queryParams: { parentId: request.parentRequestId }
    });
  }

  /**
   * View the new hire request details page with the parent HR request ID
   */
  viewNewHireRequest(request: ApiHRRequestDetailDto): void {
    if (!request.parentRequestId) {
      console.error('No parent request ID found for new hire request:', request);
      return;
    }

    console.log('Opening new hire view page for parent request ID:', request.parentRequestId);

    // Navigate to new hire view page with parentId parameter
    this.router.navigate(['/new-hire/view', request.parentRequestId]);
  }

  /**
   * Navigate to new hire page and focus on Work Phone Number field (ECM_ADMIN edit action)
   */
  handleEditPhoneAction(request: ApiHRRequestDetailDto): void {
    if (!request.parentRequestId) {
      console.error('No parent request ID found for new hire request:', request);
      return;
    }
    this.router.navigate(['/new-hire/view', request.parentRequestId], {
      queryParams: { focus: 'workPhoneNumber' }
    });
  }

  /**
   * View the promotion request details page with the parent HR request ID
   */
  viewPromotionRequest(request: ApiHRRequestDetailDto): void {
    if (!request.parentRequestId) {
      console.error('No parent request ID found for promotion request:', request);
      return;
    }

    console.log('Opening promotion view page for parent request ID:', request.parentRequestId);

    // Navigate to promotion view page with parentId parameter
    this.router.navigate(['/promotion/view', request.parentRequestId]);
  }

  formatRequestType(requestTypeName?: string): string {
    if (!requestTypeName) return 'Unknown';
    
    // Convert camelCase or PascalCase to space-separated words
    return requestTypeName
      .replace(/([a-z])([A-Z])/g, '$1 $2') // Add space before capital letters
      .replace(/^./, str => str.toUpperCase()); // Capitalize first letter
  }

  getRequestTypeClass(requestTypeName?: string): string {
    if (!requestTypeName) return '';
    
    // Map backend request type names to CSS class names
    const typeMap: { [key: string]: string } = {
      'NewHire': 'new-hire',
      'Promotion': 'promotion',
      'PromotionTransfer': 'promotion',
      'Transfer': 'transfer',
      'Layoff': 'layoff',
      'Termination': 'termination',
      'ReturnToWork': 'return',
      'Return': 'return'
    };
    
    return typeMap[requestTypeName] || requestTypeName.toLowerCase();
  }

  formatStatus(requestStatusName?: string): string {
    if (!requestStatusName) return 'Unknown';

    // Convert camelCase or PascalCase to space-separated words
    return requestStatusName
      .replace(/([a-z])([A-Z])/g, '$1 $2') // Add space before capital letters
      .replace(/^./, str => str.toUpperCase()); // Capitalize first letter
  }

  formatDisplayStatus(requestDisplayStatusName?: string, fallbackStatusName?: string): string {
    // Use displayStatusName if provided, otherwise derive from fallback
    const statusToFormat = requestDisplayStatusName || this.deriveDisplayStatusFromActual(fallbackStatusName);

    if (!statusToFormat) return 'Unknown';

    // Convert camelCase or PascalCase to space-separated words
    return statusToFormat
      .replace(/([a-z])([A-Z])/g, '$1 $2') // Add space before capital letters
      .replace(/^./, str => str.toUpperCase()); // Capitalize first letter
  }

  /**
   * Derive display status from actual status when display status is not available
   */
  private deriveDisplayStatusFromActual(actualStatus?: string): string {
    if (!actualStatus) return '';

    // Business logic: Draft stays Draft, everything else becomes Submitted
    return actualStatus.toLowerCase() === 'draft' ? 'Draft' : 'Submitted';
  }

  /**
   * Get CSS class for status based on status name from backend
   */
  getStatusClass(statusName?: string): string {
    if (!statusName) return 'status-default';
    return StatusColorHelper.getStatusClassName(statusName);
  }

  /**
   * Get CSS class for display status based on both display status and actual status
   */
  getDisplayStatusClass(displayStatusName?: string, actualStatusName?: string): string {
    return StatusColorHelper.getDisplayStatusClassName(displayStatusName, actualStatusName);
  }

  formatCompanyDisplay(request: ApiHRRequestDetailDto): string {
    if (request.employeeCompanyCode && request.companyName) {
      return `${request.employeeCompanyCode} - ${request.companyName}`;
    }
    return request.companyName || '';
  }

  formatDivisionDisplay(request: ApiHRRequestDetailDto): string {
    if (request.employeeDepartmentCode && request.departmentName) {
      return `${request.employeeDepartmentCode} - ${request.departmentName}`;
    }
    return request.departmentName || '';
  }

  formatDate(dateString?: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString();
  }

  /**
   * Handle column sorting
   */
  async sortColumn(field: string): Promise<void> {
    if (this.sortField === field) {
      // Toggle sort direction if same field is clicked
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      // Set new field and default to ascending
      this.sortField = field;
      this.sortDirection = 'asc';
    }
    
    // Reset to first page when sorting changes
    this.currentPage = 1;
    this.updateUrlParams();

    // Reload data with new sorting
    await this.loadHRRequests();
  }

  /**
   * Get sort indicator for a column
   */
  getSortIndicator(field: string): string {
    if (this.sortField === field) {
      return this.sortDirection === 'asc' ? '↑' : '↓';
    }
    return '↕';
  }

  /**
   * Check if a column is currently being sorted
   */
  isSortActive(field: string): boolean {
    return this.sortField === field;
  }

  private async checkUserAuthorization(): Promise<void> {
    const isAuthorized = await this.authService.checkUserAuthorization();
    if (!isAuthorized) {
      console.log('User not authorized for HR dashboard');
      this.router.navigate(['/unauthorized']);
    } else {
      console.log('User authorized for HR dashboard');
    }

    const userRoles = await this.authService.getUserRoles();
    this.isEcmAdmin = userRoles.some(role => role.toLowerCase() === 'ecm_admin');
  }

  /**
   * Check if a request is in completed status
   */
  isRequestCompleted(requestStatusName?: string): boolean {
    if (!requestStatusName) return false;
    
    const completedStatuses = ['Completed', 'Cancelled', 'Failed'];
    return completedStatuses.some(status => 
      requestStatusName.toLowerCase().includes(status.toLowerCase())
    );
  }

  /**
   * Check if a request has failed status
   */
  isRequestFailed(requestStatusName?: string): boolean {
    if (!requestStatusName) return false;

    const failedStatuses = ['Failed'];
    return failedStatuses.some(status =>
      requestStatusName.toLowerCase().includes(status.toLowerCase())
    );
  }

  /**
   * Check if a request is still in process (Pending or Processing status)
   */
  isRequestInProcess(requestStatusName?: string): boolean {
    if (!requestStatusName) return false;
    const status = requestStatusName.toLowerCase();
    return status.includes('pending') || status.includes('processing');
  }

  /**
   * Check if the effective date is today or in the past
   */
  isEffectiveDateTodayOrPast(effectiveDate?: string): boolean {
    if (!effectiveDate) return false;
    const effective = new Date(effectiveDate);
    const today = new Date();
    effective.setHours(0, 0, 0, 0);
    today.setHours(0, 0, 0, 0);
    return effective.getTime() <= today.getTime();
  }

  /**
   * Check if the action icon should show as edit (pencil) instead of view (eye)
   * True when user is ECM_ADMIN, request status is Pending or Processing, and Desk Phone is selected
   */
  shouldShowEditIcon(request: ApiHRRequestDetailDto): boolean {
    if (!this.isEcmAdmin || !request.requestStatusName || !request.hasDeskPhone) return false;
    const status = request.requestStatusName.toLowerCase();
    return status.includes('pending') || status.includes('processing');
  }

  /**
   * Refresh Viewpoint status for a specific HR request (only for non-completed requests)
   */
  /**
   * Initialize SignalR connection and set up notification handling
   */
  private async initializeSignalR(): Promise<void> {
    try {
      // Get access token from auth service
      const token = await this.authService.getAccessTokenPromise();
      if (!token) {
        console.warn('No access token available for SignalR connection');
        return;
      }

      // Start SignalR connection
      await this.signalRService.startConnection(token);

      // Note: No longer joining user groups since notifications are sent to all users

      // Subscribe to notifications
      this.signalRService.getNotifications()
        .pipe(takeUntil(this.destroy$))
        .subscribe(notification => {
          this.handleHRRequestNotification(notification);
        });

      console.log('SignalR initialized successfully');
    } catch (error) {
      console.error('Failed to initialize SignalR:', error);
      // Don't show error to user, just log it - SignalR is not critical for basic functionality
    }
  }

  /**
   * Handle incoming HR request notifications
   */
  private handleHRRequestNotification(notification: HRRequestNotification): void {
    console.log('🔔 SignalR Notification Received:', notification);
    console.log('📊 Notification Details:', {
      type: notification.type,
      hrRequestDetailId: notification.hrRequestDetailId,
      status: notification.status,
      employeeName: notification.employeeName,
      isSuccess: notification.isSuccess,
      message: notification.message,
      timestamp: notification.timestamp
    });

    // Create a unique key for this notification to prevent duplicates
    const notificationKey = `${notification.type}-${notification.hrRequestDetailId}-${notification.timestamp}`;
    
    // Check if we've already processed this notification
    if (this.processedNotifications.has(notificationKey)) {
      console.log('⚠️ Duplicate notification ignored:', notificationKey);
      return;
    }
    
    // Mark this notification as processed
    this.processedNotifications.add(notificationKey);

    if (notification.type === 'HRRequestStatusUpdate') {
      console.log('🔄 Processing HRRequestStatusUpdate notification');
      
      // Handle intermediate status updates (Pending, Processing, etc.)
      if (notification.status) {
        // Log specific status updates we're tracking
        if (notification.status === 'Processing') {
          console.log('⚙️ PROCESSING STATUS UPDATE received for request:', notification.hrRequestDetailId);
        } else if (notification.status === 'Pending') {
          console.log('⏳ PENDING STATUS UPDATE received for request:', notification.hrRequestDetailId);
        } else {
          console.log(`📝 STATUS UPDATE to ${notification.status} received for request:`, notification.hrRequestDetailId);
        }
        
        this.updateRequestInTable(notification.hrRequestDetailId, notification.status, notification.displayStatusName);
        
        // Show subtle notification for status changes
        this.toasterService.showInfo(
          `${notification.employeeName} - ${notification.message || 'Status updated'}`,
          `Status: ${notification.status}`
        );
      } else {
        console.log('⚠️ HRRequestStatusUpdate notification missing status property');
      }
      
    } else if (notification.type === 'HRRequestCompletion') {
      console.log('✅ Processing HRRequestCompletion notification');
      
      // Handle final status updates (Completed, Failed)
      if (notification.isSuccess) {
        console.log('🎉 COMPLETED STATUS UPDATE received for request:', notification.hrRequestDetailId);
        this.toasterService.showSuccess(
          `${notification.employeeName} status update completed successfully`,
          'Status Update Complete'
        );
        this.updateRequestInTable(notification.hrRequestDetailId, 'Completed', notification.displayStatusName);
      } else {
        console.log('❌ FAILED STATUS UPDATE received for request:', notification.hrRequestDetailId, 'Error:', notification.message);
        this.toasterService.showError(
          `${notification.employeeName} status update failed: ${notification.message}`,
          'Status Update Failed'
        );
        this.updateRequestInTable(notification.hrRequestDetailId, 'Failed', notification.displayStatusName);
      }
    } else {
      console.log('❓ Unknown notification type received:', notification.type);
    }
  }

  /**
   * Update a specific request in the table
   */
  private updateRequestInTable(hrRequestDetailId: number, newStatus: string, newDisplayStatus?: string): void {
    console.log(`🔄 Attempting to update request ${hrRequestDetailId} to status ${newStatus}, displayStatus: ${newDisplayStatus}`);
    console.log('📋 Current requests:', this.requests.map(r => ({ id: r.id, status: r.requestStatusName, displayStatus: r.requestDisplayStatusName })));

    // Get the status ID for the new status
    const statusId = this.getStatusIdByName(newStatus);

    // Find and update the request in the current data
    const requestIndex = this.requests.findIndex(r => r.id === hrRequestDetailId);
    if (requestIndex !== -1) {
      console.log(`✅ Found request at index ${requestIndex}, updating status from ${this.requests[requestIndex].requestStatusName} to ${newStatus}`);

      // Update status ID, name, and display status
      this.requests[requestIndex].requestStatusId = statusId;
      this.requests[requestIndex].requestStatusName = newStatus;

      // Update display status if provided, otherwise derive it
      if (newDisplayStatus) {
        this.requests[requestIndex].requestDisplayStatusName = newDisplayStatus;
      } else {
        // Derive display status based on business logic
        this.requests[requestIndex].requestDisplayStatusName = this.deriveDisplayStatus(newStatus);
      }

      // Also update filtered requests if they're different
      const filteredIndex = this.filteredRequests.findIndex(r => r.id === hrRequestDetailId);
      if (filteredIndex !== -1) {
        this.filteredRequests[filteredIndex].requestStatusId = statusId;
        this.filteredRequests[filteredIndex].requestStatusName = newStatus;
        this.filteredRequests[filteredIndex].requestDisplayStatusName = this.requests[requestIndex].requestDisplayStatusName;
        console.log(`✅ Also updated filtered request at index ${filteredIndex}`);
      }

      console.log(`🎉 Successfully updated request ${hrRequestDetailId} status to ${newStatus}, displayStatus: ${this.requests[requestIndex].requestDisplayStatusName} (ID: ${statusId})`);

      // Force Angular change detection
      this.requests = [...this.requests];
      this.filteredRequests = [...this.filteredRequests];
    } else {
      console.warn(`⚠️ Request with ID ${hrRequestDetailId} not found in current requests list`);
    }
  }

  /**
   * Get status ID by status name
   */
  private getStatusIdByName(statusName: string): number {
    const statusMap: { [key: string]: number } = {
      'Pending': 1,
      'Processing': 2,
      'Completed': 3,
      'Failed': 4,
      'Cancelled': 5
    };

    return statusMap[statusName] || 1; // Default to Pending if unknown status
  }

  /**
   * Derive display status from actual status based on business logic
   */
  private deriveDisplayStatus(actualStatus: string): string {
    return this.deriveDisplayStatusFromActual(actualStatus);
  }


  /**
   * Retry a failed HR request
   */
  async retryFailedRequest(request: ApiHRRequestDetailDto): Promise<void> {
    if (!request.id) {
      console.error('No request ID found for retry:', request);
      return;
    }

    // Validate that request has failed
    if (!this.isRequestFailed(request.requestStatusName)) {
      console.warn('Cannot retry request that has not failed:', request.requestStatusName);
      return;
    }

    try {
      const response = await this.hrRequestService.retryHRRequestDetail(request.id).toPromise();

      if (response?.success) {
        console.log('Failed request retry initiated successfully:', response.data);
        this.toasterService.showSuccess('Failed request retry initiated successfully', 'Retry Initiated');

        // Don't update UI immediately - wait for SignalR notification to update status
      } else {
        console.error('Failed to retry request:', response?.message);
        this.toasterService.showError(`Failed to retry request: ${response?.message}`, 'Retry Failed');
      }
    } catch (error: any) {
      console.error('Error retrying failed request:', error);
      const errorMessage = error?.error?.message || error?.message || 'Unknown error occurred';
      this.toasterService.showError(`Error retrying request: ${errorMessage}`, 'Retry Error');
    }
  }

  /**
   * Check if cancel button should be shown for a request
   * Cancel button is shown if:
   * - Request type is New Hire
   * - Effective date is today or within past 7 days
   * - Status is Pending, Processing, Failed, or Draft
   */
  canShowCancelButton(request: ApiHRRequestDetailDto): boolean {
    // Check if it's a new hire request
    if (!this.isNewHireRequest(request.requestTypeName)) {
      return false;
    }

    // Check if status is eligible for cancellation
    const eligibleStatuses = ['Pending', 'Processing', 'Failed', 'Draft'];
    if (!eligibleStatuses.includes(request.requestStatusName || '')) {
      return false;
    }

    // Check if effective date is today or within past 7 days
    if (!request.effectiveDate) {
      return false;
    }

    const effectiveDate = new Date(request.effectiveDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    effectiveDate.setHours(0, 0, 0, 0);

    const daysDifference = Math.floor((today.getTime() - effectiveDate.getTime()) / (1000 * 60 * 60 * 24));

    // Return true if today or within past 7 days (0 to 7 days)
    return daysDifference >= 0 && daysDifference <= 7;
  }

  /**
   * Cancel an HR request - Shows confirmation dialog first
   */
  cancelRequest(request: ApiHRRequestDetailDto): void {
    if (!request.id) {
      console.error('No request ID found for cancel:', request);
      return;
    }

    // Store the request for later use when confirmed
    this.pendingCancelRequest = request;

    // Show the confirmation dialog
    this.showCancelConfirmDialog = true;
  }

  /**
   * Handle cancel confirmation - called when user confirms in the dialog
   */
  async onCancelConfirmed(): Promise<void> {
    if (!this.pendingCancelRequest?.id) {
      console.error('No pending cancel request');
      return;
    }

    const request = this.pendingCancelRequest;
    this.showCancelConfirmDialog = false;

    try {
      const response = await this.hrRequestService.cancelHRRequestDetail(request.id).toPromise();

      if (response?.success) {
        console.log('HR request cancelled successfully:', response.data);
        this.toasterService.showSuccess('HR request cancelled successfully', 'Request Cancelled');

        // Update the request in the UI
        this.updateRequestInTable(request.id, 'Cancelled', 'Cancelled');
      } else {
        console.error('Failed to cancel request:', response?.message);
        this.toasterService.showError(`Failed to cancel request: ${response?.message}`, 'Cancel Failed');
      }
    } catch (error: any) {
      console.error('Error cancelling request:', error);
      const errorMessage = error?.error?.message || error?.message || 'Unknown error occurred';
      this.toasterService.showError(`Error cancelling request: ${errorMessage}`, 'Cancel Error');
    } finally {
      this.pendingCancelRequest = null;
    }
  }

  /**
   * Handle cancel dialog closed or cancelled
   */
  onCancelDialogClosed(): void {
    this.showCancelConfirmDialog = false;
    this.pendingCancelRequest = null;
  }

}