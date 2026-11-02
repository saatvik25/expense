using Microsoft.EntityFrameworkCore;
using Npgsql;
using EALRMS.Data;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. DEPENDENCY INJECTION (SERVICE REGISTRATION)
// ==============================================================================
// Dependency Injection (DI) is a software design pattern where objects (services)
// are provided to other classes that depend on them, rather than creating them directly.

// Register ASP.NET Core MVC controllers and Razor view rendering services.
builder.Services.AddControllersWithViews();

// Read PostgreSQL connection string from appsettings.json or environment variables.
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in configuration.");

// Secure password handling: If an environment variable (EALRMS_DB_PASSWORD or PGPASSWORD)
// is provided, inject it securely into the connection string without exposing secrets in files.
var envPassword = Environment.GetEnvironmentVariable("EALRMS_DB_PASSWORD") 
               ?? Environment.GetEnvironmentVariable("PGPASSWORD");

var connectionStringBuilder = new NpgsqlConnectionStringBuilder(rawConnectionString);
if (!string.IsNullOrEmpty(envPassword) && string.IsNullOrEmpty(connectionStringBuilder.Password))
{
    connectionStringBuilder.Password = envPassword;
}

// Register ApplicationDbContext with Scoped lifetime.
// A scoped lifetime means one instance of ApplicationDbContext is created per HTTP request
// and disposed automatically at the end of the request.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Use the official Npgsql PostgreSQL provider
    options.UseNpgsql(connectionStringBuilder.ConnectionString);

    // In development mode, log detailed EF Core diagnostic info
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

var app = builder.Build();

// ==============================================================================
// 2. HTTP REQUEST PIPELINE (MIDDLEWARE)
// ==============================================================================
// Middleware components form a pipeline that processes incoming HTTP requests
// and outgoing responses sequentially.

if (!app.Environment.IsDevelopment())
{
    // Production error handler
    app.UseExceptionHandler("/Home/Error");
    // HTTP Strict Transport Security
    app.UseHsts();
}
else
{
    // Developer exception page provides detailed error reports during development
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Serve static files (CSS, JavaScript, images) from wwwroot folder
app.MapStaticAssets();

// Configure the default route: / -> Applications/Index
// We route directly to ApplicationsController as the primary entry point of EALRMS.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Applications}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
