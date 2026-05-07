using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JustShortIt.Service;

namespace JustShortIt.Pages;

public class IndexModel : PageModel
{
    private SqliteUrlStore Db { get; set; }
    private readonly ILogger<IndexModel> _logger;

    // Bound property
    public string? Id { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates the landing page model that resolves incoming short IDs.
    /// </summary>
    /// <param name="db">Redirect store used to resolve short IDs to target URLs.</param>
    /// <param name="logger">Logger used to record redirect hits and misses.</param>
    public IndexModel(SqliteUrlStore db, ILogger<IndexModel> logger)
    {
        Db = db;
        _logger = logger;
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
        if (data is not null)
        {
            var clickedAtUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var referrer = string.IsNullOrWhiteSpace(Request.Headers.Referer) ? null : Request.Headers.Referer.ToString();

            try
            {
                await Db.LogRedirectClickAsync(Id, referrer, clickedAtUtc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log click event for redirect ID {RedirectId}; redirecting anyway.", Id);
            }

            _logger.LogInformation("Redirect hit for ID {RedirectId}.", Id);
            return Redirect(data);
        }

        _logger.LogWarning("Redirect miss for ID {RedirectId}.", Id);

        ErrorMessage = "Redirect ID not found, it may have been deleted or expired";

        return Page();
    }
}