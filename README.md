# EALRMS — Enterprise Application Lifecycle & Release Management Syste
EALRMS is a centralized enterprise platform designed to manage application inventories, software releases, and technical issues across an organization.

This repository contains **MVP 1.0**, focusing on **Application Inventory Management** using C#, ASP.NET Core MVC, Entity Framework Core, and PostgreSQL
--
## 1. Business Problem

Modern software enterprises maintain dozens or hundreds of internal services, APIs, customer portals, and background jobs. Without a centralized inventory:
- Engineering leaders lose visibility into which technology stacks are running in production.
- Teams struggle to track decommissioned vs. active services.
- Audit trails for when systems were introduced or modified become fragmented across spreadsheets.

**EALRMS MVP 1.0** solves this by establishing a single source of truth for all enterprise applications with end-to-end CRUD operations, reliable auditing, and robust validation.

---

## 2. MVP 1.0 Core Features

1. **Application Inventory Table (`/Applications`)**:
   - Lists all applications with IDs, names, technology stacks, versions, lifecycle badges (Active/Inactive), and creation timestamps.
   - Informative empty state when no applications are registered.
2. **Registration Form (`/Applications/Create`)**:
   - Register new applications with server-side validation.
   - Enforces name (max 100 chars), technology (max 100 chars), version, and status validation.
3. **Application Details (`/Applications/Details/{id}`)**:
   - Complete architectural overview of an application with UTC timestamps and database metadata.
4. **Application Modification (`/Applications/Edit/{id}`)**:
   - Edit metadata while protecting immutable system-managed creation timestamps.
5. **Safe Deletion Flow (`/Applications/Delete/{id}`)**:
   - Dedicated confirmation screen. Deletion requires an explicit `POST` request protected by anti-forgery tokens.
6. **PostgreSQL Persistence**:
   - Direct integration using `Npgsql.EntityFrameworkCore.PostgreSQL`.
   - UTC timestamp synchronization with PostgreSQL `timestamp with time zone` (`timestamptz`).

---

## 3. Technology Stack

- **Platform**: .NET 10 (C# 14 / ASP.NET Core MVC)
- **Database**: PostgreSQL (host: `localhost:5432`, db: `ealrms_db`)
- **ORM**: Entity Framework Core 10.0 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Frontend**: Razor Views, Bootstrap 5, Custom Enterprise Design System
- **OS Support**: macOS (Apple Silicon ARM64 / Darwin), Linux, Windows

---

## 4. Prerequisites

1. **.NET 10 SDK** (verified with `dotnet --info`)
2. **PostgreSQL 14+** running locally on port 5432:
   ```bash
   pg_isready -h localhost -p 5432
   ```

---

## 5. PostgreSQL Configuration & Credentials

The connection string is configured in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ealrms_db;Username=saatviksrivastava"
  }
}
```

### Password Handling
To adhere to enterprise security standards, **passwords are never committed to Git**.
If your local PostgreSQL installation requires a password, set it as an environment variable in your terminal before launching the app:

```bash
export EALRMS_DB_PASSWORD="your_postgres_password"
# or use standard PostgreSQL variable:
export PGPASSWORD="your_postgres_password"
```

The application's `Program.cs` automatically detects and injects this variable into `NpgsqlConnectionStringBuilder` at runtime.

---

## 6. Database Migrations

Entity Framework Core migrations manage schema evolution:

1. **Install the EF Core tool** (if not already restored):
   ```bash
   dotnet tool restore
   ```
2. **Apply existing migrations to PostgreSQL**:
   ```bash
   dotnet tool run dotnet-ef database update
   ```
3. **Add a new migration** (for future schema enhancements):
   ```bash
   dotnet tool run dotnet-ef migrations add <MigrationName>
   ```

---

## 7. How to Run the Application

Execute the following in the repository root:

```bash
dotnet run
```

Then open your browser to the URL displayed in the console output (typically `http://localhost:5000` or `https://localhost:5001`).

---

## 8. Project Structure

```
EALRMS/
├── Controllers/
│   ├── ApplicationsController.cs   # All CRUD actions, validation, and async EF Core operations
│   └── HomeController.cs           # Diagnostic and home routing
├── Data/
│   └── ApplicationDbContext.cs     # EF Core DbContext, PostgreSQL table & constraint mapping
├── Models/
│   ├── Application.cs              # Application entity with data annotations & validation
│   └── ErrorViewModel.cs           # Diagnostic error model
├── Views/
│   ├── Applications/
│   │   ├── Index.cshtml            # Application catalog table & empty state
│   │   ├── Create.cshtml           # Registration form
│   │   ├── Edit.cshtml             # Update form with read-only audit stamps
│   │   ├── Details.cshtml          # Full application record view
│   │   └── Delete.cshtml           # Deletion confirmation page (POST-only action)
│   ├── Shared/
│   │   ├── _Layout.cshtml          # Enterprise layout, responsive navbar & flash banners
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── wwwroot/
│   ├── css/
│   │   └── site.css                # Enterprise design system styles & status badges
│   └── js/
├── docs/
│   ├── ARCHITECTURE.md             # System architecture & EF Core / PostgreSQL pipeline
│   └── LEARNING_GUIDE.md           # Beginner-friendly guide explaining .NET MVC concepts
├── Migrations/                     # EF Core migration history and snapshots
├── appsettings.json                # Database connection string & log configuration
├── Program.cs                      # Dependency Injection container & HTTP middleware pipeline
├── EALRMS.csproj                   # Project configuration & NuGet dependencies
└── README.md
```

---

## 9. Known Limitations in MVP 1.0

- Authentication and role-based permissions (RBAC) are not yet enabled.
- Audit logs are limited to `CreatedAt` and `UpdatedAt` timestamps.
- Single-entity scope (no relational joins to issues or release plans yet).

---

## 10. Future Development Roadmap

- **v1.1**: Search, filtering, and column sorting.
- **v1.2**: Issue & bug tracking module linked to applications.
- **v1.3**: Developer assignment & verification notes.
- **v1.4**: Release management (release schedules, deployment versions).
- **v1.5**: Analytics dashboard.
- **v1.6**: Identity & RBAC.
