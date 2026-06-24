import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AppHeaderComponent } from '../../../shared/app-header/app-header.component';
import { HRRequestService } from '../../../core/services/hr-request.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { EmailTemplateBuilderComponent } from './components/email-template-builder/email-template-builder.component';
import { NewHireEmailData } from '../../../core/services/email-template.service';

interface EmailNotificationDto {
  toEmail: string;
  ccEmail?: string;
  subject: string;
  body: string;
  requestId?: number;
  templateId?: number;
  notificationType: string;
  priority: number;
  templateData?: { [key: string]: string };
  module?: string;
  trigger?: string;
}

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

@Component({
  selector: 'app-email-test',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AppHeaderComponent,
    EmailTemplateBuilderComponent
  ],
  templateUrl: './email-test.component.html',
  styleUrls: ['./email-test.component.css', '../../../shared/styles/common.css']
})
export class EmailTestComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  emailForm: FormGroup;
  isSubmitting = false;
  isSendingBulk = false;
  isCheckingQueue = false;

  // Tab management
  activeTab: 'builder' | 'manual' | 'preview' = 'manual';

  // Recipients management
  recipientEmails: string[] = [];
  newRecipientEmail: string = '';

  // Template data from builder
  templateData: NewHireEmailData | null = null;

  notificationTypes = [
    { value: 'Confirmation', label: 'Confirmation' },
    { value: 'Task', label: 'Task' },
    { value: 'Reminder', label: 'Reminder' },
    { value: 'Welcome', label: 'Welcome' },
    { value: 'Draft', label: 'Draft' },
    { value: 'Test', label: 'Test' }
  ];

  priorities = [
    { value: 1, label: '1 - High' },
    { value: 2, label: '2 - Normal' },
    { value: 3, label: '3 - Low' }
  ];

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private hrRequestService: HRRequestService,
    private toasterService: ToasterService,
    private sanitizer: DomSanitizer
  ) {
    this.emailForm = this.createForm();
  }

  ngOnInit(): void {
    // Component initialization
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createForm(): FormGroup {
    return this.fb.group({
      toEmail: ['', [Validators.required]],
      ccEmail: ['', [Validators.email]],
      subject: ['Test Email from ELM System', [Validators.required, Validators.maxLength(200)]],
      body: ['This is a test email sent from the ELM HR Management System via Azure Service Bus.\n\nThis email is queued in Azure Service Bus and will be processed by Power Automate to send the actual email.\n\nTimestamp: ' + new Date().toISOString(), [Validators.required, Validators.maxLength(1000)]],
      notificationType: ['Test', [Validators.required]],
      priority: [2, [Validators.required]],
      requestId: [''],
      templateId: [''],
      module: ['EmailTest'],
      trigger: ['ManualTest']
    });
  }

  onSendSingle(): void {
    console.log('=== onSendSingle() called ===');
    console.log('Form valid:', this.emailForm.valid);
    console.log('Form value:', this.emailForm.value);
    console.log('isSubmitting:', this.isSubmitting);

    // Log validation errors for each control
    Object.keys(this.emailForm.controls).forEach(key => {
      const control = this.emailForm.get(key);
      if (control && control.invalid) {
        console.log(`Field "${key}" is invalid:`, control.errors);
      }
    });

    if (this.emailForm.valid && !this.isSubmitting) {
      console.log('Form is valid, proceeding with submission...');
      this.isSubmitting = true;

      const formData = this.emailForm.value;
      const request: EmailNotificationDto = {
        toEmail: formData.toEmail,
        ccEmail: formData.ccEmail || undefined,
        subject: formData.subject,
        body: formData.body,
        notificationType: formData.notificationType,
        priority: parseInt(formData.priority),
        requestId: formData.requestId ? parseInt(formData.requestId) : undefined,
        templateId: formData.templateId ? parseInt(formData.templateId) : undefined,

        // Add template data if available from builder
        templateData: this.templateData ? {
          'StartDate': this.templateData.startDate,
          'NewEmployeeName': this.templateData.newEmployeeName,
          'Company': this.templateData.company,
          'Division': this.templateData.division,
          'Position': this.templateData.position,
          'HourlySalaried': this.templateData.salaryCode,
          'RequestCreatedBy': this.templateData.requestCreatedBy,
          'EmploymentStatus': this.templateData.employmentStatus,
          'BYOD': this.templateData.byod,
          'Rehire': this.templateData.rehire
        } : undefined,

        module: formData.module || undefined,
        trigger: formData.trigger || undefined
      };

      console.log('Request object:', request);
      this.sendSingleEmail(request);
    } else {
      console.log('Form is INVALID or already submitting. Marking fields as touched.');
      this.markFormGroupTouched();
    }
  }

  debugFormState(): void {
    console.log('=== FORM DEBUG STATE ===');
    console.log('Form valid:', this.emailForm.valid);
    console.log('Form touched:', this.emailForm.touched);
    console.log('Form dirty:', this.emailForm.dirty);
    console.log('Form value:', this.emailForm.value);
    console.log('Recipient emails array:', this.recipientEmails);
    console.log('Template data from builder:', this.templateData);

    console.log('\n=== FIELD VALIDATION ===');
    Object.keys(this.emailForm.controls).forEach(key => {
      const control = this.emailForm.get(key);
      console.log(`${key}:`, {
        value: control?.value,
        valid: control?.valid,
        errors: control?.errors,
        touched: control?.touched,
        dirty: control?.dirty
      });
    });
  }

  private sendSingleEmail(request: EmailNotificationDto): void {
    console.log('=== sendSingleEmail() called ===');
    console.log('Sending request to API:', request);

    this.hrRequestService.sendTestEmail(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: ApiResponse<boolean>) => {
          console.log('API Response received:', response);
          if (response.success) {
            this.toasterService.showSuccess(response.message || 'Email queued successfully in Azure Service Bus');
          } else {
            this.toasterService.showError(response.message || 'Failed to queue email');
            if (response.errors && response.errors.length > 0) {
              response.errors.forEach(error => this.toasterService.showError(error));
            }
          }
          this.isSubmitting = false;
        },
        error: (error) => {
          console.error('=== API ERROR ===');
          console.error('Error sending test email:', error);
          console.error('Error details:', {
            status: error.status,
            statusText: error.statusText,
            message: error.message,
            error: error.error
          });
          this.toasterService.showError('An error occurred while queueing email');
          this.isSubmitting = false;
        }
      });
  }

  onSendBulk(): void {
    if (this.emailForm.valid && !this.isSendingBulk) {
      this.isSendingBulk = true;

      const formData = this.emailForm.value;
      const baseEmail: EmailNotificationDto = {
        toEmail: formData.toEmail,
        ccEmail: formData.ccEmail || undefined,
        subject: formData.subject,
        body: formData.body,
        notificationType: formData.notificationType,
        priority: parseInt(formData.priority),
        requestId: formData.requestId ? parseInt(formData.requestId) : undefined,
        templateId: formData.templateId ? parseInt(formData.templateId) : undefined,
        module: formData.module || undefined,
        trigger: formData.trigger || undefined
      };

      // Create 5 test emails with different priorities and subjects
      const bulkRequests: EmailNotificationDto[] = [
        { ...baseEmail, subject: `${baseEmail.subject} - High Priority`, priority: 1, notificationType: 'Test' },
        { ...baseEmail, subject: `${baseEmail.subject} - Normal Priority #1`, priority: 2, notificationType: 'Confirmation' },
        { ...baseEmail, subject: `${baseEmail.subject} - Normal Priority #2`, priority: 2, notificationType: 'Task' },
        { ...baseEmail, subject: `${baseEmail.subject} - Low Priority #1`, priority: 3, notificationType: 'Reminder' },
        { ...baseEmail, subject: `${baseEmail.subject} - Low Priority #2`, priority: 3, notificationType: 'Welcome' }
      ];

      this.sendBulkEmails(bulkRequests);
    } else {
      this.markFormGroupTouched();
    }
  }

  private sendBulkEmails(requests: EmailNotificationDto[]): void {
    this.hrRequestService.sendBulkTestEmails(requests)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: ApiResponse<number>) => {
          if (response.success) {
            this.toasterService.showSuccess(`${response.data}/${requests.length} emails queued successfully in Azure Service Bus`);
          } else {
            this.toasterService.showWarning(`Only ${response.data}/${requests.length} emails were queued. Check logs for details.`);
            if (response.errors && response.errors.length > 0) {
              response.errors.forEach(error => this.toasterService.showError(error));
            }
          }
          this.isSendingBulk = false;
        },
        error: (error) => {
          console.error('Error sending bulk test emails:', error);
          this.toasterService.showError('An error occurred while queueing bulk emails');
          this.isSendingBulk = false;
        }
      });
  }

  onCheckQueue(): void {
    if (!this.isCheckingQueue) {
      this.isCheckingQueue = true;

      this.hrRequestService.checkEmailQueueStatus()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response: ApiResponse<boolean>) => {
            if (response.success && response.data) {
              this.toasterService.showSuccess(response.message || 'Azure Service Bus queue is ready');
            } else {
              this.toasterService.showWarning(response.message || 'Queue status could not be verified');
            }
            this.isCheckingQueue = false;
          },
          error: (error) => {
            console.error('Error checking queue status:', error);
            this.toasterService.showError('An error occurred while checking queue status');
            this.isCheckingQueue = false;
          }
        });
    }
  }

  private markFormGroupTouched(): void {
    Object.keys(this.emailForm.controls).forEach(key => {
      const control = this.emailForm.get(key);
      control?.markAsTouched();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.emailForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.emailForm.get(fieldName);
    if (field && field.errors && (field.dirty || field.touched)) {
      if (field.errors['required']) {
        return `${this.getFieldDisplayName(fieldName)} is required`;
      }
      if (field.errors['email']) {
        return `${this.getFieldDisplayName(fieldName)} must be a valid email address`;
      }
      if (field.errors['maxlength']) {
        return `${this.getFieldDisplayName(fieldName)} is too long`;
      }
    }
    return '';
  }

  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      toEmail: 'To Email',
      ccEmail: 'CC Email',
      subject: 'Subject',
      body: 'Body',
      notificationType: 'Notification Type',
      priority: 'Priority',
      requestId: 'Request ID',
      templateId: 'Template ID',
      module: 'Module',
      trigger: 'Trigger'
    };
    return displayNames[fieldName] || fieldName;
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  // Tab management
  setActiveTab(tab: 'builder' | 'manual' | 'preview'): void {
    this.activeTab = tab;
  }

  // Recipients management
  addRecipient(): void {
    const email = this.newRecipientEmail.trim();
    if (email && this.isValidEmail(email)) {
      if (!this.recipientEmails.includes(email)) {
        this.recipientEmails.push(email);
        this.newRecipientEmail = '';
        this.updateToEmailField();
      } else {
        this.toasterService.showWarning('This email is already in the recipient list');
      }
    } else {
      this.toasterService.showError('Please enter a valid email address');
    }
  }

  removeRecipient(email: string): void {
    this.recipientEmails = this.recipientEmails.filter(e => e !== email);
    this.updateToEmailField();
  }

  private updateToEmailField(): void {
    // Update the form control with comma-separated emails
    this.emailForm.patchValue({
      toEmail: this.recipientEmails.join(', ')
    });
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  onRecipientKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.addRecipient();
    }
  }

  // Template integration
  onTemplateDataGenerated(data: NewHireEmailData): void {
    console.log('Template data received:', data);

    // Store the data for use in request
    this.templateData = data;

    // Set body to simple text (Power Automate will generate HTML)
    this.emailForm.patchValue({
      subject: 'New Hire Request Confirmation',
      body: 'New Hire Request - Template data will be processed by Power Automate to generate HTML email.',
      module: 'NewHire',
      trigger: 'NewHireRequest',
      notificationType: 'Confirmation'
    });

    this.toasterService.showSuccess('Template data captured. Email will be generated by Power Automate.');
    this.activeTab = 'manual';
  }

  // HTML preview
  getPreviewHtml(): SafeHtml {
    const bodyContent = this.emailForm.get('body')?.value || '';
    return this.sanitizer.bypassSecurityTrustHtml(bodyContent);
  }
}
