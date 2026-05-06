using JustShortIt.Model;
using Microsoft.EntityFrameworkCore;

namespace JustShortIt.Service;

public class JustShortItDbContext : DbContext
{
    public JustShortItDbContext(DbContextOptions<JustShortItDbContext> options)
        : base(options)
    {
    }

    public DbSet<StoredUrlRedirect> Redirects => Set<StoredUrlRedirect>();

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
    }
}