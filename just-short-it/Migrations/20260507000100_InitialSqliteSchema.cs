using JustShortIt.Service;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JustShortIt.Migrations;

[DbContext(typeof(JustShortItDbContext))]
[Migration("20260507000100_InitialSqliteSchema")]
public partial class InitialSqliteSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "redirects",
            columns: table => new
            {
                id = table.Column<string>(type: "TEXT", nullable: false),
                target = table.Column<string>(type: "TEXT", nullable: false),
                expires_at_utc = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_redirects", x => x.id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "redirects");
    }
}