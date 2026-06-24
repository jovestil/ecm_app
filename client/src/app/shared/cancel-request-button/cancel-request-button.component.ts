import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ConfirmationDialogComponent, ConfirmationDialogConfig } from '../confirmation-dialog/confirmation-dialog.component';
import { ReferenceDataService } from '../../core/services/reference-data.service';
import { HRRequestService } from '../../core/services/hr-request.service';
import { ToasterService } from '../../core/services/toaster.service';
import { UpdateHRRequestDetailDto } from '../../models/api-hr-request.model';

// Request Status Constants
const REQUEST_STATUS = {
  PENDING: 1,
  FAILED: 4
} as const;

@Component({
  selector: 'app-cancel-request-button',
  standalone: true,
  imports: [CommonModule, ConfirmationDialogComponent],
  templateUrl: './cancel-request-button.component.html',
  styleUrls: ['./cancel-request-button.component.css']
})
export class CancelRequestButtonComponent implements OnInit {
  @Input() parentId: number | null = null;
  @Input() isEditMode: boolean = false;
  @Input() isCancelledRequest: boolean = false;
  @Input() requestType: string = 'request'; // e.g., 'termination request', 'request'
  @Input() isLoading: boolean = false;
  @Input() requestStatusId: number | null = null;
  
  @Output() cancelled = new EventEmitter<void>();
  @Output() loadingChange = new EventEmitter<boolean>();

  // Internal state
  showConfirmDialog: boolean = false;
  confirmDialogConfig: ConfirmationDialogConfig = {
    title: 'Cancel Request',
    message: 'Are you sure you want to cancel this request? This action cannot be undone.',
    confirmButtonText: 'Yes, Cancel Request',
    cancelButtonText: 'Keep Request',
    confirmButtonClass: 'btn-danger',
    cancelButtonClass: 'btn-secondary',
    showIcon: true,
    iconType: 'warning'
  };

  constructor(
    private router: Router,
    private referenceDataService: ReferenceDataService,
    private hrRequestService: HRRequestService,
    private toasterService: ToasterService
  ) {}

  ngOnInit(): void {
    // Update dialog message based on request type
    this.confirmDialogConfig.message = `Are you sure you want to cancel this ${this.requestType}? This action cannot be undone.`;
  }

  /**
   * Determines if the cancel button should be visible
   * Only visible for Pending (1) request status
   */
  get isVisible(): boolean {
    // Must be in edit mode
    if (!this.isEditMode) {
      return false;
    }

    // Must have a valid parent ID
    if (!this.parentId) {
      return false;
    }

    // If request is already cancelled, don't show
    if (this.isCancelledRequest) {
      return false;
    }

    // Only show for Pending (1) status
    if (this.requestStatusId === REQUEST_STATUS.PENDING) {
      return true;
    }

    // Default to hidden for any other status or null/undefined status
    return false;
  }

  showCancelConfirmation(): void {
    if (!this.parentId || !this.isEditMode) {
      console.error('Cannot cancel request: no parentId or not in edit mode');
      return;
    }
    this.showConfirmDialog = true;
  }

  onCancelConfirmed(): void {
    this.showConfirmDialog = false;
    this.performCancelRequest();
  }

  onCancelDialogClosed(): void {
    this.showConfirmDialog = false;
  }

  private setLoading(loading: boolean): void {
    this.isLoading = loading;
    this.loadingChange.emit(loading);
  }

  async performCancelRequest(): Promise<void> {
    this.setLoading(true);
    try {
      // First, get the 'Cancelled' status ID
      const statusResponse = await this.referenceDataService.getRequestStatusByName('Cancelled').toPromise();
      
      if (!statusResponse?.success || !statusResponse.data || statusResponse.data.length === 0) {
        throw new Error('Could not find Cancelled status');
      }

      const cancelledStatusId = statusResponse.data[0].id;
      console.log('Found Cancelled status ID:', cancelledStatusId);

      // Get all HR request details for this parent ID
      const detailsResponse = await this.hrRequestService.getHRRequestDetailsByParentId(this.parentId!).toPromise();
      
      if (!detailsResponse?.success || !detailsResponse.data) {
        throw new Error('Could not load request details');
      }

      console.log('Cancelling request details:', detailsResponse.data.length, 'details');

      // Cancel each request detail individually
      const cancelPromises = detailsResponse.data.map(detail => {
        const updateDto: UpdateHRRequestDetailDto = {
          id: detail.id,
          requestStatusId: cancelledStatusId,
          effectiveDate: detail.effectiveDate,
          processingNotes: detail.processingNotes,
          viewpointProcessed: detail.viewpointProcessed,
          viewpointErrorMessage: detail.viewpointErrorMessage
        };
        return this.hrRequestService.updateHRRequestDetail(detail.id, updateDto).toPromise();
      });

      // Wait for all updates to complete
      const results = await Promise.all(cancelPromises);
      
      // Check if all updates were successful
      const failedUpdates = results.filter(result => !result?.success);
      
      if (failedUpdates.length === 0) {
        console.log('All request details cancelled successfully');
        this.toasterService.showSuccess(
          `${this.requestType.charAt(0).toUpperCase() + this.requestType.slice(1)} cancelled successfully`,
          'Request Cancelled'
        );
        
        // Emit cancelled event
        this.cancelled.emit();
        
        // Navigate back to homepage after a short delay
        setTimeout(() => {
          this.router.navigate(['/']);
        }, 500);
      } else {
        throw new Error(`Failed to cancel ${failedUpdates.length} out of ${results.length} request details`);
      }
    } catch (error: any) {
      console.error('Error cancelling request:', error);
      
      let errorMessage = 'Error cancelling request. Please try again.';
      if (error.error?.message) {
        errorMessage = error.error.message;
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      this.toasterService.showError(errorMessage, 'Cancellation Error');
    } finally {
      this.setLoading(false);
    }
  }
}