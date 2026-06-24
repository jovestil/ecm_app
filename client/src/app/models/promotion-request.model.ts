// Promotion Request DTOs matching backend CreatePromotionRequestDto structure

export interface CreatePromotionRequestDto {
  notes?: string;
  employeeId: number;
  // Current Position (nullable - for display only)
  currentPayrollCompanyCode?: number;
  currentPayrollGroupCode?: number;
  currentPayrollDeptCode?: number;
  currentPositionCode?: string;
  currentSupervisorId?: number;
  currentPhysicalLocationCode?: number;
  currentStatus?: string;
  currentSalaryCode?: number;
  currentWorkEmail?: string;
  // New Position (required for submit)
  newPayrollCompanyCode: number;
  newPayrollGroupCode: number;
  newPayrollDeptCode: number;
  newPositionCode: string;
  newSupervisorId?: number;
  newPhysicalLocationCode: number;
  newStatus: string;
  newSalaryCode?: number;
  newWorkEmail?: string;
  effectiveDate: string | Date;
  // Access Features (optional)
  creditCardInfo?: PromotionCreditCardInfoDto;
  vehicleInfo?: PromotionVehicleInfoDto;
  itInfo?: PromotionITInfoDto;
  phoneInfo?: PromotionPhoneRequirementDto;
  applications?: PromotionApplicationRequestDto[];
  folders?: PromotionFolderRequestDto[];
  tabletProfiles?: PromotionTabletProfileDto[];
  computerRequirements?: PromotionComputerRequirementDto[];
  buildingAccess?: PromotionBuildingAccessDto[];
  useExistingKeyFob?: boolean;
}

export interface PromotionCreditCardInfoDto {
  kwikTripCard?: boolean;
  companyExpenseCard?: boolean;
  creditExpenseType?: string;
  weeklyLimit?: number;
  fuelCardlockAccess?: boolean;
  fuelCardlockAddress?: string;
}

export interface PromotionVehicleInfoDto {
  isApprovedToOperate?: boolean;
  licenseClass?: string;
  drugAndAlcoholProfile?: string;
  needCompanyCar?: boolean;
  isApplicationPart2Complete?: boolean;
}

export interface PromotionITInfoDto {
  emailRequired?: boolean;
  alternateDeliveryLocation?: string;
  mSOfficeLicenseE5?: boolean;
  mSOfficeLicenseF3?: boolean;
}

export interface PromotionPhoneRequirementDto {
  deskPhone?: boolean;
  companyCellphone?: boolean;
  byodCellphone?: boolean;
  workPhoneNumber?: string;
  workExtension?: string;
  workCell?: string;
  reusingExistingPhone?: boolean;
}

export interface PromotionApplicationRequestDto {
  applicationId?: number;
  applicationName?: string;
}

export interface PromotionFolderRequestDto {
  folderType?: string;
  folderName?: string;
}

export interface PromotionTabletProfileDto {
  tabletProfileId?: number;
  tabletProfileName?: string;
  rolesRequiredForNewHire?: string;
}

export interface PromotionComputerRequirementDto {
  requirementId?: number;
  requirementName?: string;
}

export interface PromotionBuildingAccessDto {
  accessId: number;
  accessDescription: string;
}

export interface PromotionRequestDetailDto {
  id: number;
  // Current Position
  currentPayrollCompanyCode?: number;
  currentPayrollGroupCode?: number;
  currentPayrollDeptCode?: number;
  currentPositionCode?: string;
  currentSupervisorId?: number;
  currentPhysicalLocationCode?: number;
  currentStatus?: string;
  currentSalaryCode?: number;
  currentWorkEmail?: string;
  // New Position
  newPayrollCompanyCode: number;
  newPayrollGroupCode: number;
  newPayrollDeptCode: number;
  newPositionCode: string;
  newSupervisorId?: number;
  newPhysicalLocationCode: number;
  newStatus: string;
  newSalaryCode?: number;
  newWorkEmail?: string;
}
