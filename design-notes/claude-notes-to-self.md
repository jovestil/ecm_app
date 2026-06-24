# Claude Notes to Self - HR Employee Change Management Project

## Project Context & Decisions Made

### What We Built
- **HR Employee Change Management System** for construction company "Mathy"
- **Request Types**: Promotion/Transfer, Layoff, Termination, Return to Work (+ future NewHire)
- **Tech Stack**: Angular 19 frontend, .NET Core 9 API, SQL Server, on-prem IIS hosting
- **Integration**: Viewpoint/Vista ERP system, Entra ID auth, SMTP email

### Key Architecture Decisions
1. **Separate Angular + API deployments** (not hosted SPA)
2. **Real-time employee data** from Viewpoint (cached reference data only)
3. **Company-based authorization** (users can submit for specific companies)
4. **Individual request records with parent ID** (not single record with multiple employees)
5. **Request type/status at employee level** (moved from parent to child table)
6. **Shared detail tables** (CreditCard, Vehicle, IT details used by multiple request types)
7. **Interface-based authorization provider** (can switch Viewpoint → Entra groups)

### Database Schema Key Changes Made
- **INT primary keys** instead of UNIQUEIDENTIFIER
- **VARCHAR** instead of NVARCHAR throughout
- **RequestType/RequestStatus moved** from HRRequests to HRRequestDetails table
- **Added EmployeeNetworkId** for AD integration
- **Renamed promotion-specific tables** to generic names (CreditCardDetails, VehicleDetails, ITDetails, etc.)
- **Relinked shared tables** to HRRequestDetails instead of PromotionRequestDetails

### Critical Implementation Notes

#### Must Address Before Development
1. **Get Viewpoint API documentation** - endpoints, data structures, authentication
2. **Clarify employee status values** - how Viewpoint tracks Active/Laid-Off/Terminated
3. **User company access method** - Viewpoint user profile fields vs Entra group mapping
4. **Complex dropdown dependencies** - payroll company → group relationships

#### Development Approach
- **Start with mocked integrations** (IViewpointIntegrationService with mock implementation)
- **Interface-based design throughout** for easy testing and implementation swapping
- **Queue-based email system** for reliability
- **Comprehensive error handling** for integration failures

#### Form Complexity Notes
- **Promotion form is most complex** with conditional sections for IT access, credit cards, vehicles
- **Multi-employee selection** for layoffs and return-to-work
- **Real-time employee search** with company filtering
- **Side-by-side position comparison** for promotions

### File Structure Created
```
/mnt/c/repos/mathy-mockup/
├── ARCHITECTURE.md          # Complete system architecture
├── DATABASE_SCHEMA.md       # Full SQL Server schema  
├── API_DESIGN.md           # .NET Core API structure
├── INTEGRATION_INTERFACES.md # External system interfaces
├── CLAUDE.md               # Original project context
├── newhire/changes/        # Original HTML mockups
│   ├── hr_request_dashboard.html
│   ├── promotion_request_form.html
│   ├── layoff_request_form.html
│   ├── termination_request_form.html
│   └── return_to_work_form.html
└── claude-notes-to-self.md # This file
```

### Next Session Priorities
1. **Set up new repositories** (separate for frontend/backend)
2. **Start with .NET Core API project** - basic structure, authentication, mock services
3. **Create Entity Framework models** from database schema
4. **Implement core HR request CRUD** with mocked employee data
5. **Build Angular project structure** with routing and shared components

### Remember These Technical Details
- **Angular Material or similar** for UI components (not specified in mockups)
- **Entity Framework Core** for data access (not Dapper)
- **Background services** for reference data sync and email processing
- **Polly for retry policies** on Viewpoint API calls
- **Hangfire or similar** for background job processing
- **AutoMapper** for DTO mapping
- **FluentValidation** for request validation
- **Serilog** for structured logging

### Business Context to Remember
- **Construction/engineering company** with multiple locations (WI, MN, IA)
- **Multiple payroll companies** under Mathy umbrella (MTS, Mathy, Construction Plus, etc.)
- **Complex position codes** specific to construction industry
- **Managers submit requests for their employees** (not self-service)
- **HR and IT roles have elevated permissions** to see all requests
- **Email notifications critical** for workflow (confirmation + HR notification)

### Important Non-Requirements
- **No approval workflows** (just submit → process)
- **No offline capability needed** (desktop users)
- **No real-time status updates** (email confirmation sufficient)
- **No drafts initially** (will be added later for different forms)

This was a comprehensive architecture and design session. The client has clear requirements and we created a solid foundation for implementation. Ready to build!