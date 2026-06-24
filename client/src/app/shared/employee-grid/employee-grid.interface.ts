/**
 * Shared interfaces for the employee grid component
 */

export interface Employee {
  employeeNumber: number;
  name: string;
  title: string;
  division: string;
  department: string;
  positionCode: string;
  company: string;
  companyCode: number;
  hasExistingHRRequest?: boolean;
  status?: string;
}

export interface SortConfig {
  field: string | null;
  direction: 'asc' | 'desc';
}

export interface GridColumn {
  key: string;
  label: string;
  sortable: boolean;
  width?: string;
  type?: 'text' | 'number' | 'checkbox';
  hidden?: boolean;
}

export interface EmployeeGridConfig {
  showSearch?: boolean;
  showPagination?: boolean;
  showPageSize?: boolean;
  allowMultiSelect?: boolean;
  searchModes?: ('employee' | 'division')[];
  defaultPageSize?: number;
  pageSizeOptions?: number[];
  columns?: GridColumn[];
  emptyMessage?: string;
  employeeType?: string; // e.g., 'employees', 'laid-off employees', 'active employees'
}

export interface EmployeeSearchParams {
  page: number;
  pageSize: number;
  searchQuery?: string;
  searchType: 'employee' | 'division';
  orderBy?: string;
  orderByDesc?: boolean;
}

export interface EmployeeGridData {
  employees: Employee[];
  totalCount: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
}

export interface EmployeeGridState {
  isLoading: boolean;
  searchQuery: string;
  searchType: 'employee' | 'division';
  sortConfig: SortConfig;
  selectedEmployees: Employee[];
  showSearchResults: boolean;
  searchResults: Employee[];
}