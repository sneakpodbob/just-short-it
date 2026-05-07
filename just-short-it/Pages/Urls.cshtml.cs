using JustShortIt.Model.Dto;
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
    public string? GeneratedLink { get; set; }

    private string? BaseUrl { get; }
    private SqliteUrlStore Db { get; }
    private readonly ILogger<UrlsModel> _logger;

    /// <summary>
    /// Creates the page model and resolves the externally visible base URL used for generated links.
    /// </summary>
    /// <param name="configuration">Application configuration containing the production <c>BaseUrl</c> setting.</param>
    /// <param name="env">Host environment used to determine whether dynamic host-based URLs are allowed.</param>
    /// <param name="db">URL store used for ID generation and persistence.</param>
    /// <param name="logger">Logger used to record URL creation events and errors.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when running outside development and <c>BaseUrl</c> is missing or cannot be resolved as an absolute URI.
    /// </exception>
    public UrlsModel(IConfiguration configuration, IWebHostEnvironment env, SqliteUrlStore db, ILogger<UrlsModel> logger)
    {
        if (!env.IsDevelopment())
        {
            var configuredBaseUrl = configuration.GetValue<string>("BaseUrl");
            BaseUrl = new Uri(configuredBaseUrl ?? throw new InvalidOperationException("BaseUrl not configured correctly."), UriKind.Absolute).ToString();
        }
        Db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns the base URL prefix used to build user-facing short links.
    /// </summary>
    /// <remarks>
    /// In development this is inferred from the current request host and scheme.
    /// In non-development environments it is pinned to configured <c>BaseUrl</c> to avoid host-header ambiguity.
    /// </remarks>
    private string GetEffectiveBaseUrl() =>
        BaseUrl ?? $"{Request.Scheme}://{Request.Host}/";

    /// <summary>
    /// Ensures the bound form model exists with a candidate ID before rendering the page.
    /// </summary>
    /// <remarks>
    /// This preserves an existing user-entered ID when present and only allocates a new ID when the model is missing
    /// or the ID is blank.
    /// </remarks>
    private async Task EnsureFormModelInitializedAsync()
    {
        if (Model is not null && !string.IsNullOrWhiteSpace(Model.Id))
        {
            return;
        }

        Model = new UrlRedirect(await Db.GenerateNewId(), string.Empty, string.Empty);
    }

    /// <summary>
    /// Handles inspect requests by validating the submitted short ID and redirecting to the inspect page when found.
    /// </summary>
    /// <returns>
    /// A redirect to the inspect page for an existing ID; otherwise the current page with validation errors.
    /// </returns>
    public async Task<IActionResult> OnPostInspectAsync()
    {
        string? id = Request.Form["Inspect_Id"];
        if (id is null || string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("Inspect form submission rejected due to missing ID.");
            ModelState.AddModelError("Inspect_Id", "ID is a required field");
            await EnsureFormModelInitializedAsync();
            return Page();
        }

        if (await Db.ExistsAsync(id))
        {
            _logger.LogInformation("Inspect form redirected to details for ID {RedirectId}.", id);
            return RedirectToPage("Inspect", new { Id = id });
        }

        _logger.LogWarning("Inspect form lookup failed because ID {RedirectId} does not exist.", id);
        ModelState.AddModelError("Inspect_Id", "ID does not exist");
        await EnsureFormModelInitializedAsync();
        return Page();
    }

    /// <summary>
    /// Creates a new short URL mapping from the posted form values.
    /// </summary>
    /// <returns>
    /// The page with an error message when validation or persistence fails; otherwise the page with a success message
    /// containing the generated link.
    /// </returns>
    /// <remarks>
    /// The method validates that the encoded ID can form a valid absolute URL against the effective base URL,
    /// converts the posted expiration payload from <see cref="DateTime.ToBinary()"/> format, and stores expiration in UTC.
    /// </remarks>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid || Model is null) return Page();

        var id = HttpUtility.UrlEncode(Model.Id);

        if (!Uri.TryCreate($"{GetEffectiveBaseUrl()}{id}", UriKind.Absolute, out var link))
        {
            _logger.LogWarning("URL creation rejected because ID {RedirectId} cannot form an absolute URL.", id);
            Message = "This ID cannot be used in a URL.";
            return Page();
        }

        if (!long.TryParse(Model.ExpirationDate, out var expirationDateBinary))
        {
            _logger.LogWarning("URL creation rejected for ID {RedirectId} due to invalid expiration payload.", id);
            Message = "Expiration date is not valid.";
            return Page();
        }

        var expirationDate = DateTime.FromBinary(expirationDateBinary);
        if (!await Db.CreateAsync(id, Model.Target, expirationDate.ToUniversalTime()))
        {
            _logger.LogWarning("URL creation rejected because ID {RedirectId} is already taken.", id);
            Message = "This ID is already taken.";
            return Page();
        }

        ModelState.Clear();
        var generateNewId = await Db.GenerateNewId();
        ModelState.SetModelValue(nameof(UrlRedirect.Id), generateNewId, generateNewId);

        Message = "URL Generated!";
        GeneratedLink = link.ToString();
        _logger.LogInformation("Created short URL {RedirectId} for target {TargetUrl}.", id, Model.Target);
        await EnsureFormModelInitializedAsync();
        return Page();
    }

    /// <summary>
    /// Renders the creation page and optionally displays a status message from a prior action.
    /// </summary>
    /// <param name="message">User-facing status content to pre-populate on the page.</param>
    /// <returns>The page with an initialized form model.</returns>
    public async Task<IActionResult> OnGet(string message)
    {
        Message = message;
        await EnsureFormModelInitializedAsync();
        return Page();
    }

}