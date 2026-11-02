# EALRMS Beginner Learning Guide (.NET 10 & PostgreSQL)

Welcome to the .NET ecosystem! This guide breaks down the core mechanisms powering EALRMS in simple, approachable terms.

---

## 1. What Happens When You Create an Application?

Here is the step-by-step journey of your data:

```
[ User Types in Browser ]
           │
           │ 1. Submits HTML Form (POST)
           ▼
[ ASP.NET Core MVC Routing ]
   Matches URL /Applications/Create to ApplicationsController.Create()
           │
           │ 2. Model Binding & Validation
           ▼
[ Model Binding ] ──> Copies form inputs into new Application() object
           │
           ▼
[ Model Validation ] ─> Checks [Required], [StringLength]
           │
   ┌───────┴───────┐
   │ IsValid = Yes │
   ▼               ▼ IsValid = No
[ EF Core ]     [ Return View with Errors ]
   │
   │ 3. Generates SQL INSERT
   ▼
[ PostgreSQL: ealrms_db ]
   Stores row into "Applications" table
           │
           │ 4. Redirects (HTTP 302)
           ▼
[ Browser Loads /Applications/Index ]
```

---

## 2. Key Concepts Explained

### 1. What is Model Binding?
When an HTML form submits fields named `Name` and `Technology`, ASP.NET Core matches them by name to properties on the `Application` class. You don't have to write manual parsing code like `request.Form["Name"]`.

### 2. How Validation Works
In `Models/Application.cs`, attributes decorate properties:
```csharp
[Required(ErrorMessage = "Application name is required.")]
[StringLength(100)]
public string Name { get; set; } = string.Empty;
```
When `ApplicationsController.Create()` receives the object, the framework runs those rules automatically and sets `ModelState.IsValid`. If `false`, the form re-renders displaying exact error messages next to the inputs.

### 3. How Entity Framework Core Translates C# to SQL
Instead of writing raw SQL strings, you write LINQ (Language Integrated Query) in C#:
```csharp
var apps = await _context.Applications
    .OrderByDescending(a => a.CreatedAt)
    .ToListAsync();
```
The Npgsql EF Core provider compiles this expression tree into native PostgreSQL SQL:
```sql
SELECT "a"."Id", "a"."CreatedAt", "a"."Description", "a"."Name", "a"."Status", "a"."Technology", "a"."UpdatedAt", "a"."Version"
FROM "Applications" AS "a"
ORDER BY "a"."CreatedAt" DESC;
```

### 4. Why Use Asynchronous Code (`async` / `await`)?
Whenever your application talks to PostgreSQL over the local network socket, there is a delay (latency).
- **Synchronous code**: The web server thread is frozen, blocked waiting for the database to reply.
- **Asynchronous code (`await`)**: The server thread is released back to the thread pool to serve other incoming HTTP requests while PostgreSQL processes the query. When PostgreSQL replies, a thread resumes execution.

### 5. Why Deletion Uses POST Instead of GET
If deletion happened on `GET /Applications/Delete/5`, any web crawler, browser pre-fetcher, or user clicking a link would inadvertently destroy data. A `POST` request ensures explicit user intent and allows CSRF token verification.

---

## 3. Useful Development Commands Reference

- **Build project**: `dotnet build`
- **Run project**: `dotnet run`
- **Apply migrations**: `dotnet tool run dotnet-ef database update`
- **Check PostgreSQL connection**: `psql -h localhost -U saatviksrivastava -d ealrms_db -c "SELECT count(*) FROM \"Applications\";"`
