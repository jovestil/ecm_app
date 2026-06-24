/**
 * Return-to-work specific models and interfaces
 */

import { Employee, SortConfig } from '../shared/employee-grid/employee-grid.interface';

// Employee interface is now imported from shared/employee-grid/employee-grid.interface.ts

export interface ApiEmployee {
  employeeId: number;
  employeeNumber: number;
  employeeNetworkId?: string;
  employeeName: string;
  companyCode?: string;
  companyName?: string;
  divisionCode?: string;
  divisionName?: string;
  position?: string;
  department?: string;
  email?: string;
  phone?: string;
  isActive: boolean;
  supervisor?: string;
  hireDate?: string;
  status?: string;
  hasExistingHRRequest?: boolean;
}

export interface ReturnToWorkFormData {
  selectedEmployees: Employee[];
  effectiveDate: string;
  notes: string;
  requestTypeId: number;
}

export interface EmployeeSearchParams {
  page: number;
  pageSize: number;
  searchQuery?: string;
  searchType: 'employee' | 'division';
  orderBy?: string;
  orderByDesc?: boolean;
  isEditMode?: boolean;
}

export interface EmployeeSearchResult {
  employees: Employee[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

export interface ViewpointEmployeeDto {
  HRCo: number;
  PREmp: number;
  HRRef: number;
  FirstName: string;
  LastName: string;
  Status?: string;
}

export interface ReturnToWorkSubmissionData {
  employees: Employee[];
  effectiveDate: string;
  notes: string;
  submittedBy: string;
  submittedDate: string;
}

// SortConfig interface is now imported from shared/employee-grid/employee-grid.interface.ts

export interface ReturnToWorkState {
  // Edit mode properties
  parentId: number | null;
  isEditMode: boolean;
  
  // Search and selection state
  searchType: 'employee' | 'division';
  searchQuery: string;
  showSearchResults: boolean;
  showEmployeeGrid: boolean;
  
  // Pagination state
  currentPage: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  
  // Sorting state
  currentSort: SortConfig;
  
  // Data arrays
  selectedEmployees: Employee[];
  searchResults: Employee[];
  laidOffEmployees: Employee[];
  filteredEmployees: Employee[];
  
  // Form fields
  effectiveDate: string;
  notes: string;
  
  // Loading state
  isLoading: boolean;
}