import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppHeaderComponent } from '../../../shared/app-header/app-header.component';
import { BackToHomepageButtonComponent } from '../../../shared/back-to-homepage-button/back-to-homepage-button.component';
import { ReferenceDataService, CompanyDto, PayrollDepartmentDto } from '../../../core/services/reference-data.service';
import { HRRequestService } from '../../../core/services/hr-request.service';
import { ToasterService } from '../../../core/services/toaster.service';

interface CreateADUserRequest {
  companyCode: number;
  payrollDeptCode: number;
  preferredFirstName?: string;
  firstName: string;
  lastName: string;
  title?: string;
  department?: string;
}

interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

@Component({
  selector: 'app-ad-user-test',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AppHeaderComponent,
    BackToHomepageButtonComponent
  ],
  templateUrl: './ad-user-test.component.html',
  styleUrls: ['./ad-user-test.component.css', '../../../shared/styles/common.css']
})
export class AdUserTestComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  adUserForm: FormGroup;
  companies: CompanyDto[] = [];
  payrollDepartments: PayrollDepartmentDto[] = [];
  isLoading = false;
  isSubmitting = false;

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private referenceDataService: ReferenceDataService,
    private hrRequestService: HRRequestService,
    private toasterService: ToasterService
  ) {
    this.adUserForm = this.createForm();
  }

  ngOnInit(): void {
    this.loadCompanies();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private createForm(): FormGroup {
    return this.fb.group({
      companyCode: ['', [Validators.required]],
      payrollDeptCode: ['', [Validators.required]],
      preferredFirstName: ['', [Validators.maxLength(100)]],
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      title: ['', [Validators.maxLength(100)]],
      department: ['', [Validators.maxLength(100)]]
    });
  }

  private loadCompanies(): void {
    this.isLoading = true;
    this.referenceDataService.getCompanies()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.companies = response.data;
          } else {
            this.toasterService.showError('Failed to load companies');
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error loading companies:', error);
          this.toasterService.showError('Failed to load companies');
          this.isLoading = false;
        }
      });
  }

  onCompanyChange(): void {
    const companyCode = parseInt(this.adUserForm.get('companyCode')?.value);
    this.adUserForm.get('payrollDeptCode')?.setValue('');
    this.payrollDepartments = [];

    if (companyCode) {
      this.referenceDataService.getPayrollDepartmentsByCompanyWithCache(companyCode)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            if (response.success && response.data) {
              this.payrollDepartments = response.data;
            }
          },
          error: (error) => {
            console.error('Error loading payroll departments:', error);
          }
        });
    }
  }

  onSubmit(): void {
    if (this.adUserForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;

      const formData = this.adUserForm.value;
      const request: CreateADUserRequest = {
        companyCode: parseInt(formData.companyCode),
        payrollDeptCode: parseInt(formData.payrollDeptCode),
        preferredFirstName: formData.preferredFirstName || undefined,
        firstName: formData.firstName,
        lastName: formData.lastName,
        title: formData.title || undefined,
        department: formData.department || undefined
      };

      this.createADUser(request);
    } else {
      this.markFormGroupTouched();
    }
  }

  private createADUser(request: CreateADUserRequest): void {
    this.hrRequestService.createADUser(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: ApiResponse<boolean>) => {
          if (response.success) {
            this.toasterService.showSuccess(response.message || 'AD user created successfully');
            this.adUserForm.reset();
          } else {
            this.toasterService.showError(response.message || 'Failed to create AD user');
          }
          this.isSubmitting = false;
        },
        error: (error) => {
          console.error('Error creating AD user:', error);
          this.toasterService.showError('An error occurred while creating AD user');
          this.isSubmitting = false;
        }
      });
  }

  private markFormGroupTouched(): void {
    Object.keys(this.adUserForm.controls).forEach(key => {
      const control = this.adUserForm.get(key);
      control?.markAsTouched();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.adUserForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.adUserForm.get(fieldName);
    if (field && field.errors && (field.dirty || field.touched)) {
      if (field.errors['required']) {
        return `${this.getFieldDisplayName(fieldName)} is required`;
      }
      if (field.errors['maxlength']) {
        return `${this.getFieldDisplayName(fieldName)} is too long`;
      }
    }
    return '';
  }

  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      companyCode: 'Company',
      payrollDeptCode: 'Payroll Department',
      preferredFirstName: 'Preferred First Name',
      firstName: 'First Name',
      lastName: 'Last Name',
      title: 'Title',
      department: 'Department'
    };
    return displayNames[fieldName] || fieldName;
  }

  goBack(): void {
    this.router.navigate(['/']);
  }
}