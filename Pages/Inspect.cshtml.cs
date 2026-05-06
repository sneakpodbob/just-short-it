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

    public InspectModel(SqliteUrlStore db)
    {
        Db = db;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Id == null) return await OnGet(null, "Delete request without ID, aborted.");

        await Db.DeleteAsync(Id);

        return await OnGet(null, $"ID '{Id}' successfully deleted.");
    }

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