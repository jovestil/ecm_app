# PrimeNG Component Mapping - Mathy ECM System

This document details the specific PrimeNG components that will be used throughout the HR Employee Change Management System.

## Component Mapping by Feature

### 1. Dashboard & Request Listing

**Components Used:**
- `p-table` - Main request listing with sorting, filtering, pagination
- `p-card` - Individual request summary cards
- `p-button` - Action buttons (New Request, View, Edit, Delete)
- `p-menubar` - Top navigation menu
- `p-breadcrumb` - Navigation breadcrumbs
- `p-dialog` - Request type selection modal
- `p-toast` - Success/error notifications
- `p-progressBar` - Loading indicators
- `p-toolbar` - Action toolbar above table
- `p-splitButton` - New Request dropdown with multiple types

**Data Table Configuration:**
```typescript
// Request listing table columns
columns = [
  { field: 'requestId', header: 'Request ID', sortable: true },
  { field: 'requestType', header: 'Type', sortable: true },
  { field: 'employeeName', header: 'Employee', sortable: true },
  { field: 'status', header: 'Status', sortable: true },
  { field: 'createdDate', header: 'Created', sortable: true },
  { field: 'actions', header: 'Actions', sortable: false }
];
```

### 2. Employee Search & Selection

**Components Used:**
- `p-autoComplete` - Real-time employee search with Viewpoint integration
- `p-multiSelect` - Multi-employee selection for layoff/return-to-work
- `p-table` - Employee search results grid
- `p-panel` - Employee details panel
- `p-fieldset` - Grouping employee information sections
- `p-button` - Select/Deselect actions
- `p-chip` - Selected employee chips
- `p-paginator` - Search results pagination

**Employee Search Configuration:**
```typescript
// AutoComplete for employee search
employeeSearch = {
  minLength: 2,
  delay: 300,
  field: 'displayName',
  dropdown: true,
  multiple: false,
  suggestions: []
};
```

### 3. HR Request Forms

#### Promotion/Transfer Form
**Components Used:**
- `p-steps` - Multi-step form progression
- `p-panel` - Form sections (Employee Info, Position Change, Additional Resources)
- `p-inputText` - Text inputs (Employee ID, Position Title)
- `p-dropdown` - Dropdowns (Company, Department, Position)
- `p-calendar` - Date pickers (Effective Date, Start Date)
- `p-checkbox` - Checkboxes (IT Access, Credit Card, Vehicle)
- `p-radioButton` - Radio buttons (Credit Card Type, Vehicle Type)
- `p-textarea` - Multi-line text (Comments, Justification)
- `p-button` - Navigation and action buttons
- `p-messages` - Form validation messages
- `p-confirmDialog` - Submission confirmation

#### Layoff Request Form
**Components Used:**
- `p-tabView` - Tabs for Individual vs Company/Division selection
- `p-table` - Employee selection grid with multi-select
- `p-treeSelect` - Company/Division hierarchy selection
- `p-calendar` - Last day worked date picker
- `p-inputNumber` - Numeric inputs (employee count limits)
- `p-selectButton` - Toggle between selection modes
- `p-dataView` - Alternative grid view for employee selection

#### Termination Request Form
**Components Used:**
- `p-panel` - Form sections (Employee, Termination Details, Communication)
- `p-dropdown` - Termination reason selection
- `p-inputText` - Communication forwarding email/phone
- `p-checkbox` - Unemployment contest checkbox
- `p-calendar` - Termination date picker
- `p-editor` - Rich text editor for termination notes (optional)

#### Return to Work Form
**Components Used:**
- `p-table` - Laid-off employee selection grid
- `p-calendar` - Return date picker
- `p-multiSelect` - Multi-employee selection
- `p-filter` - Employee status filtering
- `p-button` - Bulk selection actions

### 4. Shared Components

#### Layout Components
- `p-menubar` - Main navigation menu
- `p-sidebar` - Mobile navigation sidebar
- `p-toolbar` - Page-level action toolbars
- `p-panel` - Content panels with headers
- `p-card` - Card-based layouts
- `p-divider` - Section dividers
- `p-scrollPanel` - Scrollable content areas

#### Form Components
- `p-inputText` - Standard text inputs
- `p-inputNumber` - Numeric inputs with validation
- `p-password` - Password inputs (for admin features)
- `p-dropdown` - Single selection dropdowns
- `p-multiSelect` - Multiple selection dropdowns
- `p-listbox` - Alternative to dropdowns for multiple options
- `p-checkbox` - Boolean checkboxes
- `p-radioButton` - Radio button groups
- `p-toggleButton` - Toggle switches
- `p-calendar` - Date/time pickers
- `p-textarea` - Multi-line text areas
- `p-inputMask` - Formatted inputs (phone, SSN, etc.)

#### Data Display Components
- `p-table` - Data grids with sorting/filtering/pagination
- `p-dataView` - Alternative grid/list views
- `p-virtualScroller` - Performance optimization for large datasets
- `p-paginator` - Pagination controls
- `p-orderList` - Reorderable lists
- `p-pickList` - Transfer lists for moving items between collections

#### Feedback Components
- `p-toast` - Toast notifications
- `p-messages` - Inline messages
- `p-message` - Single message display
- `p-confirmDialog` - Confirmation dialogs
- `p-progressBar` - Progress indicators
- `p-progressSpinner` - Loading spinners
- `p-skeleton` - Content placeholders during loading

#### Overlay Components
- `p-dialog` - Modal dialogs
- `p-confirmPopup` - Quick confirmation popups
- `p-tooltip` - Contextual help tooltips
- `p-overlay` - Custom overlay positioning

### 5. Theme Customization

**Theme Selection:**
- Base Theme: `lara-light-blue` (professional, modern)
- Custom CSS variables for company branding
- Responsive design breakpoints

**Color Scheme:**
```scss
// Custom theme variables
:root {
  --primary-color: #003366;
  --primary-color-text: #ffffff;
  --surface-a: #ffffff;
  --surface-b: #f8f9fa;
  --surface-c: #e9ecef;
  --surface-d: #dee2e6;
  --surface-e: #ffffff;
  --surface-f: #ffffff;
  --text-color: #495057;
  --text-color-secondary: #6c757d;
  --border-color: #dee2e6;
}
```

**Theme Files:**
- `server/src/assets/styles/_primeng-theme.scss` - Custom theme overrides
- `server/src/styles.scss` - Global PrimeNG styles import

### 6. Accessibility Features

**PrimeNG Accessibility Support:**
- ARIA labels and roles on all interactive components
- Keyboard navigation support
- Screen reader compatibility
- High contrast mode support
- Focus management for modals and overlays

**Custom Accessibility Enhancements:**
- Custom focus indicators matching company branding
- Skip navigation links
- Form validation announcements
- Loading state announcements

### 7. Performance Considerations

**Optimization Strategies:**
- Lazy loading of PrimeNG modules
- Virtual scrolling for large employee lists
- OnPush change detection strategy
- Debounced search inputs
- Cached dropdown data

**Bundle Size Management:**
```typescript
// Import only required PrimeNG modules
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
// ... other specific imports
```

### 8. Testing Strategy

**Component Testing:**
- Unit tests for custom PrimeNG component wrappers
- Integration tests for form submissions
- E2E tests for complete user workflows
- Accessibility testing with axe-core

**PrimeNG-Specific Testing:**
- Mock PrimeNG components in unit tests
- Test PrimeNG event handlers
- Validate PrimeNG data binding
- Test responsive behavior

### 9. Development Guidelines

**Component Usage Standards:**
- Always use PrimeNG components over custom HTML elements
- Follow PrimeNG naming conventions for consistency
- Use PrimeNG validators where available
- Leverage PrimeNG's built-in internationalization support

**Code Organization:**
- Group PrimeNG imports by feature modules
- Create shared PrimeNG modules for common components
- Use PrimeNG's CSS utilities for spacing and layout
- Follow PrimeNG's recommended patterns for data binding

### 10. Migration and Updates

**Version Management:**
- Pin PrimeNG version to ensure consistency
- Test PrimeNG updates in development environment
- Document any breaking changes in component usage
- Maintain backwards compatibility for custom components

**Future Enhancements:**
- PrimeNG Prime blocks for advanced layouts
- PrimeFlex for responsive utilities
- PrimeIcons expansion for additional icons
- Custom PrimeNG theme builder integration