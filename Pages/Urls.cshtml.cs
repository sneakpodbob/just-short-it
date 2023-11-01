using JustShortIt.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Caching.Distributed;

namespace JustShortIt.Pages;

[Authorize]
public partial class UrlsModel : PageModel
{
    [BindProperty]
    public UrlRedirect? Model { get; set; }
    [BindProperty(Name = "message")]
    public string? Message { get; set; }

    [GeneratedRegex("[/+=]")]
    private static partial Regex RegExGuid();

    private string BaseUrl { get; }
    private IDistributedCache Db { get; }

    public UrlsModel(IConfiguration configuration, IDistributedCache db)
    {
#if DEBUG
        BaseUrl = "https://localhost/";
#else 
        BaseUrl = new Uri(BaseUrl, UriKind.Absolute).ToString();
#endif
        Db = db;
    }

    public async Task<IActionResult> OnPostInspectAsync()
    {
        string? id = Request.Form["Inspect_Id"];
        if (id is null || string.IsNullOrEmpty(id))
        {
            ModelState.AddModelError("Inspect_Id", "ID is a required field");
            return Page();
        }

        if (await Db.GetAsync(id) is not null) return RedirectToPage("Inspect", new { Id = id });

        ModelState.AddModelError("Inspect_Id", "ID does not exist");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        if (Model is null) return Page();

        var id = HttpUtility.UrlEncode(Model.Id);

        if (await Db.GetAsync(id) is not null)
        {
            Message = "This ID is already taken.";
            return Page();
        }

        if (Uri.TryCreate($"{BaseUrl}{id}", UriKind.Absolute, out var link) is false)
        {
            Message = "This ID cannot be used in a URL.";
            return Page();
        }

        await Db.SetStringAsync(id, Model.Target, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTime.FromBinary(long.Parse(Model.ExpirationDate))
        });

        ModelState.Clear();
        var generateNewId = await GenerateNewId();
        ModelState.SetModelValue(nameof(UrlRedirect.Id), generateNewId, generateNewId);

        Message = $"URL Generated! <a href='{link}'>{link}</a>. " +
                  $"<button class='button is-link is-small' onclick='navigator.clipboard.writeText(\"{link}\")'>Copy</button>";
        return await OnGet(Message);
    }

    public async Task<IActionResult> OnGet(string message)
    {
        Message = message;
        Model = new UrlRedirect(await GenerateNewId(), string.Empty, string.Empty);
        return Page();
    }

    private async Task<string> GenerateNewId()
    {
        while (true)
        {
            var base64Guid = RegExGuid().Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "");

            var newId = string.Empty;
            // loop from 1 to the length of base64Guid

            foreach (var t in base64Guid[..16])
            {
                newId += t;

                if (await Db.GetAsync(newId) is null) return newId;
            }
        }
    }
}