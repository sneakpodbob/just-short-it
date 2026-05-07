using JustShortIt.Model.Database;
using JustShortIt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JustShortIt.Pages;

[Authorize]
public class RedirectsModel : PageModel
{
    public IReadOnlyList<StoredUrlRedirect> Redirects { get; private set; } = [];

    private SqliteUrlStore Db { get; }
    private readonly ILogger<RedirectsModel> _logger;

    public RedirectsModel(SqliteUrlStore db, ILogger<RedirectsModel> logger)
    {
        Db = db;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        Redirects = await Db.GetAllAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await Db.DeleteAsync(id);
        _logger.LogInformation("Redirect {RedirectId} deleted from redirects management page.", id);
        return RedirectToPage();
    }
}
