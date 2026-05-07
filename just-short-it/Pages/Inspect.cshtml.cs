using JustShortIt.Model.Dto;
using Humanizer;
using JustShortIt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JustShortIt.Pages; 

[Authorize]
public class InspectModel : PageModel
{
    private const int InspectClickHistoryRows = 500;

    [BindProperty(Name = "id", SupportsGet = true)]
    public string? Id { get; set; } = string.Empty;
    [BindProperty(Name="message")]
    public string? Message { get; set; }
    [BindProperty(Name = "returnTo", SupportsGet = true)]
    public string? ReturnTo { get; set; }

    public UrlRedirect? UrlRedirect { get; set; }
    public RedirectInspectDetails? RedirectDetails { get; set; }
    public IReadOnlyList<ClickEventItem> ClickEvents { get; set; } = [];
    
    private SqliteUrlStore Db { get; }
    private readonly ILogger<InspectModel> _logger;

    /// <summary>
    /// Creates the inspect page model used to view and delete existing redirects.
    /// </summary>
    /// <param name="db">Redirect store used for lookup and deletion.</param>
    /// <param name="logger">Logger used to record inspect lookups and delete operations.</param>
    public InspectModel(SqliteUrlStore db, ILogger<InspectModel> logger)
    {
        Db = db;
        _logger = logger;
    }

    /// <summary>
    /// Handles delete requests for the current ID.
    /// </summary>
    /// <returns>
    /// The inspect page with a status message. If no ID is provided, returns a message indicating the delete was aborted.
    /// </returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (Id == null)
        {
            _logger.LogWarning("Delete request aborted because no redirect ID was provided.");
            return await OnGet(null, "Delete request without ID, aborted.");
        }

        await Db.DeleteAsync(Id);
        _logger.LogInformation("Inspect delete request completed for ID {RedirectId}.", Id);

        return await OnGet(null, $"ID '{Id}' successfully deleted.", ReturnTo);
    }

    /// <summary>
    /// Renders the inspect view and loads redirect details when an ID is provided.
    /// </summary>
    /// <param name="id">Redirect ID to inspect.</param>
    /// <param name="message">Optional status message to show in the UI.</param>
    /// <param name="returnTo">Where to return when back button is pressed.</param>
    /// <returns>
    /// Redirects to the management page when called without both ID and message; otherwise returns the inspect page.
    /// </returns>
    public async Task<IActionResult> OnGet(string? id, string? message, string? returnTo = null)
    {
        if (id is null && message is null) return RedirectToPage("Urls");
        
        Id = id;
        Message = message;
        ReturnTo = returnTo;

        if (Id is null) return Page();

        var redirect = await Db.GetRedirectAsync(Id);
        if (redirect is not null)
        {
            UrlRedirect = new UrlRedirect(Id, redirect.Target, string.Empty);

            var clickCount = await Db.GetClickCountAsync(Id);
            var clickEvents = await Db.GetClickEventsAsync(Id, InspectClickHistoryRows);
            var nowUtc = DateTimeOffset.UtcNow;
            var ageSeconds = nowUtc.ToUnixTimeSeconds() - redirect.CreatedAtUtc;
            var ageDays = Math.Max(ageSeconds / 86400d, 1d);
            var clicksPerDay = clickCount / ageDays;
            var expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(redirect.ExpiresAtUtc);
            var validFor = GetHumanReadableValidFor(expiresAtUtc, nowUtc);

            RedirectDetails = new RedirectInspectDetails(
                DateTimeOffset.FromUnixTimeSeconds(redirect.CreatedAtUtc).UtcDateTime,
                expiresAtUtc.UtcDateTime,
                validFor,
                clickCount,
                clicksPerDay);

            ClickEvents = clickEvents
                .Select(x => new ClickEventItem(
                    DateTimeOffset.FromUnixTimeSeconds(x.ClickedAtUtc).UtcDateTime,
                    string.IsNullOrWhiteSpace(x.Referrer) ? null : x.Referrer))
                .ToList();

            _logger.LogInformation("Inspect lookup found redirect ID {RedirectId}.", Id);
        }
        else
        {
            _logger.LogWarning("Inspect lookup could not find redirect ID {RedirectId}.", Id);
        }

        return Page();
    }

    private static string GetHumanReadableValidFor(DateTimeOffset expiresAtUtc, DateTimeOffset nowUtc)
    {
        if (expiresAtUtc > nowUtc.AddYears(100))
        {
            return "Never";
        }

        if (expiresAtUtc <= nowUtc)
        {
            return "Expired";
        }

        return (expiresAtUtc - nowUtc).Humanize(precision: 2);
    }

    public record RedirectInspectDetails(
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        string ValidFor,
        long ClickCount,
        double ClicksPerDay);

    public record ClickEventItem(DateTime ClickedAtUtc, string? Referrer);
}