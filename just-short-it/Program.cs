using JustShortIt.Model;
using JustShortIt.Service;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables("JSI_");
builder.Services.AddRazorPages();

// Get Configurations
var sqlite = builder.Configuration.GetSection("Sqlite").Get<SqliteOptions>() ?? new SqliteOptions();
var securePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

var databasePath = string.IsNullOrWhiteSpace(sqlite.Path) ? "data/justshortit.db" : sqlite.Path;
if (!Path.IsPathRooted(databasePath))
{
    databasePath = Path.GetFullPath(databasePath, AppContext.BaseDirectory);
}

#if DEBUG
const string baseUrl = "http://localhost/";
var user = new User("test", "test");
Console.Error.WriteLine("YOU ARE RUNNING A DEBUG BUILD WITH TEST CREDENTIALS, " +
                        "DO NOT UNDER ANY CIRCUMSTANCES RUN THIS IN PRODUCTION, YOU HAVE BEEN WARNED.");
#else
var user = builder.Configuration.GetSection("Account").Get<User>();
var baseUrl = builder.Configuration.GetValue<string>("BaseUrl");
#endif

// Check if everything is configured (right)
if (string.IsNullOrEmpty(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute))
{
    throw new ApplicationException(
        "Base-URL is not set to a correct URL, please provide JSI_BaseUrl with a valid url.");
}

if (user is null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
{
    throw new ApplicationException(
        "Credentials not set, please provide JSI_Account__Username and JSI_Account__Password.");
}

// Set up SQLite persistence
Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? AppContext.BaseDirectory);
builder.Services.AddDbContext<JustShortItDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<SqliteUrlStore>();
Console.WriteLine($"Running with SQLite persistence at '{databasePath}'.");

// Add Authentication
builder.Services.AddSingleton(_ => new AuthenticationService(user));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/Login";
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.Cookie.SecurePolicy = securePolicy;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<JustShortItDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// Configure Cookies (used in Authentication)
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Strict,
    Secure = securePolicy
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
