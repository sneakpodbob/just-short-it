using JustShortIt.Model;
using JustShortIt.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Web;

namespace JustShortIt.Pages;

[Authorize]
public class UrlsModel : PageModel
{
    [BindProperty]
    public UrlRedirect? Model { get; set; }
    [BindProperty(Name = "message")]
    public string? Message { get; set; }

    private string? BaseUrl { get; }
    private SqliteUrlStore Db { get; }

    public UrlsModel(IConfiguration configuration, IWebHostEnvironment env, SqliteUrlStore db)
    {
        if (!env.IsDevelopment())
        {
            var configuredBaseUrl = configuration.GetValue<string>("BaseUrl");
            BaseUrl = new Uri(configuredBaseUrl ?? throw new InvalidOperationException("BaseUrl not configured correctly."), UriKind.Absolute).ToString();
        }
        Db = db;
    }

    private string GetEffectiveBaseUrl() =>
        BaseUrl ?? $"{Request.Scheme}://{Request.Host}/";

    public async Task<IActionResult> OnPostInspectAsync()
    {
        string? id = Request.Form["Inspect_Id"];
        if (id is null || string.IsNullOrEmpty(id))
        {
            ModelState.AddModelError("Inspect_Id", "ID is a required field");
            return Page();
        }

        if (await Db.ExistsAsync(id)) return RedirectToPage("Inspect", new { Id = id });

        ModelState.AddModelError("Inspect_Id", "ID does not exist");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || Model is null) return Page();

        var id = HttpUtility.UrlEncode(Model.Id);

        if (!Uri.TryCreate($"{GetEffectiveBaseUrl()}{id}", UriKind.Absolute, out var link))
        {
            Message = "This ID cannot be used in a URL.";
            return Page();
        }

        if (!long.TryParse(Model.ExpirationDate, out var expirationDateBinary))
        {
            Message = "Expiration date is not valid.";
            return Page();
        }

        var expirationDate = DateTime.FromBinary(expirationDateBinary);
        if (!await Db.CreateAsync(id, Model.Target, expirationDate.ToUniversalTime()))
        {
            Message = "This ID is already taken.";
            return Page();
        }

        ModelState.Clear();
        var generateNewId = await Db.GenerateNewId();
        ModelState.SetModelValue(nameof(UrlRedirect.Id), generateNewId, generateNewId);

        Message = $"URL Generated! <a href='{link}'>{link}</a>. " +
                  $"<button class='button is-link is-small' onclick='navigator.clipboard.writeText(\"{link}\")'>Copy</button>";
        return await OnGet(Message);
    }

    public async Task<IActionResult> OnGet(string message)
    {
        Message = message;
        Model = new UrlRedirect(await Db.GenerateNewId(), string.Empty, string.Empty);
        return Page();
    }

}