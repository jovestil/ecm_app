// Personal Information
export interface NewHirePersonalInfo {
  employeeId?: number | null;
  firstName: string;
  lastName: string;
  middleInitial?: string | null;
  suffix?: string | null;
  preferredFirstName?: string | null;
  userId?: string | null;
  firstDayEmployment: Date | null; // Made nullable for draft functionality
  referredBy?: string | null;
  rehire: boolean;
}

// Position Information
export interface NewHirePositionInfo {
  companyCode: number | null; // Made nullable for draft functionality
  locationCode: number | null; // Made nullable for draft functionality
  employmentStatus: string | null; // Made nullable for draft functionality
  isUnion?: boolean | null;
  unionCraftId?: number | null;
  isApprentice?: boolean | null;
  isUnionWage?: boolean | null;
  salaryCode: number | null; // Made nullable for draft functionality
  positionCode: string | null; // Made nullable for draft functionality
  payrollDeptCode?: number | null;
  supervisorId?: number | null;
  appPercentage: string | null; // Made nullable for draft functionality
}

// IT Information
export interface NewHireITInfo {
  emailRequired: boolean;
  alternateDeliveryLocation?: string | null;
  msOfficeLicenseE5: boolean;
  msOfficeLicenseF3: boolean;
  emailAddress?: string | null;
}

// Credit Card Information
export interface NewHireCreditCardInfo {
  kwikTripCard: boolean;
  companyExpenseCard: boolean;
  creditExpenseType?: string | null;
  weeklyLimit?: number | null;
  fuelCardlockAccess: boolean;
  fuelCardlockAddress?: string | null;
}

// Vehicle Information
export interface NewHireVehicleInfo {
  isApprovedToOperate: boolean;
  driverClassification?: string | null;
  drugAndAlcoholProfile?: string | null;
  needCompanyCar: boolean;
  isApplicationPart2Complete: boolean;
}

// Phone Information
export interface NewHirePhoneInfo {
  deskPhone: boolean;
  companyCellphone: boolean;
  byodCellphone: boolean;
  workPhoneNumber?: string | null;
  workExtension?: string | null;
  reusingExistingPhone: boolean;
}

// Application Request
export interface NewHireApplicationRequest {
  applicationId: number;
  accessNotes?: string | null;
}

// Folder Request
export interface NewHireFolderRequest {
  folderType: string;
  folderName: string;
}

// Tablet Profile
export interface NewHireTabletProfile {
  tabletProfileId: number;
  tabletProfileName?: string | null;
  rolesRequiredForNewHire?: string | null;
}

// Computer Requirement
export interface NewHireComputerRequirement {
  computerRequirementsId: number;
  isChild: boolean;
  parentId?: number | null;
}

// Building Access
export interface NewHireBuildingAccess {
  accessId: number;
  accessDescription?: string | null;
}

// Main Create New Hire Request DTO
export interface CreateNewHireRequest {
  personalInfo: NewHirePersonalInfo;
  positionInfo: NewHirePositionInfo;
  itInfo?: NewHireITInfo;
  creditCardInfo?: NewHireCreditCardInfo;
  vehicleInfo?: NewHireVehicleInfo;
  phoneInfo?: NewHirePhoneInfo;
  applications?: NewHireApplicationRequest[];
  folders?: NewHireFolderRequest[];
  tabletProfiles?: NewHireTabletProfile[];
  computerRequirements?: NewHireComputerRequirement[];
  buildingAccess?: NewHireBuildingAccess[];
  useExistingKeyFob?: boolean;
  notes?: string | null;
}

// Response DTO for New Hire Request Details
export interface NewHireRequestDetail {
  id: number;
  requestDetailId: number;
  employeeId?: number;
  firstName: string;
  lastName: string;
  middleInitial?: string;
  suffix?: string;
  preferredFirstName?: string;
  userId?: string;
  firstDayEmployment: Date;
  referredBy?: string;
  rehire: boolean;
  companyCode: number;
  locationCode: number;
  employmentStatus: string;
  isUnion?: boolean;
  unionCraftId?: number;
  isApprentice?: boolean;
  isUnionWage?: boolean;
  salaryCode?: number;
  positionCode: string;
  payrollDeptCode?: number;
  supervisorId?: number;
  createdBy: number;
  createdDate: Date;
  modifiedBy?: number;
  modifiedDate?: Date;
  isDeleted: boolean;
}

// New Hire API Response
export interface NewHireApiResponse {
  hrRequest: any;
  hrRequestDetail: any;
  newHireDetail: NewHireRequestDetail;
}

// Comprehensive View DTOs (matching backend)
export interface NewHireRequestViewDto {
  // HR Request Information
  parentRequestId: number;
  requestTitle: string;
  requestDescription: string;
  effectiveDate: Date;
  notes?: string;
  createdDate: Date;
  requestStatusName: string;
  submittedByName: string;

  // HR Request Detail Information
  requestDetailId: number;
  employeeId?: number;
  employeeNetworkId: string;
  employeePositionCode: string;

  // Personal Information
  firstName: string;
  lastName: string;
  middleInitial?: string;
  suffix?: string;
  preferredFirstName?: string;
  userId?: string;
  firstDayEmployment: Date;
  referredBy?: string;
  rehire: boolean;

  // Position Information with Display Names
  companyCode: number;
  companyName: string;
  locationCode: number;
  locationName: string;
  employmentStatus: string;
  isUnion?: boolean;
  unionCraftId?: number;
  unionCraftDescription?: string;
  isApprentice?: boolean;
  isUnionWage?: boolean;
  salaryCode?: number;
  positionCode: string;
  positionName?: string;
  payrollDeptCode: number;
  payrollDeptName?: string;
  supervisorId: number;
  supervisorName?: string;
  appPercentage: string;

  // Related Information
  creditCardInfo?: CreditCardDetailViewDto;
  vehicleInfo?: VehicleDetailViewDto;
  itInfo?: ITDetailViewDto;
  phoneInfo?: ITPhoneRequirementViewDto;
  applications: ApplicationRequestViewDto[];
  folders: FolderRequestViewDto[];
  tabletProfiles: ITTabletProfileViewDto[];
  computerRequirements: ITComputerRequirementViewDto[];
  buildingAccess: NewHireBuildingAccessViewDto[];
  useExistingKeyFob?: boolean;
}

export interface CreditCardDetailViewDto {
  kwikTripCard: boolean;
  companyExpenseCard: boolean;
  creditExpenseType?: string;
  weeklyLimit?: number;
  fuelCardlockAccess: boolean;
  fuelCardlockAddress?: string;
}

export interface VehicleDetailViewDto {
  isApprovedToOperate: boolean;
  driverClassification?: string;
  drugAndAlcoholProfile?: string;
  needCompanyCar: boolean;
  isApplicationPart2Complete: boolean;
}

export interface ITDetailViewDto {
  emailRequired: boolean;
  alternateDeliveryLocation?: string;
  msOfficeLicenseE5: boolean;
  msOfficeLicenseF3: boolean;
  emailAddress?: string;
}

export interface ITPhoneRequirementViewDto {
  deskPhone: boolean;
  companyCellphone: boolean;
  byodCellphone: boolean;
  workPhoneNumber?: string;
  workExtension?: string;
  reusingExistingPhone: boolean;
}

export interface ApplicationRequestViewDto {
  applicationId: number;
  applicationName: string;
  accessNotes?: string;
}

export interface FolderRequestViewDto {
  folderType: string;
  folderName: string;
}

export interface ITTabletProfileViewDto {
  tabletProfileId: number;
  tabletProfileName: string;
  rolesRequiredForNewHire?: string;
}

export interface ITComputerRequirementViewDto {
  computerRequirementsId: number;
  computerRequirementsDescription: string;
  isChild?: boolean;
  parentId?: number;
}

export interface NewHireBuildingAccessViewDto {
  accessId: number;
  accessDescription: string;
}