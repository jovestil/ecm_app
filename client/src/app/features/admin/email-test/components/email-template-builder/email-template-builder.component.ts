import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { EmailTemplateService, NewHireEmailData } from '../../../../../core/services/email-template.service';
import { ToasterService } from '../../../../../core/services/toaster.service';

@Component({
  selector: 'app-email-template-builder',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './email-template-builder.component.html',
  styleUrls: ['./email-template-builder.component.css']
})
export class EmailTemplateBuilderComponent implements OnInit {
  @Output() templateDataGenerated = new EventEmitter<NewHireEmailData>();

  templateForm: FormGroup;
  generatedHtml: string = '';
  showPreview: boolean = false;

  constructor(
    private fb: FormBuilder,
    private emailTemplateService: EmailTemplateService,
    private toasterService: ToasterService
  ) {
    this.templateForm = this.createForm();
  }

  ngOnInit(): void {
    // Pre-fill with sample data for testing
    this.loadSampleData();
  }

  private createForm(): FormGroup {
    return this.fb.group({
      startDate: ['', Validators.required],
      newEmployeeName: ['', Validators.required],
      company: ['', Validators.required],
      division: ['', Validators.required],
      position: ['', Validators.required],
      salaryCode: ['Salaried', Validators.required],
      requestCreatedBy: ['', Validators.required],
      employmentStatus: ['Full-Time', Validators.required],
      byod: ['No', Validators.required],
      rehire: ['N', Validators.required]
    });
  }

  private loadSampleData(): void {
    const today = new Date();
    const futureDate = new Date(today);
    futureDate.setDate(today.getDate() + 14);

    this.templateForm.patchValue({
      startDate: futureDate.toISOString().split('T')[0],
      newEmployeeName: 'John Doe',
      company: 'Mathy Construction Company',
      division: 'Main Office - Minneapolis',
      position: 'Project Manager',
      salaryCode: 'Salaried',
      requestCreatedBy: 'Jane Smith (HR Manager)',
      employmentStatus: 'Full-Time',
      byod: 'No',
      rehire: 'N'
    });
  }

  onGenerateTemplate(): void {
    if (this.templateForm.valid) {
      const data: NewHireEmailData = this.templateForm.value;
      this.generatedHtml = this.emailTemplateService.generateNewHireEmailHtml(data);
      this.showPreview = true;
      this.toasterService.showSuccess('Email template generated successfully');
    } else {
      this.markFormGroupTouched();
      this.toasterService.showError('Please fill in all required fields');
    }
  }

  onUseTemplate(): void {
    if (this.templateForm.valid) {
      const data: NewHireEmailData = this.templateForm.value;
      this.templateDataGenerated.emit(data);
      this.toasterService.showSuccess('Template data sent to form');
    } else {
      this.toasterService.showWarning('Please fill in all required fields first');
    }
  }

  onCopyToClipboard(): void {
    if (this.generatedHtml) {
      navigator.clipboard.writeText(this.generatedHtml).then(() => {
        this.toasterService.showSuccess('HTML template copied to clipboard');
      }).catch(() => {
        this.toasterService.showError('Failed to copy to clipboard');
      });
    }
  }

  onClearForm(): void {
    this.templateForm.reset({
      salaryCode: 'Salaried',
      employmentStatus: 'Full-Time',
      byod: 'No',
      rehire: 'N'
    });
    this.generatedHtml = '';
    this.showPreview = false;
  }

  private markFormGroupTouched(): void {
    Object.keys(this.templateForm.controls).forEach(key => {
      const control = this.templateForm.get(key);
      control?.markAsTouched();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.templateForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.templateForm.get(fieldName);
    if (field && field.errors && (field.dirty || field.touched)) {
      if (field.errors['required']) {
        return `${this.getFieldDisplayName(fieldName)} is required`;
      }
    }
    return '';
  }

  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      startDate: 'Start Date',
      newEmployeeName: 'New Employee Name',
      company: 'Company',
      division: 'Division',
      position: 'Position',
      salaryCode: 'Hourly/Salaried',
      requestCreatedBy: 'Request Created By',
      employmentStatus: 'Employment Status',
      byod: 'BYOD',
      rehire: 'Rehire'
    };
    return displayNames[fieldName] || fieldName;
  }
}
