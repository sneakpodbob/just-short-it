using JustShortIt.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace JustShortIt.Migrations;

[DbContext(typeof(JustShortItDbContext))]
public class JustShortItDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.7");

        modelBuilder.Entity("JustShortIt.Model.BlockedRedirectId", b =>
            {
                b.Property<string>("Id")
                    .ValueGeneratedNever()
                    .HasColumnType("TEXT")
                    .HasColumnName("id");

                b.Property<long>("ExpiresAtUtc")
                    .HasColumnType("INTEGER")
                    .HasColumnName("expires_at_utc");

                b.HasKey("Id");

                b.ToTable("blocked_redirect_ids");
            });

        modelBuilder.Entity("JustShortIt.Model.StoredUrlRedirect", b =>
            {
                b.Property<string>("Id")
                    .ValueGeneratedNever()
                    .HasColumnType("TEXT")
                    .HasColumnName("id");

                b.Property<long>("ExpiresAtUtc")
                    .HasColumnType("INTEGER")
                    .HasColumnName("expires_at_utc");

                b.Property<string>("Target")
                    .IsRequired()
                    .HasColumnType("TEXT")
                    .HasColumnName("target");

                b.HasKey("Id");

                b.ToTable("redirects");
            });
#pragma warning restore 612, 618
    }
}