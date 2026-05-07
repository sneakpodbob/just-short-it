using JustShortIt.Model.Database;
using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Service;

public class JustShortItDbContext : DbContext
{
    /// <summary>
    /// Initializes the EF Core context used for short-link persistence.
    /// </summary>
    /// <param name="options">Configured options including provider, connection string, and runtime behaviors.</param>
    public JustShortItDbContext(DbContextOptions<JustShortItDbContext> options)
        : base(options)
    {
    }

    public DbSet<BlockedRedirectId> BlockedRedirectIds => Set<BlockedRedirectId>();
    public DbSet<RedirectClickEvent> RedirectClickEvents => Set<RedirectClickEvent>();
    public DbSet<StoredUrlRedirect> Redirects => Set<StoredUrlRedirect>();

    /// <summary>
    /// Configures the database schema mapping for redirect entities.
    /// </summary>
    /// <param name="modelBuilder">Model builder used to define table, key, and column mappings.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredUrlRedirect>(entity =>
        {
            entity.ToTable("redirects");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Id)
                .IsUnique();

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(x => x.Target)
                .HasColumnName("target")
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .HasColumnName("expires_at_utc")
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasDefaultValueSql("unixepoch('now')")
                .IsRequired();

            entity.HasMany(x => x.ClickEvents)
                .WithOne(x => x.Redirect)
                .HasForeignKey(x => x.RedirectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BlockedRedirectId>(entity =>
        {
            entity.ToTable("blocked_redirect_ids");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Id)
                .IsUnique();                

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .HasColumnName("expires_at_utc")
                .IsRequired();
        });

        modelBuilder.Entity<RedirectClickEvent>(entity =>
        {
            entity.ToTable("redirect_click_events");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.RedirectId)
                .HasColumnName("redirect_id")
                .IsRequired();

            entity.Property(x => x.ClickedAtUtc)
                .HasColumnName("clicked_at_utc")
                .IsRequired();

            entity.Property(x => x.Referrer)
                .HasColumnName("referrer");

            entity.HasIndex(x => new { x.RedirectId, x.ClickedAtUtc })
                .HasDatabaseName("IX_redirect_click_events_redirect_id_clicked_at_utc")
                .IsDescending(false, true);
        });
    }
}