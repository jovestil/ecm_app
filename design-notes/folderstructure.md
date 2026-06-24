# Folder Structure - Mathy ELM System

This document outlines the complete folder structure for the Mathy Employee Change Management (ELM) System.

## **Mathy.ELM.sln** (Single Solution Structure)

```
Mathy.ELM/
├── Mathy.ELM.sln
├── .gitignore
├── README.md
├── CLAUDE.md
│
├── server/                                    # .NET Backend Projects
│   ├── src/                                   # Source code
│   │   ├── Mathy.ELM.Api/                     # Main Web API project
│   │   │   ├── Controllers/
│   │   │   │   ├── v1/
│   │   │   │   │   ├── HRRequestsController.cs
│   │   │   │   │   ├── EmployeesController.cs
│   │   │   │   │   ├── ReferenceDataController.cs
│   │   │   │   │   └── AuthorizationController.cs
│   │   │   │   └── BaseController.cs
│   │   │   ├── Middleware/
│   │   │   │   ├── AuthenticationMiddleware.cs
│   │   │   │   ├── AuthorizationMiddleware.cs
│   │   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   │   └── RequestLoggingMiddleware.cs
│   │   │   ├── Configuration/
│   │   │   │   ├── AutoMapperProfile.cs
│   │   │   │   ├── SwaggerConfiguration.cs
│   │   │   │   └── ServiceCollectionExtensions.cs
│   │   │   ├── Program.cs
│   │   │   ├── appsettings.json
│   │   │   ├── appsettings.Development.json
│   │   │   ├── appsettings.Production.json
│   │   │   └── Mathy.ELM.Api.csproj
│   │   │
│   │   ├── Mathy.ELM.Core/                    # Domain/Business Logic
│   │   │   ├── Entities/
│   │   │   │   ├── HRRequests/
│   │   │   │   │   ├── HRRequest.cs
│   │   │   │   │   ├── HRRequestDetail.cs
│   │   │   │   │   ├── PromotionRequestDetail.cs
│   │   │   │   │   ├── LayoffRequestDetail.cs
│   │   │   │   │   ├── TerminationRequestDetail.cs
│   │   │   │   │   └── ReturnToWorkRequestDetail.cs
│   │   │   │   ├── SharedDetails/
│   │   │   │   │   ├── CreditCardDetail.cs
│   │   │   │   │   ├── VehicleDetail.cs
│   │   │   │   │   ├── ITDetail.cs
│   │   │   │   │   ├── ApplicationRequest.cs
│   │   │   │   │   └── FolderRequest.cs
│   │   │   │   ├── ReferenceData/
│   │   │   │   │   ├── Company.cs
│   │   │   │   │   ├── PayrollGroup.cs
│   │   │   │   │   ├── PayrollDepartment.cs
│   │   │   │   │   ├── Position.cs
│   │   │   │   │   ├── PhysicalLocation.cs
│   │   │   │   │   ├── Application.cs
│   │   │   │   │   ├── RequestType.cs
│   │   │   │   │   └── RequestStatus.cs
│   │   │   │   └── Common/
│   │   │   │       ├── BaseEntity.cs
│   │   │   │       └── IAuditableEntity.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── Services/
│   │   │   │   │   ├── IHRRequestService.cs
│   │   │   │   │   ├── IEmployeeService.cs
│   │   │   │   │   ├── IViewpointIntegrationService.cs
│   │   │   │   │   ├── IEmailNotificationService.cs
│   │   │   │   │   ├── IReferenceDataService.cs
│   │   │   │   │   └── IAuthorizationService.cs
│   │   │   │   ├── Repositories/
│   │   │   │   │   ├── IHRRequestRepository.cs
│   │   │   │   │   ├── IReferenceDataRepository.cs
│   │   │   │   │   └── IGenericRepository.cs
│   │   │   │   └── External/
│   │   │   │       ├── IViewpointApiClient.cs
│   │   │   │       └── IEmailProvider.cs
│   │   │   ├── DTOs/
│   │   │   │   ├── Requests/
│   │   │   │   │   ├── CreateHRRequestDto.cs
│   │   │   │   │   ├── UpdateHRRequestDto.cs
│   │   │   │   │   ├── HRRequestDto.cs
│   │   │   │   │   └── HRRequestDetailDto.cs
│   │   │   │   ├── Employees/
│   │   │   │   │   ├── EmployeeDto.cs
│   │   │   │   │   └── EmployeeSearchDto.cs
│   │   │   │   ├── ReferenceData/
│   │   │   │   │   ├── CompanyDto.cs
│   │   │   │   │   ├── PositionDto.cs
│   │   │   │   │   └── PayrollGroupDto.cs
│   │   │   │   └── Common/
│   │   │   │       ├── ApiResponse.cs
│   │   │   │       └── PaginatedResponse.cs
│   │   │   ├── Enums/
│   │   │   │   ├── RequestTypes.cs
│   │   │   │   ├── RequestStatuses.cs
│   │   │   │   └── EmployeeStatuses.cs
│   │   │   ├── Constants/
│   │   │   │   ├── ApiRoutes.cs
│   │   │   │   └── BusinessConstants.cs
│   │   │   └── Mathy.ELM.Core.csproj
│   │
│   │   ├── Mathy.ELM.Infrastructure/           # Data Access & External Services
│   │   │   ├── Data/
│   │   │   │   ├── ApplicationDbContext.cs
│   │   │   │   ├── Configurations/
│   │   │   │   │   ├── HRRequestConfiguration.cs
│   │   │   │   │   ├── EmployeeConfiguration.cs
│   │   │   │   │   └── ReferenceDataConfiguration.cs
│   │   │   │   └── Migrations/
│   │   │   ├── Repositories/
│   │   │   │   ├── HRRequestRepository.cs
│   │   │   │   ├── ReferenceDataRepository.cs
│   │   │   │   └── GenericRepository.cs
│   │   │   ├── Services/
│   │   │   │   ├── HRRequestService.cs
│   │   │   │   ├── EmployeeService.cs
│   │   │   │   ├── ViewpointIntegrationService.cs
│   │   │   │   ├── EmailNotificationService.cs
│   │   │   │   ├── ReferenceDataService.cs
│   │   │   │   └── AuthorizationService.cs
│   │   │   ├── BackgroundServices/
│   │   │   │   ├── ReferenceDataSyncService.cs
│   │   │   │   └── EmailProcessingService.cs
│   │   │   ├── External/
│   │   │   │   ├── ViewpointApiClient.cs
│   │   │   │   └── SmtpEmailProvider.cs
│   │   │   └── Mathy.ELM.Infrastructure.csproj
│   │   │
│   │   └── Mathy.ELM.Tests/                   # Test Projects
│   │       ├── Unit/
│   │       │   ├── Services/
│   │       │   ├── Controllers/
│   │       │   └── Repositories/
│   │       ├── Integration/
│   │       │   ├── Api/
│   │       │   └── Database/
│   │       ├── TestHelpers/
│   │       │   ├── MockServices/
│   │       │   └── TestData/
│   │       └── Mathy.ELM.Tests.csproj
│
├── client/                                    # Angular 19 Frontend
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                          # Singleton services, guards, interceptors
│   │   │   │   ├── services/
│   │   │   │   │   ├── auth/
│   │   │   │   │   │   ├── auth.service.ts
│   │   │   │   │   │   └── token.service.ts
│   │   │   │   │   ├── api/
│   │   │   │   │   │   ├── api.service.ts
│   │   │   │   │   │   ├── hr-requests-api.service.ts
│   │   │   │   │   │   ├── employees-api.service.ts
│   │   │   │   │   │   ├── reference-data-api.service.ts
│   │   │   │   │   │   └── authorization-api.service.ts
│   │   │   │   │   ├── notification/
│   │   │   │   │   │   └── notification.service.ts
│   │   │   │   │   └── state/
│   │   │   │   │       ├── user-state.service.ts
│   │   │   │   │       └── reference-data-state.service.ts
│   │   │   │   ├── guards/
│   │   │   │   │   ├── auth.guard.ts
│   │   │   │   │   └── company-authorization.guard.ts
│   │   │   │   ├── interceptors/
│   │   │   │   │   ├── auth.interceptor.ts
│   │   │   │   │   ├── error.interceptor.ts
│   │   │   │   │   └── loading.interceptor.ts
│   │   │   │   ├── models/
│   │   │   │   │   ├── api-response.model.ts
│   │   │   │   │   ├── pagination.model.ts
│   │   │   │   │   ├── user.model.ts
│   │   │   │   │   └── error.model.ts
│   │   │   │   └── core.module.ts
│   │   │   │
│   │   │   ├── shared/                        # Reusable components, pipes, directives
│   │   │   │   ├── components/
│   │   │   │   │   ├── layout/
│   │   │   │   │   │   ├── header/
│   │   │   │   │   │   │   ├── header.component.ts
│   │   │   │   │   │   │   ├── header.component.html
│   │   │   │   │   │   │   └── header.component.scss
│   │   │   │   │   │   └── navigation/
│   │   │   │   │   │       ├── back-button/
│   │   │   │   │   │       └── breadcrumb/
│   │   │   │   │   ├── forms/
│   │   │   │   │   │   ├── employee-search/
│   │   │   │   │   │   │   ├── employee-search.component.ts
│   │   │   │   │   │   │   ├── employee-search.component.html
│   │   │   │   │   │   │   └── employee-search.component.scss
│   │   │   │   │   │   ├── form-section/
│   │   │   │   │   │   ├── validation-messages/
│   │   │   │   │   │   └── conditional-section/
│   │   │   │   │   ├── data-grid/
│   │   │   │   │   │   ├── data-grid.component.ts
│   │   │   │   │   │   ├── data-grid.component.html
│   │   │   │   │   │   ├── data-grid.component.scss
│   │   │   │   │   │   └── models/
│   │   │   │   │   │       ├── column-definition.model.ts
│   │   │   │   │   │       └── grid-config.model.ts
│   │   │   │   │   ├── modals/
│   │   │   │   │   │   ├── confirmation-modal/
│   │   │   │   │   │   └── selection-modal/
│   │   │   │   │   └── ui/
│   │   │   │   │       ├── loading-spinner/
│   │   │   │   │       ├── status-badge/
│   │   │   │   │       └── card/
│   │   │   │   ├── pipes/
│   │   │   │   │   ├── date-format.pipe.ts
│   │   │   │   │   ├── status-display.pipe.ts
│   │   │   │   │   └── safe-html.pipe.ts
│   │   │   │   ├── directives/
│   │   │   │   │   ├── auto-focus.directive.ts
│   │   │   │   │   └── click-outside.directive.ts
│   │   │   │   ├── validators/
│   │   │   │   │   ├── custom-validators.ts
│   │   │   │   │   └── async-validators.ts
│   │   │   │   ├── models/
│   │   │   │   │   ├── base.model.ts
│   │   │   │   │   └── form-config.model.ts
│   │   │   │   └── shared.module.ts
│   │   │   │
│   │   │   ├── features/                      # Feature modules
│   │   │   │   ├── dashboard/
│   │   │   │   │   ├── components/
│   │   │   │   │   │   ├── dashboard/
│   │   │   │   │   │   │   ├── dashboard.component.ts
│   │   │   │   │   │   │   ├── dashboard.component.html
│   │   │   │   │   │   │   └── dashboard.component.scss
│   │   │   │   │   │   ├── request-list/
│   │   │   │   │   │   └── request-type-modal/
│   │   │   │   │   ├── services/
│   │   │   │   │   │   └── dashboard.service.ts
│   │   │   │   │   ├── models/
│   │   │   │   │   │   └── dashboard.model.ts
│   │   │   │   │   ├── dashboard-routing.module.ts
│   │   │   │   │   └── dashboard.module.ts
│   │   │   │   │
│   │   │   │   ├── hr-requests/
│   │   │   │   │   ├── components/
│   │   │   │   │   │   ├── promotion/
│   │   │   │   │   │   │   ├── promotion-form/
│   │   │   │   │   │   │   │   ├── promotion-form.component.ts
│   │   │   │   │   │   │   │   ├── promotion-form.component.html
│   │   │   │   │   │   │   │   └── promotion-form.component.scss
│   │   │   │   │   │   │   ├── position-comparison/
│   │   │   │   │   │   │   ├── it-access-section/
│   │   │   │   │   │   │   ├── credit-card-section/
│   │   │   │   │   │   │   ├── vehicle-section/
│   │   │   │   │   │   │   └── application-folder-grid/
│   │   │   │   │   │   ├── layoff/
│   │   │   │   │   │   │   ├── layoff-form/
│   │   │   │   │   │   │   ├── employee-selection-grid/
│   │   │   │   │   │   │   └── company-browser/
│   │   │   │   │   │   ├── termination/
│   │   │   │   │   │   │   ├── termination-form/
│   │   │   │   │   │   │   └── communication-forwarding/
│   │   │   │   │   │   └── return-to-work/
│   │   │   │   │   │       ├── return-to-work-form/
│   │   │   │   │   │       └── laid-off-employee-grid/
│   │   │   │   │   ├── services/
│   │   │   │   │   │   ├── hr-request.service.ts
│   │   │   │   │   │   ├── promotion-request.service.ts
│   │   │   │   │   │   ├── layoff-request.service.ts
│   │   │   │   │   │   ├── termination-request.service.ts
│   │   │   │   │   │   └── return-to-work-request.service.ts
│   │   │   │   │   ├── models/
│   │   │   │   │   │   ├── hr-request.model.ts
│   │   │   │   │   │   ├── promotion-request.model.ts
│   │   │   │   │   │   ├── layoff-request.model.ts
│   │   │   │   │   │   ├── termination-request.model.ts
│   │   │   │   │   │   ├── return-to-work-request.model.ts
│   │   │   │   │   │   └── shared-details.model.ts
│   │   │   │   │   ├── resolvers/
│   │   │   │   │   │   └── reference-data.resolver.ts
│   │   │   │   │   ├── hr-requests-routing.module.ts
│   │   │   │   │   └── hr-requests.module.ts
│   │   │   │   │
│   │   │   │   ├── employees/
│   │   │   │   │   ├── components/
│   │   │   │   │   │   ├── employee-search/
│   │   │   │   │   │   └── employee-details/
│   │   │   │   │   ├── services/
│   │   │   │   │   │   └── employee.service.ts
│   │   │   │   │   ├── models/
│   │   │   │   │   │   └── employee.model.ts
│   │   │   │   │   ├── employees-routing.module.ts
│   │   │   │   │   └── employees.module.ts
│   │   │   │   │
│   │   │   │   └── reference-data/
│   │   │   │       ├── services/
│   │   │   │       │   └── reference-data.service.ts
│   │   │   │       ├── models/
│   │   │   │       │   ├── company.model.ts
│   │   │   │       │   ├── position.model.ts
│   │   │   │       │   ├── payroll-group.model.ts
│   │   │   │       │   └── application.model.ts
│   │   │   │       └── reference-data.module.ts
│   │   │   │
│   │   │   ├── app-routing.module.ts
│   │   │   ├── app.component.ts
│   │   │   ├── app.component.html
│   │   │   ├── app.component.scss
│   │   │   └── app.module.ts
│   │   │
│   │   ├── assets/
│   │   │   ├── images/
│   │   │   │   ├── logos/
│   │   │   │   │   └── mathy-seal.png
│   │   │   │   └── icons/
│   │   │   ├── styles/
│   │   │   │   ├── _variables.scss
│   │   │   │   ├── _mixins.scss
│   │   │   │   ├── _components.scss
│   │   │   │   ├── _primeng-theme.scss
│   │   │   │   └── _utilities.scss
│   │   │   └── config/
│   │   │       └── app-config.json
│   │   │
│   │   ├── environments/
│   │   │   ├── environment.ts
│   │   │   ├── environment.development.ts
│   │   │   └── environment.production.ts
│   │   │
│   │   ├── styles.scss
│   │   ├── main.ts
│   │   └── index.html
│   │
│   ├── angular.json
│   ├── package.json
│   ├── tsconfig.json
│   ├── tsconfig.app.json
│   ├── tsconfig.spec.json
│   ├── karma.conf.js
│   ├── .eslintrc.json
│   └── README.md
│
├── database/                                  # Database Scripts & Migrations
│   ├── scripts/
│   │   ├── 001_initial_schema.sql
│   │   ├── 002_reference_data.sql
│   │   └── 003_normalize_request_types.sql
│   └── migrations/
│
├── docs/                                      # Documentation
│   ├── design-notes/
│   │   ├── ARCHITECTURE.md
│   │   ├── API_DESIGN.md
│   │   ├── DATABASE_SCHEMA.md
│   │   ├── INTEGRATION_INTERFACES.md
│   │   ├── folderstructure.md
│   │   └── mockups/
│   │       ├── hr_request_dashboard.html
│   │       ├── promotion_request_form.html
│   │       ├── layoff_request_form.html
│   │       ├── termination_request_form.html
│   │       └── return_to_work_form.html
│   ├── api/
│   │   └── openapi.json
│   └── deployment/
│       ├── iis-setup.md
│       └── configuration-guide.md
│
├── scripts/                                   # Build & Deployment Scripts
│   ├── build.ps1
│   ├── deploy.ps1
│   └── setup-dev.ps1
│
└── .github/                                   # GitHub Actions (if using GitHub)
    └── workflows/
        ├── build-and-test.yml
        └── deploy.yml
```

## Key Changes for ELM Naming

1. **Solution Name**: `Mathy.ELM.sln` (Employee Change Management)
2. **Project Names**: 
   - `Mathy.ELM.Api`
   - `Mathy.ELM.Core` 
   - `Mathy.ELM.Infrastructure`
   - `Mathy.ELM.Tests`
3. **Namespace Structure**: All C# code will use `Mathy.ELM.*` namespaces
4. **Angular App**: Remains in `client/` folder as the frontend application

## Benefits of This Structure

- **Single Solution**: Unified development and build process
- **Clear Separation**: Frontend (`client/`) and backend (`server/`) clearly separated
- **Scalable**: Easy to add new projects or features
- **Maintainable**: Logical organization following .NET and Angular best practices
- **Deployable**: Separate deployment paths for API and client applications
- **Testable**: Comprehensive test structure for all layers

## Next Steps

1. Install .NET SDK
2. Create the solution and projects using `dotnet new` commands
3. Set up Angular CLI and create the client application
4. Configure project references and dependencies
5. Set up build scripts for both frontend and backend