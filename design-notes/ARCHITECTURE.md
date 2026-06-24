# HR Employee Change Management System - Architecture

## System Overview

The HR Employee Change Management System is a web-based application that allows managers to submit HR requests for their employees including promotions, layoffs, terminations, and returns to work. The system integrates with Viewpoint/Vista for employee data and uses Entra ID for authentication.

### Technology Stack
- **Frontend**: Angular 19 (separate IIS site)
- **Backend**: .NET Core 9 Web API (separate IIS site)
- **Database**: SQL Server (on-premises)
- **Authentication**: Entra ID (Azure AD)
- **Integration**: Viewpoint/Vista API + direct DB access
- **Hosting**: On-premises IIS

## High-Level Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Angular 19    │    │  .NET Core 9    │    │   SQL Server    │
│   Frontend      │◄──►│   Web API       │◄──►│   Database      │
│   (IIS Site 1)  │    │   (IIS Site 2)  │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│   Entra ID      │    │ Viewpoint/Vista │
│ Authentication  │    │   Integration   │
└─────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │Email Notification│
                       │    Subsystem     │
                       └─────────────────┘
```

## Core Components

### 1. Frontend (Angular 19)

**Key Features:**
- **Forms**: Promotion/Transfer, Layoff, Termination, Return to Work
- **Dashboard**: Request listing with filtering, sorting, and pagination
- **Employee Search**: Real-time search with Viewpoint integration
- **Reference Data**: Cached dropdowns (positions, companies, payroll groups, etc.)
- **Authentication**: Entra ID integration with automatic token refresh

**Architecture:**
- Standalone Angular application deployed as static files on IIS
- Component-based architecture with shared services
- PrimeNG UI component library for enterprise-grade components
- Real-time API communication with .NET Core backend
- Client-side validation with server-side validation backup

**PrimeNG Component Strategy:**
- **Data Tables**: p-table for employee grids, request listings with sorting/filtering/pagination
- **Forms**: p-inputText, p-dropdown, p-calendar, p-checkbox, p-radioButton for form controls
- **Layout**: p-panel, p-fieldset, p-card for structured content sections
- **Navigation**: p-menubar, p-breadcrumb, p-button for navigation elements
- **Modals**: p-dialog for confirmation dialogs and selection modals
- **Feedback**: p-toast, p-progressBar, p-messages for user notifications
- **Advanced**: p-multiSelect for employee selection, p-autoComplete for employee search
- **Theme**: Customizable PrimeNG theme to match company branding

### 2. Backend API (.NET Core 9)

**Controllers:**
- `HRRequestsController`: CRUD operations for HR requests
- `EmployeeController`: Real-time employee search via Viewpoint
- `ReferenceDataController`: Cached lookup data (companies, positions, etc.)
- `AuthorizationController`: User permissions and company access

**Services:**
- `IAuthorizationProvider`: Interface for user company access (Viewpoint profile → Entra groups)
- `IViewpointIntegrationService`: Employee data and reference data from Viewpoint/Vista
- `IEmailNotificationService`: Queue and send templated emails
- `IReferenceDataSyncService`: Scheduled sync of lookup data from Viewpoint

**Background Services:**
- Scheduled reference data synchronization
- Email queue processing
- Failed integration retry logic

### 3. Database Schema (SQL Server)

**Main Tables:**
- `HRRequests`: Parent request records (one per batch/submission)
- `HRRequestDetails`: Individual employee requests (references parent via ParentRequestId)
- `ReferenceData` tables: Companies, Positions, PayrollGroups, PhysicalLocations, etc.
- `Applications`: Available IT software/applications for request forms
- `EmailTemplates`: Editable notification templates by request type
- `AuditLog`: Basic tracking (CreatedBy, CreatedDate, ModifiedBy, ModifiedDate)
- `NotificationQueue`: Email notifications pending processing

**Design Principles:**
- Soft deletes on all tables (IsDeleted flag)
- Basic audit fields on all entities
- Parent-child relationship for multi-employee requests
- Separate tables for each request type details (PromotionDetails, LayoffDetails, etc.)

### 4. Integration Layer

**Viewpoint/Vista Integration:**
- **Employee API**: Real-time employee data for search and selection
- **Reference Data API**: Periodic sync of lookup values
- **Update Queue**: Queued processing of approved HR changes back to Viewpoint

**Authorization Integration:**
- Interface-based design to support both Viewpoint user profile fields and Entra group membership
- Returns list of companies user can submit requests for

**Email Notification System:**
- Database-driven queue for reliability
- Templated emails with request-specific data
- Support for multiple emails per request type
- Admin notification for failed integrations

## Key Design Decisions

### Data Strategy
- **Employee Data**: Real-time API calls to Viewpoint (no offline capability needed)
- **Reference Data**: Cached in local database, synced on schedule
- **Performance**: If real-time employee data proves too slow, implement intelligent caching

### Access Control
- **Company-Based Permissions**: Users can only submit for companies they have access to
- **Role-Based Access**: IT and HR roles (via Entra groups) can see all requests
- **Request Visibility**: Regular users only see requests they created

### Multi-Employee Requests
- Individual request records with ParentRequestId for batch operations
- Granular status tracking per employee
- Independent processing and error handling per employee

### Form Logic
- Conditional sections hard-coded in Angular components
- IT applications validated against database-maintained list
- Form validation on both client and server sides

### Error Handling & Resilience
- Retry failed Viewpoint integrations automatically
- Email admin on repeated failures
- User receives confirmation email regardless of background processing status
- Queue-based approach for all external integrations

### Configuration Management
- Environment-specific settings via appsettings.json files
- Separate deployment sites for frontend and backend
- Interface-based services for easy testing and implementation swapping

## Request Types

### 1. Promotion/Transfer Request
- Employee search and selection
- Current vs. new position comparison
- Conditional sections for additional resources:
  - Credit cards (Kwik Trip, Company Expense, Fuel Cardlock)
  - Company vehicle approval and classification
  - IT access (email, applications, folders/SharePoint)
- Dynamic grids for applications and folder management

### 2. Layoff Request
- Multi-employee selection capability
- Search modes: Individual or by Company/Division
- Paginated grid view with sorting
- Batch processing support
- Last day worked tracking

### 3. Termination Request
- Single employee selection
- Termination reason categorization
- Communication forwarding setup (email, phones)
- Automatic email reply configuration
- Unemployment contest tracking

### 4. Return to Work Request
- Multi-employee selection for previously laid-off employees
- Status filtering (laid-off employees only)
- Effective date for reinstatement
- Batch processing support

## Open Questions

### Technical Design Questions
1. **Complex Dropdown Dependencies**: How are dependent dropdowns (payroll company → payroll groups) modeled in Viewpoint? Should relationships be cached locally or resolved via API?

2. **Submission Workflow**: Detailed design needed for "what happens on submission" - immediate vs. queued processing, status updates, retry logic

3. **Viewpoint Update Integration**: Specific API endpoints and data formats for pushing HR changes back to Viewpoint

### Business Logic Questions
1. **Authorization Provider Implementation**: Final decision on Viewpoint user profile fields vs. Entra group membership for company access

2. **Email Template Management**: Admin interface design for managing email templates

3. **Reference Data Sync Schedule**: Frequency and timing for syncing lookup data from Viewpoint

## Development Phases

### Phase 1: Core Infrastructure
- Database schema and initial data
- .NET Core API with authentication
- Angular project setup with routing
- Basic CRUD operations

### Phase 2: Employee Integration
- Viewpoint employee search integration
- Reference data sync implementation
- Authorization provider implementation

### Phase 3: Request Forms
- Promotion/Transfer form with conditional logic
- Layoff and Termination forms
- Return to Work form
- Form validation and submission

### Phase 4: Dashboard & Management
- Request listing and filtering
- Status tracking and updates
- Email notification system
- Error handling and monitoring

### Phase 5: Production Readiness
- Performance optimization
- Security hardening
- Deployment automation
- Monitoring and logging

## Security Considerations

- Entra ID token validation on all API endpoints
- Company-based authorization on all employee data access
- Input validation and SQL injection prevention
- Audit logging for compliance
- Secure handling of employee PII data
- HTTPS enforcement across all communications

## Deployment Architecture

- **Frontend**: Static Angular files served by IIS Site 1
- **Backend**: .NET Core API hosted on IIS Site 2
- **Database**: SQL Server instance with connection pooling
- **Configuration**: Environment-specific appsettings.json files
- **Monitoring**: Application logs and health checks for integration points