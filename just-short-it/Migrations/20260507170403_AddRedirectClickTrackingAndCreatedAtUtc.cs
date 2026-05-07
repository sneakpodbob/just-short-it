using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustShortIt.Migrations
{
    /// <inheritdoc />
    public partial class AddRedirectClickTrackingAndCreatedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "created_at_utc",
                table: "redirects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE redirects SET created_at_utc = unixepoch('now') WHERE created_at_utc = 0;");

            migrationBuilder.CreateTable(
                name: "redirect_click_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    redirect_id = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    clicked_at_utc = table.Column<long>(type: "INTEGER", nullable: false),
                    referrer = table.Column<string>(type: "TEXT", maxLength: 3072, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redirect_click_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_redirect_click_events_redirects_redirect_id",
                        column: x => x.redirect_id,
                        principalTable: "redirects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_redirect_click_events_redirect_id_clicked_at_utc",
                table: "redirect_click_events",
                columns: new[] { "redirect_id", "clicked_at_utc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redirect_click_events");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "redirects");
        }
    }
}
