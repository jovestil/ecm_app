import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

export interface SingleEmployeeSearchConfig {
  placeholder?: string;
  minSearchLength?: number;
  debounceTime?: number;
  theme?: 'default' | 'danger' | 'promotion';
}

export interface SelectedEmployee {
  id: number;
  name: string;
  title: string;
  division: string;
  department?: string;
  employeeNumber?: number;
  hasExistingHRRequest?: boolean;
  currentData?: any;
}

@Component({
  selector: 'app-single-employee-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './single-employee-search.component.html',
  styleUrls: ['./single-employee-search.component.css']
})
export class SingleEmployeeSearchComponent implements OnInit, OnDestroy {
  @Input() config: SingleEmployeeSearchConfig = {
    placeholder: 'Search for employee...',
    minSearchLength: 2,
    debounceTime: 300,
    theme: 'default'
  };
  
  @Input() disabled: boolean = false;
  @Input() searchService: any; // Service for making API calls
  @Input() selectedEmployee: SelectedEmployee | null = null;
  @Input() selectionHeader: string = '';

  @Output() employeeSelected = new EventEmitter<SelectedEmployee>();
  @Output() employeeCleared = new EventEmitter<void>();
  @Output() searchPerformed = new EventEmitter<string>();

  // Component state
  searchQuery: string = '';
  searchResults: SelectedEmployee[] = [];
  showSearchResults: boolean = false;
  isLoading: boolean = false;
  
  // Debounce search
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  ngOnInit(): void {
    // Setup debounced search
    this.searchSubject.pipe(
      debounceTime(this.config.debounceTime || 300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.performSearch(searchTerm);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchInputChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchQuery = target.value;
    
    if (this.searchQuery.length < (this.config.minSearchLength || 2)) {
      this.clearSearchResults();
      return;
    }

    this.searchSubject.next(this.searchQuery);
  }

  private async performSearch(searchTerm: string): Promise<void> {
    if (searchTerm.length < (this.config.minSearchLength || 2)) {
      this.clearSearchResults();
      return;
    }

    this.isLoading = true;
    this.showSearchResults = true;
    
    try {
      if (this.searchService && this.searchService.searchEmployees) {
        const results = await this.searchService.searchEmployees(searchTerm);
        this.searchResults = results;
      } else {
        // Fallback to mock data for development
        this.searchResults = this.getMockEmployees().filter(emp => 
          emp.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
          emp.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
          emp.division.toLowerCase().includes(searchTerm.toLowerCase())
        );
      }
      
      this.searchPerformed.emit(searchTerm);
    } catch (error) {
      console.error('Error searching employees:', error);
      this.searchResults = [];
    } finally {
      this.isLoading = false;
    }
  }

  selectEmployee(employee: SelectedEmployee): void {
    if (this.disabled || this.isEmployeeDisabled(employee)) return;
    
    this.selectedEmployee = employee;
    this.clearSearchResults();
    this.searchQuery = '';
    this.employeeSelected.emit(employee);
  }

  isEmployeeDisabled(employee: SelectedEmployee): boolean {
    return employee.hasExistingHRRequest === true || this.disabled;
  }

  clearEmployee(): void {
    if (this.disabled) return;
    
    this.selectedEmployee = null;
    this.employeeCleared.emit();
  }

  private clearSearchResults(): void {
    this.searchResults = [];
    this.showSearchResults = false;
    this.isLoading = false;
  }

  getSelectedEmployeeClass(): string {
    const theme = this.config.theme || 'default';
    return `selected-employee selected-employee--${theme}`;
  }

  getClearButtonClass(): string {
    const theme = this.config.theme || 'default';
    return `clear-btn clear-btn--${theme}`;
  }

  private getMockEmployees(): SelectedEmployee[] {
    return [
      { 
        id: 1, 
        name: 'John Smith', 
        title: 'Site Supervisor', 
        division: 'Field Operations', 
        department: 'Commercial Construction',
        employeeNumber: 1001,
        currentData: {
          payrollCompany: '64',
          payrollGroup: '2',
          payrollDept: '6401',
          position: 'SITESUPR',
          timeCardSupervisor: 'Mike Johnson',
          vacationSupervisor: 'Sarah Davis',
          functionalDept: 'None',
          physicalLocation: '100',
          status: 'FULL TIME'
        }
      },
      { 
        id: 2, 
        name: 'Sarah Johnson', 
        title: 'Project Manager', 
        division: 'Project Management', 
        department: 'Residential Projects',
        employeeNumber: 1002,
        currentData: {
          payrollCompany: '19',
          payrollGroup: '6',
          payrollDept: '6404',
          position: 'PROJMGR BA',
          timeCardSupervisor: 'David Wilson',
          vacationSupervisor: 'Lisa Chen',
          functionalDept: 'None',
          physicalLocation: '101',
          status: 'FULL TIME'
        }
      },
      { 
        id: 3, 
        name: 'Mike Chen', 
        title: 'Heavy Equipment Operator', 
        division: 'Field Operations', 
        department: 'Infrastructure',
        employeeNumber: 1003,
        currentData: {
          payrollCompany: '64',
          payrollGroup: '4',
          payrollDept: '6401',
          position: 'EQPOPER',
          timeCardSupervisor: 'Robert Martinez',
          vacationSupervisor: 'John Smith',
          functionalDept: 'None',
          physicalLocation: '102',
          status: 'FULL TIME'
        }
      },
      { 
        id: 4, 
        name: 'Emily Davis', 
        title: 'Safety Coordinator', 
        division: 'Safety & Compliance', 
        department: 'Job Site Safety',
        employeeNumber: 1004
      },
      { 
        id: 5, 
        name: 'David Rodriguez', 
        title: 'Electrician', 
        division: 'Trades', 
        department: 'Electrical Services',
        employeeNumber: 1005
      },
      { 
        id: 6, 
        name: 'Lisa Wang', 
        title: 'CAD Technician', 
        division: 'Engineering', 
        department: 'Design & Drafting',
        employeeNumber: 1006
      },
      { 
        id: 7, 
        name: 'James Wilson', 
        title: 'Cost Estimator', 
        division: 'Pre-Construction', 
        department: 'Estimating',
        employeeNumber: 1007
      },
      { 
        id: 8, 
        name: 'Emma Thompson', 
        title: 'Carpenter', 
        division: 'Trades', 
        department: 'Framing & Finish',
        employeeNumber: 1008
      },
      { 
        id: 9, 
        name: 'Robert Martinez', 
        title: 'Crane Operator', 
        division: 'Field Operations', 
        department: 'Heavy Equipment',
        employeeNumber: 1009
      },
      { 
        id: 10, 
        name: 'Jennifer Lee', 
        title: 'Concrete Finisher', 
        division: 'Trades', 
        department: 'Concrete & Masonry',
        employeeNumber: 1010
      },
      { 
        id: 11, 
        name: 'Alex Turner', 
        title: 'Foreman', 
        division: 'Field Operations', 
        department: 'Commercial Construction',
        employeeNumber: 1011
      },
      { 
        id: 12, 
        name: 'Maria Garcia', 
        title: 'Plumber', 
        division: 'Trades', 
        department: 'Plumbing Services',
        employeeNumber: 1012
      }
    ];
  }
}