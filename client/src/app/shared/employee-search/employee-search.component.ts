import { Component, Input, Output, EventEmitter, OnInit, ViewChild, ElementRef, OnDestroy, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { Employee } from '../employee-grid/employee-grid.interface';

export interface EmployeeSearchConfig {
  searchModes: ('employee' | 'division')[];
  defaultSearchMode?: 'employee' | 'division';  
  employeePlaceholder?: string;
  divisionPlaceholder?: string;
  showSearchTip?: boolean;
}

export interface SearchResult {
  searchType: 'employee' | 'division';
  searchQuery: string;
}

@Component({
  selector: 'app-employee-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './employee-search.component.html',
  styleUrls: ['./employee-search.component.css']
})
export class EmployeeSearchComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('searchInput', { static: false }) searchInput!: ElementRef<HTMLInputElement>;
  
  // Add debugging console reference
  console = console;
  
  @Input() config: EmployeeSearchConfig = {
    searchModes: ['employee', 'division'],
    defaultSearchMode: 'employee',
    employeePlaceholder: 'Search employees... (select multiple with checkboxes)',
    divisionPlaceholder: 'Search divisions... (select multiple employees per division)',
    showSearchTip: true
  };
  
  @Input() disabled: boolean = false;
  @Input() selectedEmployees: Employee[] = [];
  @Input() searchService: any; // Service for making API calls

  @Output() searchResultsChanged = new EventEmitter<Employee[]>();
  @Output() searchModeChanged = new EventEmitter<'employee' | 'division'>();
  @Output() employeeToggled = new EventEmitter<number>();
  @Output() showGridChanged = new EventEmitter<boolean>();
  @Output() divisionSearchChanged = new EventEmitter<string>();

  // Component state - completely autonomous like mockup
  searchType: 'employee' | 'division' = 'employee';
  searchResults: Employee[] = [];
  showSearchResults: boolean = true;
  isLoading: boolean = false;
  
  // Debounce search like mockup but internal only
  private searchSubject = new Subject<string>();
  private divisionSearchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();

  ngOnInit(): void {
    // Initialize search type from config
    this.searchType = this.config.defaultSearchMode || 'employee';
    console.log('EmployeeSearchComponent initialized with searchType:', this.searchType);
    
    // Setup debounced search for employee search (internal only, like mockup logic)
    this.searchSubject.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      this.performSearch(searchTerm);
    });
    
    // Setup debounced search for division search to prevent API call on every keystroke
    this.divisionSearchSubject.pipe(
      debounceTime(200), // Shorter debounce for better responsiveness
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      console.log('Division search debounced: emitting query:', searchTerm);
      this.divisionSearchChanged.emit(searchTerm);
      this.searchResultsChanged.emit([]); // Clear individual results
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    
    // Clear typing timeout
    if (this.typingTimeout) {
      clearTimeout(this.typingTimeout);
    }
  }

  private lastRestoredValue: string = '';
  private restorationCount: number = 0;

  ngAfterViewChecked(): void {
    // Only intervene if user is actively typing and input has been cleared
    if (this.isUserTyping && this.searchInput?.nativeElement && this.currentInputValue) {
      const currentDOMValue = this.searchInput.nativeElement.value;
      if (currentDOMValue !== this.currentInputValue && currentDOMValue.length < this.currentInputValue.length) {
        // Only restore if the DOM value is shorter (indicating it was cleared)
        console.log('DETECTED: Input value was cleared during typing! DOM:', currentDOMValue, 'Stored:', this.currentInputValue);
        
        // Prevent infinite restoration loops
        if (this.lastRestoredValue !== this.currentInputValue) {
          this.lastRestoredValue = this.currentInputValue;
          this.restorationCount = 0;
        }
        
        if (this.restorationCount < 3) { // Maximum 3 restoration attempts
          this.restorationCount++;
          
          // Restore the stored value and focus immediately
          const inputElement = this.searchInput.nativeElement;
          inputElement.value = this.currentInputValue;
          
          // Always restore focus when user is typing
          setTimeout(() => {
            inputElement.focus();
            // Set cursor to end of text
            inputElement.setSelectionRange(inputElement.value.length, inputElement.value.length);
            console.log('Restored focus and cursor position during typing (attempt', this.restorationCount, ')');
          }, 0);
        }
      }
    }
  }

  get hasMultipleSearchModes(): boolean {
    return this.config.searchModes.length > 1;
  }

  get currentPlaceholder(): string {
    return this.searchType === 'employee' 
      ? (this.config.employeePlaceholder || 'Search employees...')
      : (this.config.divisionPlaceholder || 'Search divisions...');
  }

  // Store current input value to prevent unwanted clearing
  private currentInputValue: string = '';
  private isUserTyping: boolean = false;
  private typingTimeout: any;

  // Direct input handler like mockup - reads input value directly
  onSearchInputChange(event?: Event): void {
    // Mark that user is actively typing
    this.isUserTyping = true;
    
    // Clear any existing timeout and set a new one
    if (this.typingTimeout) {
      clearTimeout(this.typingTimeout);
    }
    this.typingTimeout = setTimeout(() => {
      this.isUserTyping = false;
      console.log('User stopped typing');
    }, 1000);
    
    // Get query from event target - this is most reliable
    let query: string;
    const target = event?.target as HTMLInputElement;
    
    if (target && target.value !== undefined) {
      query = target.value;
      console.log('Got query from event target:', query);
    } else if (this.searchInput?.nativeElement) {
      query = this.searchInput.nativeElement.value;
      console.log('Got query from ViewChild:', query);
    } else {
      console.error('Cannot get search input value - no event target or ViewChild available');
      return;
    }
    
    // Store the current value
    this.currentInputValue = query;
    console.log('Input changed, query:', query, 'searchType:', this.searchType, 'event type:', event?.type, 'isUserTyping:', this.isUserTyping);
    
    if (this.searchType === 'division') {
      // For division search, use debounced search to prevent API calls on every keystroke
      console.log('Division search: sending to debounced subject:', query);
      this.divisionSearchSubject.next(query);
      return;
    }

    // For employee search, use debounced individual search
    console.log('Employee search: sending to subject:', query);
    this.searchSubject.next(query);
  }

  onSearchModeToggle(mode: 'employee' | 'division'): void {
    console.log('onSearchModeToggle called with mode:', mode, 'current searchType:', this.searchType);
    if (this.searchType !== mode) {
      const previousMode = this.searchType;
      this.searchType = mode;
      console.log('Switching from', previousMode, 'to mode:', mode);
      
      // Clear input value and stored value when switching modes
      this.currentInputValue = '';
      if (this.searchInput?.nativeElement) {
        const inputElement = this.searchInput.nativeElement;
        inputElement.value = '';
        console.log('Cleared input value and stored value, input is:', inputElement.disabled ? 'disabled' : 'enabled', 'readonly:', inputElement.readOnly);
        
        // Use setTimeout to ensure DOM updates are processed
        setTimeout(() => {
          inputElement.focus();
          console.log('Focused input element after mode change');
        }, 50);
      } else {
        console.log('Warning: searchInput ViewChild not available during mode toggle');
      }
      
      if (mode === 'employee') {
        // Show individual search results, hide grid
        this.showSearchResults = true;
        this.searchResults = [];
        this.showGridChanged.emit(false);
        console.log('Set to employee mode: showSearchResults=true, showGrid=false');
      } else {
        // Hide individual search results, show grid
        this.showSearchResults = false;  
        this.searchResults = [];
        this.showGridChanged.emit(true);
        console.log('Set to division mode: showSearchResults=false, showGrid=true');
      }
      
      this.searchModeChanged.emit(mode);
      console.log('Emitted searchModeChanged:', mode);
    } else {
      console.log('Mode toggle called but already in', mode, 'mode');
    }
  }

  private async performSearch(searchTerm: string): Promise<void> {
    console.log('EmployeeSearchComponent.performSearch called with:', searchTerm);
    
    // Employee search logic like mockup
    if (searchTerm.length < 2) {
      console.log('Search term too short, clearing results');
      this.searchResults = [];
      this.searchResultsChanged.emit([]);
      return;
    }

    this.isLoading = true;
    try {
      console.log('Calling searchService.searchEmployees with:', searchTerm);
      // This will be implemented by parent via searchService
      if (this.searchService && this.searchService.searchEmployees) {
        const results = await this.searchService.searchEmployees(searchTerm);
        console.log('Search results received:', results);
        this.searchResults = results;
        this.searchResultsChanged.emit(results);
      } else {
        console.error('No searchService or searchEmployees method provided');
      }
    } catch (error) {
      console.error('Error searching employees:', error);
      this.searchResults = [];
      this.searchResultsChanged.emit([]);
    } finally {
      this.isLoading = false;
    }
  }

  canShowSearchMode(mode: 'employee' | 'division'): boolean {
    return this.config.searchModes.includes(mode);
  }

  // Employee selection methods to match original behavior
  isEmployeeSelected(employeeNumber: number): boolean {
    return this.selectedEmployees.some(emp => emp.employeeNumber === employeeNumber);
  }

  isEmployeeDisabled(employee: Employee): boolean {
    return employee.hasExistingHRRequest === true;
  }

  onEmployeeToggle(employeeNumber: number): void {
    if (!this.disabled) {
      this.employeeToggled.emit(employeeNumber);
    }
  }

  get showEmployeeSearch(): boolean {
    return this.searchType === 'employee';
  }

  // Debug method to help with testing
  debugInputState(): void {
    if (this.searchInput?.nativeElement) {
      const input = this.searchInput.nativeElement;
      console.log('=== Input Debug Info ===');
      console.log('Search Type:', this.searchType);
      console.log('Input Value (DOM):', input.value);
      console.log('Input Value (stored):', this.currentInputValue);
      console.log('Input Disabled:', input.disabled);
      console.log('Input ReadOnly:', input.readOnly);
      console.log('Input Focused:', document.activeElement === input);
      console.log('Input Visible:', input.offsetParent !== null);
      console.log('========================');
      
      // Try to restore the stored value if DOM value is empty but stored isn't
      if (!input.value && this.currentInputValue) {
        console.log('Restoring input value from stored value:', this.currentInputValue);
        input.value = this.currentInputValue;
        input.focus();
      }
    } else {
      console.log('ViewChild not available for debug');
    }
  }
}