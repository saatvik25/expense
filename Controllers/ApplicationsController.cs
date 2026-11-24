using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EALRMS.Data;
using EALRMS.Models;

namespace EALRMS.Controllers;

/// <summary>
/// Controller responsible for managing Application lifecycle inventory.
/// Handles standard CRUD operations (Create, Read, Update, Delete) for enterprise applications.
/// 
/// Interacts directly with Entity Framework Core via dependency injection of ApplicationDbContext.
/// All database operations are asynchronous to optimize throughput on the web server thread pool.
/// </summary>
public class ApplicationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationsController> _logger;

    /// <summary>
    /// Constructor with Dependency Injection.
    /// ASP.NET Core supplies the registered scoped ApplicationDbContext and ILogger instances.
    /// </summary>
    public ApplicationsController(ApplicationDbContext context, ILogger<ApplicationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================================
    // 1. READ: List Applications (GET: /Applications or /Applications/Index)
    // =========================================================================
    /// <summary>
    /// Retrieves all applications from the PostgreSQL database and renders the Index view.
    /// Uses AsNoTracking() for read-only queries to bypass EF Core change tracking overhead.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Retrieve records ordered by most recently created first
        var applications = await _context.Applications
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View(applications);
    }

    // =========================================================================
    // 2. READ: Application Details (GET: /Applications/Details/5)
    // =========================================================================
    /// <summary>
    /// Displays detailed information for a single application identified by its primary key ID.
    /// Returns 404 NotFound if the ID does not exist.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var application = await _context.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        return View(application);
    }

    // =========================================================================
    // 3. CREATE: Show Creation Form (GET: /Applications/Create)
    // =========================================================================
    /// <summary>
    /// Displays the empty registration form to add a new application.
    /// Pre-populates default values (e.g., Status = "Active").
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var application = new Application
        {
            Status = "Active"
        };
        return View(application);
    }

    // =========================================================================
    // 3. CREATE: Process Form Submission (POST: /Applications/Create)
    // =========================================================================
    /// <summary>
    /// Receives submitted application form data, validates it, and saves it to PostgreSQL.
    /// Protected by ValidateAntiForgeryToken to prevent Cross-Site Request Forgery (CSRF).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,Technology,Version,Status")] Application application)
    {
        // Server-side validation against data annotation rules on Application model
        if (ModelState.IsValid)
        {
            // Set UTC creation timestamp explicitly
            application.CreatedAt = DateTime.UtcNow;

            _context.Add(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Application '{application.Name}' was successfully registered.";
            _logger.LogInformation("Successfully created application: {Name} (ID: {Id})", application.Name, application.Id);

            return RedirectToAction(nameof(Index));
        }

        // If validation fails, return to form with entered values and validation error messages
        return View(application);
    }

    // =========================================================================
    // 4. UPDATE: Show Edit Form (GET: /Applications/Edit/5)
    // =========================================================================
    /// <summary>
    /// Displays the edit form populated with current application details.
    /// Returns 404 NotFound if ID is missing or record does not exist.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var application = await _context.Applications.FindAsync(id);
        if (application == null)
        {
            return NotFound();
        }

        return View(application);
    }

    // =========================================================================
    // 4. UPDATE: Process Edit Submission (POST: /Applications/Edit/5)
    // =========================================================================
    /// <summary>
    /// Updates an existing application in PostgreSQL.
    /// Prevents overwriting system-managed properties such as CreatedAt.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Technology,Version,Status")] Application application)
    {
        if (id != application.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Fetch existing entity from database to preserve system audit fields (CreatedAt)
                var existingApp = await _context.Applications.FindAsync(id);
                if (existingApp == null)
                {
                    return NotFound();
                }

                // Update user-editable properties only
                existingApp.Name = application.Name;
                existingApp.Description = application.Description;
                existingApp.Technology = application.Technology;
                existingApp.Version = application.Version;
                existingApp.Status = application.Status;
                existingApp.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Application '{existingApp.Name}' was updated successfully.";
                _logger.LogInformation("Successfully updated application ID: {Id}", id);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ApplicationExists(application.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        return View(application);
    }

    // =========================================================================
    // 5. DELETE: Show Confirmation Page (GET: /Applications/Delete/5)
    // =========================================================================
    /// <summary>
    /// Displays a confirmation page asking the user to verify deletion.
    /// Never performs deletion on a GET request to prevent accidental or malicious link clicks.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var application = await _context.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (application == null)
        {
            return NotFound();
        }

        return View(application);
    }

    // =========================================================================
    // 5. DELETE: Confirm and Execute Deletion (POST: /Applications/Delete/5)
    // =========================================================================
    /// <summary>
    /// Performs the actual deletion from PostgreSQL upon explicit POST confirmation.
    /// Protected by antiforgery token.
    /// </summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var application = await _context.Applications.FindAsync(id);
        if (application != null)
        {
            _context.Applications.Remove(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Application '{application.Name}' was successfully deleted.";
            _logger.LogInformation("Successfully deleted application ID: {Id}", id);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Helper method to verify if an application exists in the database.
    /// </summary>
    private async Task<bool> ApplicationExists(int id)
    {
        return await _context.Applications.AnyAsync(e => e.Id == id);
    }
}
