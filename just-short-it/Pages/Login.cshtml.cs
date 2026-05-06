using System.Security.Claims;
using JustShortIt.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AuthenticationService = JustShortIt.Service.AuthenticationService;

namespace JustShortIt.Pages; 

public class LoginModel : PageModel
{
    [BindProperty]
    public User? UserModel { get; set; }

    private AuthenticationService Authentication { get; }
    private ILogger<LoginModel> Logger { get; }

    /// <summary>
    /// Creates the login page model.
    /// </summary>
    /// <param name="authentication">Credential validator for the configured application user.</param>
    /// <param name="logger">Logger used to record login attempts and outcomes.</param>
    public LoginModel(AuthenticationService authentication, ILogger<LoginModel> logger)
    {
        Authentication = authentication;
        Logger = logger;
    }

    /// <summary>
    /// Processes credential submission and signs the user in with a cookie when valid.
    /// </summary>
    /// <returns>
    /// Redirects to URL management after successful authentication; otherwise returns the login page with validation errors.
    /// </returns>
    /// <remarks>
    /// Successful login issues a persistent authentication cookie with a one-day expiration.
    /// </remarks>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Logger.LogWarning("Login attempt rejected due to invalid model state.");
            return Page();
        }

        if (Authentication.IsUser(UserModel!.Username, UserModel!.Password))
        {

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, UserModel.Username),
                new(ClaimTypes.Role, "Administrator")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var properties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1),
                IssuedUtc = DateTimeOffset.UtcNow,
                RedirectUri = "/"
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity), 
                properties);

            Logger.LogInformation("User {Username} logged in successfully.", UserModel.Username);
            return RedirectToPage("Urls");
        }

        Logger.LogWarning("Failed login attempt for username {Username}.", UserModel!.Username);
        ModelState.AddModelError(string.Empty, "Invalid Username or Password");
        return Page();
    }

    /// <summary>
    /// Renders the login page.
    /// </summary>
    /// <returns>The login page.</returns>
    public IActionResult OnGet() => Page();
}