using JustShortIt.Model;
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

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(x => x.Target)
                .HasColumnName("target")
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .HasColumnName("expires_at_utc")
                .IsRequired();
        });

        modelBuilder.Entity<BlockedRedirectId>(entity =>
        {
            entity.ToTable("blocked_redirect_ids");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .HasColumnName("expires_at_utc")
                .IsRequired();
        });
    }
}