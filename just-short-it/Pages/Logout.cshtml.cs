using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JustShortIt.Pages;

public class LogoutModel : PageModel
{
    /// <summary>
    /// Signs the current user out and returns to the landing page.
    /// </summary>
    /// <returns>A redirect to the index page after cookie sign-out.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("Index");
    }
}