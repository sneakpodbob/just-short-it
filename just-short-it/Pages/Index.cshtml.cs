using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JustShortIt.Service;

namespace JustShortIt.Pages;

public class IndexModel : PageModel
{
    private SqliteUrlStore Db { get; set; }

    // Bound property
    public string? Id { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates the landing page model that resolves incoming short IDs.
    /// </summary>
    /// <param name="db">Redirect store used to resolve short IDs to target URLs.</param>
    public IndexModel(SqliteUrlStore db)
    {
        Db = db;
    }

    /// <summary>
    /// Handles landing-page requests and redirects when a valid short ID is provided.
    /// </summary>
    /// <param name="id">Optional short ID from the route or query string.</param>
    /// <returns>
    /// A redirect to the destination URL when found; otherwise the page with an error message when an ID is unknown.
    /// </returns>
    public async Task<IActionResult> OnGetAsync(string? id)
    {
        Id = id;

        if (Id is null) return Page();

        var data = await Db.GetTargetAsync(Id);
        if (data is not null) return Redirect(data);

        ErrorMessage = "Redirect ID not found, it may have been deleted or expired";

        return Page();
    }
}