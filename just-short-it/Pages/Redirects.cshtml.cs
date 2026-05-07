using JustShortIt.Model.Database;
using JustShortIt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JustShortIt.Pages;

[Authorize]
public class RedirectsModel : PageModel
{
    public IReadOnlyList<RedirectListItem> Redirects { get; private set; } = [];

    private string? BaseUrl { get; }
    private SqliteUrlStore Db { get; }
    private readonly ILogger<RedirectsModel> _logger;

    public RedirectsModel(IConfiguration configuration, IWebHostEnvironment env, SqliteUrlStore db, ILogger<RedirectsModel> logger)
    {
        if (!env.IsDevelopment())
        {
            var configuredBaseUrl = configuration.GetValue<string>("BaseUrl");
            BaseUrl = new Uri(configuredBaseUrl ?? throw new InvalidOperationException("BaseUrl not configured correctly."), UriKind.Absolute).ToString();
        }

        Db = db;
        _logger = logger;
    }

    private string GetEffectiveBaseUrl() =>
        BaseUrl ?? $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";

    private string BuildRedirectLink(string id) =>
        new Uri(new Uri(GetEffectiveBaseUrl(), UriKind.Absolute), id).ToString();

    public async Task OnGetAsync()
    {
        var redirects = await Db.GetAllAsync();
        var clickCounts = await Db.GetClickCountsForActiveRedirectsAsync();

        Redirects = redirects
            .Select(x => new RedirectListItem(x, clickCounts.GetValueOrDefault(x.Id, 0), BuildRedirectLink(x.Id)))
            .ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await Db.DeleteAsync(id);
        _logger.LogInformation("Redirect {RedirectId} deleted from redirects management page.", id);
        return RedirectToPage();
    }

    public record RedirectListItem(StoredUrlRedirect Redirect, long ClickCount, string RedirectLink);
}
