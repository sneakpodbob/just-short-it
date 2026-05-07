using JustShortIt.Service;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JustShortIt.Migrations;

[DbContext(typeof(JustShortItDbContext))]
[Migration("20260507000200_AddBlockedRedirectIds")]
public partial class AddBlockedRedirectIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "blocked_redirect_ids",
            columns: table => new
            {
                id = table.Column<string>(type: "TEXT", nullable: false),
                expires_at_utc = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blocked_redirect_ids", x => x.id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "blocked_redirect_ids");
    }
}