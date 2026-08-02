# TaskBoard API

A clean architecture task management REST API built with ASP.NET Core 8, Entity Framework Core, and PostgreSQL — inspired by tools like Trello and Jira.

---

## Architecture

This project follows **Clean Architecture** principles with the **CQRS pattern** using MediatR, separating concerns into clearly defined layers:

```
src/
├── API/            → Controllers, middleware, Swagger configuration
├── Application/    → Use cases, CQRS handlers, validators, interfaces
├── Domain/         → Entities, enums, business rules
├── Infrastructure/ → JWT tokens, password hashing, external services
├── Persistence/    → EF Core DbContext, configurations, migrations
└── Shared/         → Common utilities, Result<T>, pagination
```

### Dependency Direction

```
API → Application → Domain
         ↑               ↑
   Persistence      Shared
         ↑
  Infrastructure
```

Domain has zero external dependencies. Application defines interfaces — Persistence and Infrastructure implement them. This means business logic is completely framework-agnostic and fully unit-testable.

---

## Tech Stack

| Category | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| Language | C# 12 |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 17 |
| Authentication | JWT Bearer + Stateless Refresh Tokens |
| Validation | FluentValidation 11 |
| Mapping | AutoMapper 16 |
| Logging | Serilog (Console + File sinks) |
| Mediator | MediatR 12 |
| API Docs | Swagger / OpenAPI (Swashbuckle) |
| Pattern | Clean Architecture + CQRS |

---

## Features

### Authentication & Security
- JWT access tokens (15 min expiry)
- Stateless refresh tokens (7 day expiry)
- BCrypt password hashing (work factor 12)
- Rate limiting (5 req/min on auth, 60 req/min on API)
- Role-based access within projects (Owner / Member)

### Project Management
- Create and manage projects
- Add/remove members with role assignment
- Project-level ownership checks on all mutations

### Kanban Board
- Task columns with drag-and-drop ordering support
- Tasks with priority levels (Low / Medium / High / Urgent)
- Task assignment to project members
- Move tasks between columns
- Due date tracking

### Data Integrity
- Soft delete with `IsDeleted` flag (recoverable)
- Cascade soft delete (deleting a project marks all children as deleted)
- EF Core global query filters (deleted records automatically excluded)
- Activity logging on task create, update, and move events

### Developer Experience
- MediatR pipeline behaviors (validation + logging run automatically on every request)
- Centralized exception handling middleware (maps exceptions to proper HTTP responses)
- Structured Serilog logging with daily rolling file sink
- Swagger UI with JWT authorization support
- Health check endpoint (`/health`)
- `dotnet user-secrets` for local secret management

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 17](https://www.postgresql.org/download/)

### 1. Clone the repository

```bash
git clone https://github.com/yourusername/taskboard-api.git
cd taskboard-api
```

### 2. Configure secrets

This project uses `dotnet user-secrets` to keep sensitive values off disk and out of source control.

```bash
cd src/API

dotnet user-secrets init

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=taskboard;Username=postgres;Password=YOUR_PASSWORD"

dotnet user-secrets set "JwtSettings:Secret" \
  "YOUR_SECRET_KEY_MINIMUM_32_CHARACTERS_LONG"
```

### 3. Create the database

Open pgAdmin (or psql) and create a database named `taskboard`.

### 4. Run migrations

```bash
dotnet ef database update \
  --project src/Persistence/TaskBoard.Persistence.csproj \
  --startup-project src/API/TaskBoard.API.csproj
```

### 5. Run the API

```bash
cd src/API
dotnet run
```

### 6. Open Swagger UI

```
https://localhost:{port}/swagger
```

Login via `POST /api/auth/login`, copy the `accessToken`, click **Authorize** in Swagger, and paste the token to test protected endpoints.

---

## API Endpoints

### Auth
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| POST | `/api/auth/register` | Register new account | No |
| POST | `/api/auth/login` | Login, get tokens | No |
| POST | `/api/auth/refresh` | Refresh access token | No |

### Projects
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| GET | `/api/projects` | Get all your projects | Yes |
| GET | `/api/projects/{id}` | Get project detail | Yes |
| POST | `/api/projects` | Create project | Yes |
| PUT | `/api/projects/{id}` | Update project | Yes (Owner) |
| DELETE | `/api/projects/{id}` | Soft delete project | Yes (Owner) |

### Columns
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| GET | `/api/projects/{id}/columns` | Get all columns | Yes (Member) |
| POST | `/api/projects/{id}/columns` | Create column | Yes (Member) |
| PUT | `/api/projects/{id}/columns/{columnId}` | Update column | Yes (Member) |
| DELETE | `/api/projects/{id}/columns/{columnId}` | Delete column | Yes (Member) |
| PATCH | `/api/projects/{id}/columns/reorder` | Reorder columns | Yes (Member) |

### Tasks
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| GET | `/api/tasks/column/{columnId}` | Get tasks by column | Yes (Member) |
| GET | `/api/tasks/{id}` | Get task detail | Yes (Member) |
| POST | `/api/tasks` | Create task | Yes (Member) |
| PUT | `/api/tasks/{id}` | Update task | Yes (Member) |
| PATCH | `/api/tasks/{id}/move` | Move task to column | Yes (Member) |
| DELETE | `/api/tasks/{id}` | Soft delete task | Yes (Member) |

### Comments
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| GET | `/api/comments/task/{taskId}` | Get comments on task | Yes (Member) |
| POST | `/api/comments` | Add comment | Yes (Member) |
| PUT | `/api/comments/{id}` | Edit comment | Yes (Author) |
| DELETE | `/api/comments/{id}` | Delete comment | Yes (Author/Owner) |

### Members
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| GET | `/api/projects/{id}/members` | List project members | Yes (Member) |
| POST | `/api/projects/{id}/members` | Add member | Yes (Owner) |
| PUT | `/api/projects/{id}/members/{userId}/role` | Update member role | Yes (Owner) |
| DELETE | `/api/projects/{id}/members/{userId}` | Remove member | Yes (Owner) |

### System
| Method | Endpoint | Description | Auth Required |
|---|---|---|---|
| GET | `/health` | API + DB health status | No |

---

## Project Structure

### Domain Layer
```
Domain/
├── Common/
│   ├── BaseEntity.cs        → Id + CreatedDate (auto-generated)
│   └── ISoftDelete.cs       → IsDeleted, Delete(), Restore()
├── Entities/
│   ├── User.cs              → encapsulated (protects PasswordHash)
│   ├── Project.cs           → soft deletable
│   ├── ProjectMember.cs     → encapsulated (pairs ProjectId+UserId+Role)
│   ├── TaskColumn.cs        → soft deletable
│   ├── TaskItem.cs          → soft deletable
│   ├── Comment.cs           → soft deletable
│   └── ActivityLog.cs       → encapsulated, write-once (no update methods)
└── Enums/
    ├── Priority.cs          → Low, Medium, High, Urgent
    └── ProjectRole.cs       → Member, Owner
```

### Application Layer — CQRS Features
```
Features/
├── Auth/
│   ├── Commands/    → RegisterCommand, LoginCommand, RefreshTokenCommand
│   └── DTOs/        → AuthResponseDto
├── Projects/
│   ├── Commands/    → CreateProjectCommand, UpdateProjectCommand, DeleteProjectCommand
│   ├── Queries/     → GetProjectsQuery, GetProjectByIdQuery
│   └── DTOs/        → ProjectDto, ProjectDetailDto
├── Columns/
│   ├── Commands/    → CreateColumnCommand, UpdateColumnCommand,
│   │                  DeleteColumnCommand, ReorderColumnsCommand
│   ├── Queries/     → GetColumnsByProjectQuery
│   └── DTOs/        → ColumnDto, ReorderColumnDto
├── Tasks/
│   ├── Commands/    → CreateTaskCommand, UpdateTaskCommand,
│   │                  MoveTaskCommand, DeleteTaskCommand
│   ├── Queries/     → GetTasksByColumnQuery, GetTaskByIdQuery
│   └── DTOs/        → TaskDto, TaskDetailDto, CommentDto, ActivityLogDto
├── Comments/
│   ├── Commands/    → CreateCommentCommand, UpdateCommentCommand, DeleteCommentCommand
│   ├── Queries/     → GetCommentsByTaskQuery
│   └── DTOs/        → CommentResponseDto
└── Members/
    ├── Commands/    → AddMemberCommand, RemoveMemberCommand, UpdateMemberRoleCommand
    ├── Queries/     → GetMembersByProjectQuery
    └── DTOs/        → MemberDto
```

### MediatR Pipeline (runs on every request)
```
Controller
  → MediatR.Send(command/query)
    → LoggingBehavior       (logs request name)
      → ValidationBehavior  (runs FluentValidation rules)
        → Handler           (business logic + DB access)
          → Result<T>       (success/failure wrapper, no exceptions for expected failures)
```

### Exception Handling
Custom exceptions map to HTTP status codes via `ExceptionHandlingMiddleware`:

| Exception | HTTP Status |
|---|---|
| `NotFoundException` | 404 Not Found |
| `UnauthorizedException` | 401 Unauthorized |
| `ForbiddenException` | 403 Forbidden |
| `ValidationException` | 400 Bad Request |
| Unhandled exception | 500 Internal Server Error |

---

## Configuration

`appsettings.json` contains non-sensitive defaults. Sensitive values are managed via `dotnet user-secrets` locally and environment variables in production.

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | (set via secrets) |
| `JwtSettings:Secret` | JWT signing key (min 32 chars) | (set via secrets) |
| `JwtSettings:Issuer` | JWT issuer | `TaskBoard.API` |
| `JwtSettings:Audience` | JWT audience | `TaskBoard.Client` |
| `JwtSettings:AccessTokenExpirationMinutes` | Access token lifetime | `15` |
| `JwtSettings:RefreshTokenExpirationDays` | Refresh token lifetime | `7` |

---

## Database Schema

```
Users           → Id, Username, Email, PasswordHash
Projects        → Id, Name, Description, OwnerId, IsDeleted
ProjectMembers  → Id, ProjectId, UserId, Role
TaskColumns     → Id, ProjectId, Name, Order, IsDeleted
Tasks           → Id, ColumnId, Title, Description, Priority, DueDate, AssignedUserId, IsDeleted
Comments        → Id, TaskId, UserId, Message, IsDeleted
ActivityLogs    → Id, TaskId, Action, CreatedBy, CreatedDate
```

---

## Design Decisions

**Why Clean Architecture?**
Keeps business logic (Application + Domain) completely independent of frameworks, databases, and delivery mechanisms. The entire Application layer is unit-testable without a real database or HTTP context.

**Why CQRS with MediatR?**
Each use case is one small, focused file. A bug in `DeleteProjectCommand` has zero risk of affecting `CreateProjectCommand`. New features are added by creating new files, not modifying existing ones.

**Why soft delete?**
Data recovery, audit trail consistency, and referential integrity. Activity logs and comments remain meaningful even after their parent entities are "deleted."

**Why stateless refresh tokens?**
Simpler infrastructure — no refresh token table, no DB lookup on every refresh. Trade-off: tokens can't be individually revoked before expiry. Swap to DB-backed tokens by implementing a new `ITokenService` — Application layer stays unchanged.

**Why FluentValidation over Data Annotations?**
Validation rules are separated from DTOs, support complex cross-field rules, and integrate cleanly into the MediatR pipeline so no handler ever needs to manually validate input.

---

## What's Next

Planned additions:
- [ ] User profile endpoints (`GET /api/users/me`, update profile, change password)
- [ ] User search endpoint (`GET /api/users/search`) for adding members
- [ ] CORS configuration for frontend integration
- [ ] Docker + docker-compose setup
- [ ] Unit tests for Application layer handlers
- [ ] Integration tests for API endpoints
- [ ] Frontend (React / Angular / Vue)

---

## License

MIT License — feel free to use this as a reference or starting point for your own projects.
