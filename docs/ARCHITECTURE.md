# EALRMS Architectural Specification

This document details the architectural design, patterns, and component interactions of the Enterprise Application Lifecycle & Release Management System (MVP 1.0).

---

## 1. Architectural Style: Model-View-Controller (MVC)

EALRMS adopts the standard ASP.NET Core MVC architecture:

```
[ Browser / HTTP Client ]
           │
           ▼
    [ Middleware Pipeline ] (Routing, Static Files, Antiforgery)
           │
           ▼
    [ ApplicationsController ] ◄── Injected with ── [ ApplicationDbContext ]
           │                                                │
           ├─── Sends Model Data ───┐                       │
           ▼                        ▼                       ▼
      [ Razor Views ]          [ C# Model ]       [ PostgreSQL Database ]
   (.cshtml -> HTML/CSS)     (Application.cs)       ("Applications" Table)
```

### Components
1. **Model (`Models/Application.cs`)**:
   - Represents the core business entity.
   - Contains data validation rules (e.g. `[Required]`, `[StringLength]`, `[RegularExpression]`).
2. **View (`Views/Applications/*.cshtml`)**:
   - Razor templates combining HTML with C# code to render dynamic pages.
   - Leverages Tag Helpers (`asp-action`, `asp-for`, `asp-route-id`) for clean markup.
3. **Controller (`Controllers/ApplicationsController.cs`)**:
   - Coordinates business workflow.
   - Handles incoming HTTP requests, performs validations, queries the database, and selects views.

---

## 2. Request and Response Lifecycle

When a user submits the "Register Application" form:
1. **HTTP Request**: The browser issues a `POST` request to `/Applications/Create` with form parameters.
2. **Antiforgery Validation**: ASP.NET Core verifies the CSRF token generated in the hidden form input.
3. **Model Binding**: The framework automatically converts incoming form fields into an `Application` C# object.
4. **Model Validation**: The framework executes attributes specified on `Application`. Results are captured in `ModelState.IsValid`.
5. **Persistence**:
   - If valid, `ApplicationsController` calls `_context.Add(application)` and `await _context.SaveChangesAsync()`.
   - EF Core translates the C# call to an SQL `INSERT INTO "Applications" ...` statement executed against PostgreSQL.
   - The user is redirected (`HTTP 302`) to `/Applications/Index`.
6. **Error Case**:
   - If invalid, the controller returns the current view with the existing model. Validation errors are rendered inline.

---

## 3. Dependency Injection (DI)

ASP.NET Core includes a built-in IoC (Inversion of Control) container configured in `Program.cs`:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### Lifetime: Scoped
`ApplicationDbContext` is registered with a **Scoped** lifetime:
- A new instance is created for each incoming HTTP request.
- The same instance is shared across all components handling that specific request.
- It is safely disposed of when the request finishes, releasing database connections back to Npgsql's connection pool.

---

## 4. Entity Framework Core & PostgreSQL (Npgsql)

EF Core acts as an Object-Relational Mapper (ORM). 

### Schema Mapping
| C# Property (`Application.cs`) | PostgreSQL Column (`"Applications"`) | Constraints / Rules |
| :--- | :--- | :--- |
| `int Id` | `integer` | Primary Key, Identity auto-increment |
| `string Name` | `character varying(100)` | NOT NULL |
| `string? Description` | `character varying(500)` | NULL |
| `string Technology` | `character varying(100)` | NOT NULL |
| `string? Version` | `character varying(30)` | NULL |
| `string Status` | `character varying(20)` | NOT NULL, Default: 'Active' |
| `DateTime CreatedAt` | `timestamp with time zone` | NOT NULL, UTC |
| `DateTime? UpdatedAt` | `timestamp with time zone` | NULL, UTC |

### Database Migrations
Migrations translate C# schema definitions into versioned SQL scripts.
- The `__EFMigrationsHistory` table in PostgreSQL records every migration applied.
- `dotnet ef database update` applies pending schema changes idempotently.
