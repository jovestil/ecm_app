# \# ECM — Employee Change Management System

# 

# A full-stack enterprise HR workflow application that enables managers to submit, track, and process employee change requests — including promotions, terminations, layoffs, and return-to-work — with real-time notifications and third-party integrations.

# 

# \---

# 

# \## Tech Stack

# 

# | Layer | Technology |

# |---|---|

# | Frontend | Angular 17, PrimeNG, MSAL (Azure AD) |

# | Backend | ASP.NET Core 9 Web API, C# |

# | Database | MS SQL Server, Entity Framework Core |

# | Auth | Azure AD / Entra ID (JWT Bearer) |

# | Real-time | SignalR |

# | Background Jobs | Hangfire |

# | Email | Azure Service Bus + Azure Communication Services |

# | External Integration | Viewpoint/Vista API (ERP sync) |

# | Active Directory | On-premises AD provisioning via LDAP |

# 

# \---

# 

# \## Features

# 

# \### HR Request Types

# \- \*\*New Hire\*\* — Onboard new employees with IT provisioning, building access, and AD account creation

# \- \*\*Promotion / Transfer\*\* — Role changes with conditional IT access, credit cards, and vehicle approvals

# \- \*\*Termination\*\* — Single employee offboarding with communication forwarding and equipment return

# \- \*\*Layoff\*\* — Bulk employee layoff with batch processing support

# \- \*\*Return to Work\*\* — Reinstate previously laid-off employees

# 

# \### Core Capabilities

# \- \*\*Role-based access control\*\* — HR Admins, Managers, and IT roles via Entra ID groups

# \- \*\*Company-scoped permissions\*\* — Users can only submit requests for companies they have access to

# \- \*\*Real-time status updates\*\* — SignalR-powered dashboard reflecting live request progress

# \- \*\*Automated email notifications\*\* — Templated emails triggered at each workflow stage via Azure Service Bus

# \- \*\*Viewpoint ERP integration\*\* — Syncs employee data and pushes approved changes back to Viewpoint/Vista

# \- \*\*Active Directory provisioning\*\* — Automatically creates and updates AD accounts for new hires and promotions

# \- \*\*Background job processing\*\* — Hangfire-powered retry logic for failed integrations

# \- \*\*Audit trail\*\* — Full audit fields on all entities with soft deletes

# 

# \---

# 

# \## Architecture

# 

# ```

# ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐

# │   Angular 17     │     │  ASP.NET Core 9  │     │   SQL Server     │

# │   Frontend       │◄───►│   Web API        │◄───►│   Database       │

# │   (IIS Site 1)   │     │   (IIS Site 2)   │     │                  │

# └──────────────────┘     └──────────────────┘     └──────────────────┘

# &#x20;        │                        │

# &#x20;        ▼                        ▼

# ┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐

# │   Azure AD /     │     │  Viewpoint/Vista │     │  Azure Service   │

# │   Entra ID       │     │  ERP Integration │     │  Bus + Email     │

# └──────────────────┘     └──────────────────┘     └──────────────────┘

# &#x20;                                 │

# &#x20;                                 ▼

# &#x20;                        ┌──────────────────┐

# &#x20;                        │  Active Directory│

# &#x20;                        │  (LDAP / AD)     │

# &#x20;                        └──────────────────┘

# ```

# 

# \---

# 

# \## Project Structure

# 

# ```

# ecm\_app/

# ├── client/                        # Angular 17 frontend

# │   ├── src/app/

# │   │   ├── core/                  # Auth, interceptors, guards, services

# │   │   ├── features/              # HR request forms \& dashboard

# │   │   ├── shared/                # Reusable components

# │   │   └── models/                # TypeScript interfaces

# │   └── .env.example               # Frontend config template

# │

# ├── server/                        # ASP.NET Core 9 backend

# │   └── src/

# │       ├── Mathy.ELM.Api/         # Controllers, Hubs, Program.cs

# │       ├── Mathy.ELM.Core/        # Entities, DTOs, Interfaces

# │       └── Mathy.ELM.Infrastructure/  # Services, EF Migrations

# │

# └── design-notes/                  # Architecture docs, ERD, mockups

# ```

# 

# \---

# 

# \## Getting Started

# 

# \### Prerequisites

# 

# \- \[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

# \- \[Node.js v20+](https://nodejs.org/)

# \- \[Angular CLI 17](https://angular.io/cli) — `npm install -g @angular/cli@17`

# \- SQL Server (LocalDB, Developer, or Docker)

# \- \[EF Core Tools](https://learn.microsoft.com/ef/core/cli/dotnet) — `dotnet tool install --global dotnet-ef`

# 

# \### Backend Setup

# 

# ```bash

# cd server

# dotnet restore

# 

# \# Create local config (gitignored)

# \# Copy appsettings.json structure and fill in real values

# cp src/Mathy.ELM.Api/appsettings.json src/Mathy.ELM.Api/appsettings.Local.json

# 

# \# Run migrations

# cd src/Mathy.ELM.Api

# dotnet ef database update

# 

# \# Start API

# dotnet run

# \# API: https://localhost:7001

# \# Swagger: https://localhost:7001/swagger

# \# Hangfire: https://localhost:7001/hangfire

# ```

# 

# \### Frontend Setup

# 

# ```bash

# cd client

# npm install

# 

# \# Copy and fill in environment variables

# cp .env.example .env

# \# Edit .env with your Azure AD Client ID, Tenant ID, API URL

# 

# \# Start dev server

# npm start

# \# App: http://localhost:4200

# ```

# 

# \### Configuration

# 

# All sensitive values are loaded from gitignored local config files:

# 

# | File | Purpose |

# |---|---|

# | `server/.../appsettings.Local.json` | DB connection, Azure AD, API keys |

# | `client/.env` | MSAL Client ID, Tenant ID, API URL |

# 

# See `appsettings.json` and `client/.env.example` for required keys.

# 

# \---

# 

# \## API Overview

# 

# All endpoints follow REST conventions under `/api/v1/`:

# 

# | Resource | Endpoints |

# |---|---|

# | `auth` | Health check, current user, company access |

# | `employees` | Search, get by number, get by company |

# | `hr-requests` | CRUD, submit, status update |

# | `new-hire-requests` | Create and manage new hire details |

# | `promotion-requests` | Promotion/transfer workflows |

# | `termination-requests` | Termination offboarding |

# | `layoff-requests` | Bulk layoff processing |

# | `return-to-work` | Reinstatement workflows |

# | `reference-data` | Companies, positions, payroll groups |

# | `background-jobs` | Trigger/monitor Hangfire jobs |

# 

# Authentication: \*\*Bearer JWT token\*\* from Azure AD required on all endpoints.

# 

# \---

# 

# \## Security

# 

# \- All API endpoints protected with Entra ID JWT validation

# \- Company-scoped authorization — users only access their permitted companies

# \- Secrets managed via gitignored local config files — never committed to source control

# \- HTTPS enforced across all communication

# \- Input validation on both client and server

# 

# \---

# 

# \## Documentation

# 

# Additional technical documentation is available in the `design-notes/` folder:

# 

# \- `ARCHITECTURE.md` — System design and component overview

# \- `API\_DESIGN.md` — Full API specification

# \- `DATABASE\_SCHEMA.md` — Entity relationships and schema

# \- `AZURE\_AD\_APP\_REGISTRATIONS.md` — Authentication setup

# \- `BACKEND\_DEPLOYMENT\_IIS.md` / `FRONTEND\_DEPLOYMENT\_IIS.md` — Deployment guides

# \- `mockups/` — UI wireframes for all request forms

# 

# \---

# 

# \## License

# 

# Private repository — not licensed for public use.

