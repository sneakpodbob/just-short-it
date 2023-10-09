using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;

namespace JustShortIt.Pages;

public class IndexModel : PageModel
{
    private IDistributedCache Db { get; set; }

    // Bound property
    public string? Id { get; set; }
    public string? ErrorMessage { get; set; }

    public IndexModel(IDistributedCache distributedCache)
    {
        Db = distributedCache;
    }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        Id = id;

        if (Id is null) return Page();

        var data = await Db.GetStringAsync(Id);
        if (data is not null) return Redirect(data);

        ErrorMessage = "Redirect ID not found, it may have been deleted or expired";

        return Page();
    }
}