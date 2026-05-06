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

    public IndexModel(SqliteUrlStore db)
    {
        Db = db;
    }

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