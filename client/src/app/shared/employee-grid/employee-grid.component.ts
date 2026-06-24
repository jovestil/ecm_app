import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { 
  Employee, 
  SortConfig, 
  GridColumn, 
  EmployeeGridConfig,
  EmployeeGridData,
  EmployeeSearchParams 
} from './employee-grid.interface';

@Component({
  selector: 'app-employee-grid',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee-grid.component.html',
  styleUrls: ['./employee-grid.component.css']
})
export class EmployeeGridComponent implements OnInit, OnChanges {
  // Inputs
  @Input() employees: Employee[] = [];
  @Input() selectedEmployees: Employee[] = [];
  @Input() isLoading: boolean = false;
  @Input() totalCount: number = 0;
  @Input() currentPage: number = 1;
  @Input() pageSize: number = 25;
  @Input() totalPages: number = 1;
  @Input() sortConfig: SortConfig = { field: null, direction: 'asc' };
  @Input() searchQuery: string = '';
  @Input() searchType: 'employee' | 'division' = 'employee';
  @Input() searchResults: Employee[] = [];
  @Input() showSearchResults: boolean = false;
  @Input() config: EmployeeGridConfig = {};
  @Input() disabled: boolean = false;

  // Outputs
  @Output() employeeToggled = new EventEmitter<number>();
  @Output() pageChanged = new EventEmitter<number>();
  @Output() pageSizeChanged = new EventEmitter<number>();
  @Output() sortChanged = new EventEmitter<SortConfig>();
  @Output() searchChanged = new EventEmitter<string>();
  @Output() searchModeChanged = new EventEmitter<'employee' | 'division'>();

  // Default configuration
  defaultConfig: EmployeeGridConfig = {
    showSearch: true,
    showPagination: true,
    showPageSize: true,
    allowMultiSelect: true,
    searchModes: ['employee', 'division'],
    defaultPageSize: 25,
    pageSizeOptions: [10, 25, 50, 100],
    emptyMessage: 'No employees found',
    employeeType: 'employees', // Default employee type for labels
    columns: [
      { key: 'name', label: 'Employee Name', sortable: true },
      { key: 'company', label: 'Company', sortable: true },
      { key: 'division', label: 'Division', sortable: true },
      { key: 'employeeNumber', label: 'Emp ID', sortable: true },
      { key: 'positionCode', label: 'Position', sortable: true }
    ]
  };

  // Component state
  effectiveConfig: EmployeeGridConfig = {};
  displayedEmployees: Employee[] = [];

  ngOnInit() {
    this.effectiveConfig = { ...this.defaultConfig, ...this.config };
    this.updateDisplayedEmployees();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['employees'] || changes['searchResults'] || changes['showSearchResults']) {
      this.updateDisplayedEmployees();
    }
  }

  private updateDisplayedEmployees() {
    // Use search results if search is active, otherwise use main employees list
    this.displayedEmployees = this.showSearchResults ? this.searchResults : this.employees;
  }

  // Grid info display
  get gridInfo(): string {
    const startIndex = (this.currentPage - 1) * this.pageSize + 1;
    const endIndex = Math.min(this.currentPage * this.pageSize, this.totalCount);
    const total = this.totalCount;
    const employeeType = this.effectiveConfig.employeeType || 'employees';

    if (total === 0) {
      return this.effectiveConfig.emptyMessage || `No ${employeeType} found`;
    }
    return `Showing ${startIndex}-${endIndex} of ${total} ${employeeType}`;
  }

  get gridInfoBottom(): string {
    const total = this.totalCount;
    const employeeType = this.effectiveConfig.employeeType || 'employees';
    return `Total: ${total} ${employeeType}`;
  }

  get pageInfo(): string {
    return `Page ${this.currentPage} of ${this.totalPages}`;
  }

  // Employee selection methods
  isEmployeeSelected(employeeNumber: number): boolean {
    return this.selectedEmployees.some(emp => emp.employeeNumber === employeeNumber);
  }

  isEmployeeDisabled(employee: Employee): boolean {
    return employee.hasExistingHRRequest === true || this.disabled;
  }

  onEmployeeToggle(employeeNumber: number) {
    if (!this.disabled) {
      this.employeeToggled.emit(employeeNumber);
    }
  }

  // Pagination methods
  onPageChange(page: number) {
    if (page >= 1 && page <= this.totalPages && !this.disabled) {
      this.pageChanged.emit(page);
    }
  }

  onPageSizeChange(newSize: number) {
    if (!this.disabled) {
      this.pageSizeChanged.emit(newSize);
    }
  }

  // Sorting methods
  onSort(field: string) {
    if (!this.disabled) {
      const newConfig: SortConfig = {
        field: field,
        direction: this.sortConfig.field === field && this.sortConfig.direction === 'asc' ? 'desc' : 'asc'
      };
      this.sortChanged.emit(newConfig);
    }
  }

  getSortIndicator(field: string): string {
    if (this.sortConfig.field === field) {
      return this.sortConfig.direction === 'asc' ? '↑' : '↓';
    }
    return '↕';
  }

  isSortActive(field: string): boolean {
    return this.sortConfig.field === field;
  }

  // Search methods
  onSearchChange(query: string) {
    if (!this.disabled) {
      this.searchChanged.emit(query);
    }
  }

  onSearchModeChange(mode: 'employee' | 'division') {
    if (!this.disabled) {
      this.searchModeChanged.emit(mode);
    }
  }

  // Utility methods
  getColumnValue(employee: Employee, columnKey: string): any {
    return (employee as any)[columnKey];
  }

  get visibleColumns(): GridColumn[] {
    return this.effectiveConfig.columns?.filter(col => !col.hidden) || [];
  }

  get hasMultipleSearchModes(): boolean {
    return (this.effectiveConfig.searchModes?.length || 0) > 1;
  }

  get showEmployeeSearch(): boolean {
    return this.searchType === 'employee' && this.effectiveConfig.showSearch === true;
  }

  get showDivisionSearch(): boolean {
    return this.searchType === 'division' && this.effectiveConfig.showSearch === true;
  }
}