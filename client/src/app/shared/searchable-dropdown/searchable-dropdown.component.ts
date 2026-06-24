import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChanges, forwardRef, ViewChild, ElementRef, Renderer2 } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';

export interface SearchableDropdownConfig<T> {
  placeholder?: string;
  displayProperty: keyof T;
  valueProperty: keyof T;
  disabled?: boolean;
  required?: boolean;
  loading?: boolean;
  noResultsText?: string;
  minSearchLength?: number;
}

export interface SearchService<T> {
  searchItems: (searchTerm: string) => Promise<T[]>;
}

@Component({
  selector: 'app-searchable-dropdown',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './searchable-dropdown.component.html',
  styleUrls: ['./searchable-dropdown.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SearchableDropdownComponent),
      multi: true
    }
  ]
})
export class SearchableDropdownComponent<T> implements OnInit, OnDestroy, OnChanges, ControlValueAccessor {
  @Input() config: SearchableDropdownConfig<T> = {
    placeholder: 'Select...',
    displayProperty: '' as keyof T,
    valueProperty: '' as keyof T,
    disabled: false,
    required: false,
    loading: false,
    noResultsText: 'No results found',
    minSearchLength: 2
  };
  @Input() required: boolean = false;
  @Input() items?: T[];
  @Input() searchService?: SearchService<T>;

  @Output() selectionChanged = new EventEmitter<T | null>();

  @ViewChild('searchInput', { static: false }) searchInputRef?: ElementRef<HTMLInputElement>;
  @ViewChild('dropdownList', { static: false }) dropdownListRef?: ElementRef<HTMLDivElement>;

  // Component state
  searchQuery: string = '';
  searchResults: T[] = [];
  isDropdownOpen: boolean = false;
  selectedItem: T | null = null;
  highlightedIndex: number = -1;
  isSearching: boolean = false;
  
  // Internal state
  private value: any = null;
  private searchSubject = new Subject<string>();
  private destroy$ = new Subject<void>();
  private blurTimeout: any = null;
  private lastKnownDisplayText: string = '';
  private isDataReloading: boolean = false;
  private isSelectingItem: boolean = false;
  private lastFocusTime: number = 0;
  
  // ControlValueAccessor callbacks
  private onChange = (value: any) => {};
  private onTouched = () => {};

  // Document click listener for closing dropdown when clicking outside
  private documentClickListener: (() => void) | null = null;

  constructor(
    private elementRef: ElementRef,
    private renderer: Renderer2
  ) {}

  ngOnInit(): void {
    // Setup debounced search (client or server)
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(searchTerm => {
      if (this.searchService) {
        this.performServerSearch(searchTerm);
      } else {
        this.performClientSearch(searchTerm);
      }
    });

    // Initialize with all items for client-side search
    if (this.items && !this.searchService) {
      this.searchResults = [...this.items];
    }

    // Update display when value changes
    this.updateDisplayFromValue();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // When items input changes, update search results and re-evaluate the display
    if (changes['items'] && !this.searchService) {
      console.log('[SearchableDropdown] ngOnChanges - items changed:', {
        itemsCount: this.items?.length || 0,
        currentValue: this.value,
        placeholder: this.config.placeholder
      });
      if (this.items && this.items.length > 0) {
        this.searchResults = [...this.items];
        // Re-run updateDisplayFromValue immediately AND with setTimeout
        console.log('[SearchableDropdown] ngOnChanges - calling updateDisplayFromValue immediately');
        this.updateDisplayFromValue();

        // Also schedule a delayed update as backup
        setTimeout(() => {
          console.log('[SearchableDropdown] ngOnChanges setTimeout - calling updateDisplayFromValue again');
          this.updateDisplayFromValue();
        }, 10);
      }
    }
  }

  ngOnDestroy(): void {
    // Clean up timeout
    if (this.blurTimeout) {
      clearTimeout(this.blurTimeout);
      this.blurTimeout = null;
    }

    // Clean up document click listener
    this.removeDocumentClickListener();

    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Add document click listener to close dropdown when clicking outside
   */
  private addDocumentClickListener(): void {
    // Remove existing listener first to prevent duplicates
    this.removeDocumentClickListener();

    // Use setTimeout to defer adding the listener until after the current click event
    // This prevents the dropdown from immediately closing when opened via click
    setTimeout(() => {
      this.documentClickListener = this.renderer.listen('document', 'click', (event: MouseEvent) => {
        const clickedInside = this.elementRef.nativeElement.contains(event.target);
        if (!clickedInside && this.isDropdownOpen) {
          this.isDropdownOpen = false;
          this.onTouched();
          this.removeDocumentClickListener();
        }
      });
    }, 0);
  }

  /**
   * Remove document click listener
   */
  private removeDocumentClickListener(): void {
    if (this.documentClickListener) {
      this.documentClickListener();
      this.documentClickListener = null;
    }
  }

  // ControlValueAccessor implementation
  writeValue(value: any): void {
    console.log('[SearchableDropdown] writeValue called:', {
      value,
      hasItems: !!(this.items && this.items.length > 0),
      itemsCount: this.items?.length || 0,
      placeholder: this.config.placeholder
    });

    // Don't update display if we're currently selecting an item to prevent conflicts
    if (this.isSelectingItem) {
      this.value = value;
      return;
    }

    this.value = value;

    // If items are already loaded, update display immediately
    // Otherwise, ngOnChanges will handle it when items arrive
    if (this.items && this.items.length > 0) {
      this.updateDisplayFromValue();
    }
    // Also schedule a delayed check in case items are loaded asynchronously
    // This handles the race condition where writeValue is called before items binding updates
    setTimeout(() => {
      if (this.value && this.items && this.items.length > 0 && !this.selectedItem) {
        console.log('[SearchableDropdown] Delayed writeValue check - updating display');
        this.updateDisplayFromValue();
      }
    }, 50);
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.config.disabled = isDisabled;
  }

  // Component methods
  onInputFocus(): void {
    if (this.config.disabled || this.config.loading) return;

    // Prevent rapid double focus events (within 50ms)
    const now = Date.now();
    if (now - this.lastFocusTime < 50) {
      return;
    }
    this.lastFocusTime = now;

    this.isDropdownOpen = true;
    this.highlightedIndex = -1;

    // Add document click listener to close dropdown when clicking outside
    this.addDocumentClickListener();
    
    // For client-side search, show all items initially
    if (!this.searchService && this.items) {
      this.searchResults = [...this.items];
    }
    
    // For server-side search, show existing items if available, don't clear immediately
    if (this.searchService) {
      if (this.searchQuery && this.searchQuery.length >= (this.config.minSearchLength || 2)) {
        // If we have a valid search query, perform search
        this.searchSubject.next(this.searchQuery);
      } else if (this.items && this.items.length > 0) {
        // If we have items available (cached or pre-loaded), show them
        this.searchResults = [...this.items];
      } else {
        // Only clear results if we have no items to show
        this.searchResults = [];
      }
    }
  }

  onInputBlur(): void {
    // Clear any existing timeout
    if (this.blurTimeout) {
      clearTimeout(this.blurTimeout);
    }

    // Increased delay to allow for click selection
    this.blurTimeout = setTimeout(() => {
      this.isDropdownOpen = false;
      this.removeDocumentClickListener();
      this.onTouched();
      this.blurTimeout = null;
    }, 300);
  }

  onSearchInputChange(event: Event): void {
    // Don't trigger search when we're programmatically selecting an item
    if (this.isSelectingItem) {
      return;
    }

    const target = event.target as HTMLInputElement;
    const newValue = target.value;

    // Don't trigger search if the value hasn't actually changed
    if (newValue === this.searchQuery) {
      return;
    }

    // Ensure dropdown is open when user is typing
    if (!this.isDropdownOpen) {
      this.isDropdownOpen = true;
      this.addDocumentClickListener();
    }

    // If user manually cleared the input and there was a selected item, reset the selection
    if (newValue === '' && this.selectedItem !== null) {
      this.selectedItem = null;
      this.value = null;
      this.searchQuery = '';
      this.lastKnownDisplayText = '';

      // Reset search results to show all items for client-side search
      if (!this.searchService && this.items) {
        this.searchResults = [...this.items];
      }

      this.highlightedIndex = -1;

      // Emit changes
      this.onChange(null);
      this.selectionChanged.emit(null);

      return;
    }

    // If user is typing but had a previous selection, clear it since they're searching for something new
    if (this.selectedItem !== null && newValue !== this.lastKnownDisplayText) {
      this.selectedItem = null;
      this.value = null;
      this.lastKnownDisplayText = '';
      this.onChange(null);
      this.selectionChanged.emit(null);
    }

    this.searchQuery = newValue;

    // Perform search immediately for client-side (no debounce needed for filtering)
    if (!this.searchService) {
      if (this.searchQuery.length > 0) {
        this.performClientSearch(this.searchQuery);
      } else if (this.items) {
        this.searchResults = [...this.items];
      }
    } else {
      // Server-side search with debounce
      if (this.searchQuery.length >= (this.config.minSearchLength || 2)) {
        this.searchSubject.next(this.searchQuery);
      } else {
        this.searchResults = [];
      }
    }

    this.highlightedIndex = -1;
  }

  onItemMouseDown(event: MouseEvent, item: T): void {
    // Prevent the blur event from firing immediately
    event.preventDefault();
    // Call selectItem as backup in case click event doesn't work
    this.selectItem(item);
  }

  onKeyDown(event: KeyboardEvent): void {
    if (this.config.disabled || this.config.loading) return;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        if (!this.isDropdownOpen) {
          this.onInputFocus();
        } else {
          this.highlightedIndex = Math.min(this.highlightedIndex + 1, this.searchResults.length - 1);
          this.scrollHighlightedItemIntoView();
        }
        break;

      case 'ArrowUp':
        event.preventDefault();
        this.highlightedIndex = Math.max(this.highlightedIndex - 1, 0);
        this.scrollHighlightedItemIntoView();
        break;

      case 'Enter':
        event.preventDefault();
        if (this.isDropdownOpen && this.highlightedIndex >= 0) {
          this.selectItem(this.searchResults[this.highlightedIndex]);
        }
        break;

      case 'Escape':
        event.preventDefault();
        this.isDropdownOpen = false;
        this.removeDocumentClickListener();
        break;
    }
  }

  /**
   * Scrolls the currently highlighted item into view within the dropdown list
   */
  private scrollHighlightedItemIntoView(): void {
    if (this.highlightedIndex < 0 || !this.dropdownListRef) return;

    // Use setTimeout to ensure DOM has updated
    setTimeout(() => {
      const dropdownList = this.dropdownListRef?.nativeElement;
      if (!dropdownList) return;

      const items = dropdownList.querySelectorAll('.dropdown-item:not(.loading):not(.no-results)');
      const highlightedItem = items[this.highlightedIndex] as HTMLElement;

      if (highlightedItem) {
        highlightedItem.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      }
    }, 0);
  }

  selectItem(item: T): void {
    // Cancel blur timeout since we're actively selecting an item
    if (this.blurTimeout) {
      clearTimeout(this.blurTimeout);
      this.blurTimeout = null;
    }

    // Set flag to prevent onSearchQueryChange from interfering
    this.isSelectingItem = true;
    
    this.selectedItem = item;
    this.value = item[this.config.valueProperty];
    this.searchQuery = String(item[this.config.displayProperty]);
    this.lastKnownDisplayText = this.searchQuery; // Preserve display text
    this.isDropdownOpen = false;
    this.removeDocumentClickListener();
    this.highlightedIndex = -1;
    
    // Emit changes
    this.onChange(this.value);
    this.selectionChanged.emit(item);
    
    // Call onTouched since user has interacted with the control
    this.onTouched();
    
    // Reset flag after a small delay to ensure all reactive form cycles complete
    setTimeout(() => {
      this.isSelectingItem = false;
      // Ensure the input field shows the selected value
      if (this.searchInputRef?.nativeElement) {
        this.searchInputRef.nativeElement.value = this.searchQuery;
      }
    }, 100);
  }

  clearSelection(): void {
    // Cancel blur timeout since we're actively clearing selection
    if (this.blurTimeout) {
      clearTimeout(this.blurTimeout);
      this.blurTimeout = null;
    }

    // Set flag to prevent onSearchQueryChange from interfering
    this.isSelectingItem = true;
    
    this.selectedItem = null;
    this.value = null;
    this.searchQuery = '';
    this.searchResults = [];
    this.isDropdownOpen = false;
    this.removeDocumentClickListener();
    this.highlightedIndex = -1;
    
    // Emit changes
    this.onChange(null);
    this.selectionChanged.emit(null);
    
    // Reset flag after a small delay to ensure all reactive form cycles complete
    setTimeout(() => {
      this.isSelectingItem = false;
    }, 100);
  }

  private performClientSearch(searchTerm: string): void {
    if (!this.items) {
      this.searchResults = [];
      return;
    }

    if (!searchTerm || searchTerm.length === 0) {
      this.searchResults = [...this.items];
      return;
    }

    const lowerSearchTerm = searchTerm.toLowerCase();
    this.searchResults = this.items.filter(item => {
      const displayText = String(item[this.config.displayProperty]).toLowerCase();
      const valueText = String(item[this.config.valueProperty]).toLowerCase();
      return displayText.includes(lowerSearchTerm) || valueText.includes(lowerSearchTerm);
    });
    
    this.highlightedIndex = -1;
  }

  private async performServerSearch(searchTerm: string): Promise<void> {
    if (!this.searchService) {
      console.warn('SearchableDropdownComponent: No search service provided');
      return;
    }

    if (!searchTerm || searchTerm.length < (this.config.minSearchLength || 2)) {
      this.searchResults = [];
      return;
    }

    try {
      this.isSearching = true;
      const results = await this.searchService.searchItems(searchTerm);
      this.searchResults = results || [];
      this.highlightedIndex = -1;
    } catch (error) {
      console.error('Error performing server search:', error);
      this.searchResults = [];
    } finally {
      this.isSearching = false;
    }
  }

  private updateDisplayFromValue(): void {
    console.log('[SearchableDropdown] updateDisplayFromValue called:', {
      value: this.value,
      hasItems: !!(this.items && this.items.length > 0),
      itemsCount: this.items?.length || 0,
      valueProperty: this.config.valueProperty,
      displayProperty: this.config.displayProperty,
      placeholder: this.config.placeholder
    });

    if (!this.value) {
      this.selectedItem = null;
      this.searchQuery = '';
      this.lastKnownDisplayText = '';
      this.syncInputValue();
      return;
    }

    // For client-side search, try to find the item in the items array
    if (this.items && !this.searchService) {
      const foundItem = this.items.find(item =>
        String(item[this.config.valueProperty]) === String(this.value)
      );

      console.log('[SearchableDropdown] Finding item:', {
        searchingFor: this.value,
        valueProperty: this.config.valueProperty,
        foundItem: foundItem ? 'FOUND' : 'NOT FOUND',
        itemIds: this.items.slice(0, 5).map(i => i[this.config.valueProperty])
      });

      if (foundItem) {
        this.selectedItem = foundItem;
        this.searchQuery = String(foundItem[this.config.displayProperty]);
        this.lastKnownDisplayText = this.searchQuery;
        console.log('[SearchableDropdown] Item found, setting searchQuery:', this.searchQuery);
        // Ensure the input field shows the selected value
        this.syncInputValue();
      } else if (this.isDataReloading && this.lastKnownDisplayText) {
        // During data reloading, preserve the last known display text
        this.selectedItem = null;
        this.searchQuery = this.lastKnownDisplayText;
      } else if (this.isSelectingItem && this.lastKnownDisplayText) {
        // During item selection, preserve the display text to prevent clearing
        this.searchQuery = this.lastKnownDisplayText;
      } else {
        // Clear selection only if we're not reloading data, not selecting items, or have no preserved text
        console.log('[SearchableDropdown] Item NOT found, clearing selection');
        this.selectedItem = null;
        this.searchQuery = '';
        this.lastKnownDisplayText = '';
      }
    }
    // For server-side search, we keep the search query as is
    // The user will need to search again to see the full item details
  }

  /**
   * Sync the input element's value with searchQuery
   * This ensures the DOM is updated even if Angular's change detection misses it
   */
  private syncInputValue(): void {
    // Use setTimeout to ensure this runs after Angular's change detection
    setTimeout(() => {
      if (this.searchInputRef?.nativeElement) {
        this.searchInputRef.nativeElement.value = this.searchQuery;
      }
    }, 0);
  }

  // Helper methods for template
  getDisplayText(item: T): string {
    return String(item[this.config.displayProperty]);
  }

  isItemHighlighted(index: number): boolean {
    return index === this.highlightedIndex;
  }

  getPlaceholder(): string {
    if (this.config.loading || this.isSearching) {
      return 'Loading...';
    }
    return this.config.placeholder || 'Type to search...';
  }

  hasSearchResults(): boolean {
    return this.searchResults.length > 0;
  }

  shouldShowNoResults(): boolean {
    return !this.isSearching && 
           !this.config.loading && 
           this.searchQuery.length >= (this.config.minSearchLength || 2) && 
           this.searchResults.length === 0;
  }

  // Methods to signal data reloading state (can be called by parent components)
  signalDataReloadStart(): void {
    this.isDataReloading = true;
    // Preserve current display text
    if (this.searchQuery) {
      this.lastKnownDisplayText = this.searchQuery;
    }
  }

  signalDataReloadEnd(): void {
    this.isDataReloading = false;
    // Update display after data reload is complete
    this.updateDisplayFromValue();
  }

}