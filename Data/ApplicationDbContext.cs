using Microsoft.EntityFrameworkCore;
using EALRMS.Models;

namespace EALRMS.Data;

/// <summary>
/// The ApplicationDbContext serves as the bridge between our C# domain models
/// and the PostgreSQL database. It handles database connections, querying,
/// change tracking, and persisting changes.
///
/// In ASP.NET Core, DbContext is registered in the Dependency Injection (DI)
/// container and injected into Controllers with a Scoped lifetime (one instance per HTTP request).
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the ApplicationDbContext class.
    /// The options parameter contains database connection string and provider configuration,
    /// which are configured in Program.cs.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Represents the collection of all Application entities in the context.
    /// LINQ queries executed against this DbSet are translated by the Npgsql provider
    /// into native PostgreSQL SQL queries.
    /// </summary>
    public DbSet<Application> Applications => Set<Application>();

    /// <summary>
    /// Configures the model relationships, database tables, column constraints,
    /// and indexes using the EF Core Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Application>(entity =>
        {
            // Explicitly set the PostgreSQL table name
            entity.ToTable("Applications");

            // Define the primary key
            entity.HasKey(a => a.Id);

            // Configure column constraints
            entity.Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.Description)
                .HasMaxLength(500);

            entity.Property(a => a.Technology)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.Version)
                .HasMaxLength(30);

            entity.Property(a => a.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            // Timestamps: PostgreSQL timestamp with time zone (timestamptz)
            entity.Property(a => a.CreatedAt)
                .IsRequired();

            entity.Property(a => a.UpdatedAt)
                .IsRequired(false);

            // Create indexes for optimized searching and filtering
            entity.HasIndex(a => a.Name);
            entity.HasIndex(a => a.Status);
        });
    }

    /// <summary>
    /// Overrides SaveChangesAsync to automatically manage audit timestamps (CreatedAt and UpdatedAt).
    /// This ensures consistent UTC timestamps without relying on manual controller assignments.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // ChangeTracker inspects entity states (Added, Modified, Unchanged, Deleted)
        var entries = ChangeTracker.Entries<Application>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Ensure CreatedAt is populated and explicitly marked as UTC for PostgreSQL timestamptz
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    entry.Entity.CreatedAt = DateTime.SpecifyKind(entry.Entity.CreatedAt, DateTimeKind.Utc);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Prevent accidental modification of the original creation date
                entry.Property(a => a.CreatedAt).IsModified = false;

                // Stamp the modification time with current UTC time
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
