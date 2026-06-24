import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { NewHireRequestComponent } from './new-hire-request.component';
import { AuthService } from '../../../core/services/auth.service';
import { ToasterService } from '../../../core/services/toaster.service';
import { ReferenceDataService } from '../../../core/services/reference-data.service';

describe('NewHireRequestComponent', () => {
  let component: NewHireRequestComponent;
  let fixture: ComponentFixture<NewHireRequestComponent>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockToasterService: jasmine.SpyObj<ToasterService>;
  let mockReferenceDataService: jasmine.SpyObj<ReferenceDataService>;

  beforeEach(async () => {
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    const authSpy = jasmine.createSpyObj('AuthService', ['getCurrentUser']);
    const toasterSpy = jasmine.createSpyObj('ToasterService', ['showSuccess', 'showError']);
    const referenceDataSpy = jasmine.createSpyObj('ReferenceDataService', ['getCompanies', 'getPhysicalLocations']);

    await TestBed.configureTestingModule({
      imports: [NewHireRequestComponent, ReactiveFormsModule],
      providers: [
        { provide: Router, useValue: routerSpy },
        { provide: AuthService, useValue: authSpy },
        { provide: ToasterService, useValue: toasterSpy },
        { provide: ReferenceDataService, useValue: referenceDataSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NewHireRequestComponent);
    component = fixture.componentInstance;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    mockAuthService = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    mockToasterService = TestBed.inject(ToasterService) as jasmine.SpyObj<ToasterService>;
    mockReferenceDataService = TestBed.inject(ReferenceDataService) as jasmine.SpyObj<ReferenceDataService>;
    
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with default values', () => {
    expect(component.newHireForm).toBeDefined();
    expect(component.activeTab).toBe('personal');
    expect(component.applicationSoftwareArray.length).toBe(1);
    expect(component.folderSharepointArray.length).toBe(1);
  });

  it('should switch tabs correctly', () => {
    component.showTab('it');
    expect(component.activeTab).toBe('it');
    
    component.showTab('personal');
    expect(component.activeTab).toBe('personal');
  });

  it('should add and remove application software rows', () => {
    const initialLength = component.applicationSoftwareArray.length;
    
    component.addApplicationRow();
    expect(component.applicationSoftwareArray.length).toBe(initialLength + 1);
    
    component.removeApplicationRow(1);
    expect(component.applicationSoftwareArray.length).toBe(initialLength);
  });

  it('should add and remove folder sharepoint rows', () => {
    const initialLength = component.folderSharepointArray.length;
    
    component.addFolderSharepointRow();
    expect(component.folderSharepointArray.length).toBe(initialLength + 1);
    
    component.removeFolderSharepointRow(1);
    expect(component.folderSharepointArray.length).toBe(initialLength);
  });

  it('should show union fields when Mathy Construction is selected', () => {
    component.newHireForm.patchValue({
      positionInfo: {
        company: '19'
      }
    });
    
    expect(component.showUnionFields).toBeTruthy();
  });

  it('should hide union fields when other company is selected', () => {
    component.newHireForm.patchValue({
      positionInfo: {
        company: '78'
      }
    });
    
    expect(component.showUnionFields).toBeFalsy();
  });

  it('should show expense card fields when company expense card is yes', () => {
    component.newHireForm.patchValue({
      creditCardInfo: {
        companyExpenseCard: 'yes'
      }
    });
    
    expect(component.showExpenseCardFields).toBeTruthy();
  });

  it('should show vehicle fields when approved vehicle is yes', () => {
    component.newHireForm.patchValue({
      vehicleInfo: {
        approvedVehicle: 'yes'
      }
    });
    
    expect(component.showVehicleFields).toBeTruthy();
  });

  it('should navigate back when goBack is called', () => {
    component.goBack();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should validate required fields', () => {
    expect(component.newHireForm.valid).toBeFalsy();
    
    // Fill required fields
    component.newHireForm.patchValue({
      personalInfo: {
        firstName: 'John',
        lastName: 'Doe',
        firstDay: '2024-01-01',
        rehire: 'no'
      },
      positionInfo: {
        company: '78',
        physicalLocation: 'location1',
        employmentStatus: 'full-time',
        salaryCode: 'hourly',
        position: 'developer',
        supervisor: 'supervisor1'
      },
      creditCardInfo: {
        kwikTripCard: 'no',
        companyExpenseCard: 'no',
        fuelCardlockAccess: 'no'
      },
      vehicleInfo: {
        approvedVehicle: 'no'
      },
      itInfo: {
        emailRequired: 'yes',
        reusingPhone: 'no',
        rolesRequiredNewHires: 'None'
      }
    });
    
    expect(component.newHireForm.valid).toBeTruthy();
  });

  it('should call onSubmit and show success message on valid form', () => {
    spyOn(component, 'goBack');
    
    // Make form valid
    component.newHireForm.patchValue({
      personalInfo: {
        firstName: 'John',
        lastName: 'Doe',
        firstDay: '2024-01-01',
        rehire: 'no'
      },
      positionInfo: {
        company: '78',
        physicalLocation: 'location1',
        employmentStatus: 'full-time',
        salaryCode: 'hourly',
        position: 'developer',
        supervisor: 'supervisor1'
      },
      creditCardInfo: {
        kwikTripCard: 'no',
        companyExpenseCard: 'no',
        fuelCardlockAccess: 'no'
      },
      vehicleInfo: {
        approvedVehicle: 'no'
      },
      itInfo: {
        emailRequired: 'yes',
        reusingPhone: 'no',
        rolesRequiredNewHires: 'None'
      }
    });
    
    component.onSubmit();
    
    expect(component.isLoading).toBeTruthy();
    
    // Simulate async completion
    setTimeout(() => {
      expect(mockToasterService.showSuccess).toHaveBeenCalledWith('New hire request submitted successfully!');
      expect(component.goBack).toHaveBeenCalled();
    }, 2100);
  });

  it('should show error message on invalid form submission', () => {
    component.onSubmit();
    expect(mockToasterService.showError).toHaveBeenCalledWith('Please fill in all required fields.');
  });
});