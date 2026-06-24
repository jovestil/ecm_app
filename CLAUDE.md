# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an HR Employee Change Management System.

The system allows managers to submit HR requests (promotions, layoffs, terminations, return-to-work) and integrates with Viewpoint/Vista for employee data and Entra ID for authentication.

## Architecture

### Technology Stack
- **Backend**: .NET Core 9 Web API hosted on IIS
- **Frontend**: Angular 19 (separate IIS deployment)
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: Entra ID (Azure AD) with JWT tokens
- **Integration**: Viewpoint/Vista API for employee data
- **Email**: SMTP-based notification system

### Key Components
1. **Controllers**: HRRequestsController, EmployeeController, ReferenceDataController, AuthorizationController
2. **Services**: IHRRequestService, IEmployeeService, IViewpointIntegrationService, IEmailNotificationService
3. **Background Services**: Reference data sync and email processing
4. **Database**: Parent-child request structure with type-specific detail tables

## Database Schema

The database uses a parent-child relationship pattern:
- `HRRequests`: Parent request records
- `HRRequestDetails`: Individual employee requests (references parent via ParentRequestId)
- Type-specific tables: `PromotionRequestDetails`, `LayoffRequestDetails`, etc.
- Reference data tables: `Companies`, `PayrollGroups`, `Positions`, `Applications`
- Soft deletes with `IsDeleted` flag on all tables

## Request Types

1. **Promotion/Transfer**: Employee position changes with conditional IT access, credit cards, and vehicle approvals
2. **Layoff**: Multi-employee selection with batch processing
3. **Termination**: Single employee with communication forwarding setup
4. **Return to Work**: Multi-employee selection for previously laid-off employees

## API Design

RESTful API following consistent patterns:
- Base URL: `/api/v1/{resource}`
- Standardized response format with success/error structure
- Pagination support for list endpoints
- Comprehensive validation and error handling

## Security & Authorization

- All endpoints require Entra ID JWT authentication
- Company-based authorization (users can only access their authorized companies)
- Role-based access for HR/IT personnel
- Input validation and audit logging throughout

## Integration Points

- **Viewpoint/Vista**: Real-time employee search and reference data sync
- **Email System**: Queue-based notification processing
- **Authorization Provider**: Interface-based design supporting both Viewpoint profiles and Entra groups

## Development Notes

This is primarily a design-phase project with extensive documentation in `design-notes/`:
- `ARCHITECTURE.md`: Complete system architecture and design decisions
- `API_DESIGN.md`: Detailed API endpoint specifications and DTOs
- `DATABASE_SCHEMA.md`: Complete database schema with scripts
- `INTEGRATION_INTERFACES.md`: External system integration details
- `mockups/`: HTML mockup files for various HR request forms and dashboard

The codebase appears to be in planning/design phase with mockup files present but no actual implementation code yet.