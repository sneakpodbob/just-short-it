using JustShortIt.Model;
using JustShortIt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JustShortIt.Pages; 

[Authorize]
public class InspectModel : PageModel
{
    [BindProperty(Name = "id", SupportsGet = true)]
    public string? Id { get; set; } = string.Empty;
    [BindProperty(Name="message")]
    public string? Message { get; set; }

    public UrlRedirect? UrlRedirect { get; set; }
    
    private SqliteUrlStore Db { get; }

    /// <summary>
    /// Creates the inspect page model used to view and delete existing redirects.
    /// </summary>
    /// <param name="db">Redirect store used for lookup and deletion.</param>
    public InspectModel(SqliteUrlStore db)
    {
        Db = db;
    }

    /// <summary>
    /// Handles delete requests for the current ID.
    /// </summary>
    /// <returns>
    /// The inspect page with a status message. If no ID is provided, returns a message indicating the delete was aborted.
    /// </returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (Id == null) return await OnGet(null, "Delete request without ID, aborted.");

        await Db.DeleteAsync(Id);

        return await OnGet(null, $"ID '{Id}' successfully deleted.");
    }

    /// <summary>
    /// Renders the inspect view and loads redirect details when an ID is provided.
    /// </summary>
    /// <param name="id">Redirect ID to inspect.</param>
    /// <param name="message">Optional status message to show in the UI.</param>
    /// <returns>
    /// Redirects to the management page when called without both ID and message; otherwise returns the inspect page.
    /// </returns>
    public async Task<IActionResult> OnGet(string? id, string? message)
    {
        if (id is null && message is null) return RedirectToPage("Urls");
        
        Id = id;
        Message = message;

        if (Id is null) return Page();

        var url = await Db.GetTargetAsync(Id);
        if (url is not null) UrlRedirect = new UrlRedirect(Id, url, string.Empty);

        return Page();
    }
}