using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustShortIt.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_redirects_id",
                table: "redirects",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_blocked_redirect_ids_id",
                table: "blocked_redirect_ids",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_redirects_id",
                table: "redirects");

            migrationBuilder.DropIndex(
                name: "IX_blocked_redirect_ids_id",
                table: "blocked_redirect_ids");
        }
    }
}
