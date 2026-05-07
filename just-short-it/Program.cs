using JustShortIt.Model;
using JustShortIt.Model.Dto;
using JustShortIt.Service;
using JustShortIt.Service.HealthChecks;
using Coravel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using System.Security.Cryptography;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddEnvironmentVariables("JSI_");
    builder.Host.UseSerilog();
    builder.Services.AddRazorPages();

    // CORS — no cross-origin access is needed for this app.
    // In development allow localhost; in production deny all cross-origin requests.
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.WithOrigins("http://localhost:5128", "https://localhost:7128")
                    .AllowCredentials()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }
            // Production: no allowed origins → CORS headers are never emitted → browsers block cross-origin requests
        });
    });

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

    string? baseUrl;
    User? user;

    if (builder.Environment.IsDevelopment())
    {
        baseUrl = null; // Derived dynamically from each request at runtime
        user = new User("test", "test");
        Log.Warning("YOU ARE RUNNING A DEVELOPMENT BUILD WITH TEST CREDENTIALS, DO NOT RUN THIS IN PRODUCTION.");
    }
    else
    {
        baseUrl = builder.Configuration.GetValue<string>("BaseUrl");
        user = builder.Configuration.GetSection("Account").Get<User>();
    }

    // Check if everything is configured (right)
    if (!builder.Environment.IsDevelopment() &&
        (string.IsNullOrEmpty(baseUrl) || !Uri.IsWellFormedUriString(baseUrl, UriKind.Absolute)))
    {
        Log.Fatal("Startup validation failed: BaseUrl is missing or invalid in non-development environment.");
        throw new ApplicationException(
            "Base-URL is not set to a correct URL, please provide JSI_BaseUrl with a valid url.");
    }

    if (user is null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
    {
        Log.Fatal("Startup validation failed: account credentials are not configured.");
        throw new ApplicationException(
            "Credentials not set, please provide JSI_Account__Username and JSI_Account__Password.");
    }

    // Set up SQLite persistence
    Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? AppContext.BaseDirectory);
    builder.Services.AddDbContext<JustShortItDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
    builder.Services.AddSingleton(sqlite);
    builder.Services.AddSingleton<IReservedIdProvider, ReservedIdProvider>();
    builder.Services.AddScoped<SqliteUrlStore>();
    builder.Services.AddScoped<SqliteMaintenanceRepository>();
    builder.Services.AddScheduler();
    builder.Services.AddTransient<ExpiredRedirectCleanupInvocable>();
    builder.Services.AddTransient<SqliteMaintenanceInvocable>();
    Log.Information("Running with SQLite persistence at {DatabasePath}.", databasePath);

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

    builder.Services.AddHealthChecks()
        .AddCheck<SqliteHealthCheck>("sqlite", tags: ["ready"])
        .AddCheck<DiskSpaceHealthCheck>("disk-space", tags: ["live", "ready"]);

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto |
                                   ForwardedHeaders.XForwardedHost;
        options.ForwardLimit = 1;

        // Trust one reverse-proxy hop (NGINX) in containerized setups where proxy
        // source addresses are not static/known ahead of time.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    Log.Information("Application startup complete. Environment: {EnvironmentName}", app.Environment.EnvironmentName);

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var addresses = app.Urls.Any() ? string.Join(", ", app.Urls) : "(none)";
        Log.Information("HTTP server is listening on: {Addresses}", addresses);
    });

    app.Services.UseScheduler(scheduler =>
    {
        scheduler.Schedule<ExpiredRedirectCleanupInvocable>().Hourly();
        scheduler.Schedule<SqliteMaintenanceInvocable>().Weekly();
        Log.Information("Scheduled maintenance jobs configured: expired cleanup hourly, SQLite vacuum weekly.");
    });

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<JustShortItDbContext>();
        await DatabaseInitializer.InitializeAsync(dbContext);
    }

    // Configure Cookies (used in Authentication)
    app.UseCookiePolicy(new CookiePolicyOptions
    {
        MinimumSameSitePolicy = SameSiteMode.Strict,
        Secure = securePolicy
    });

    // Security headers
    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items["CspNonce"] = nonce;

        // Prevent clickjacking (legacy; CSP frame-ancestors covers modern browsers)
        headers.XFrameOptions = "DENY";
        // Prevent MIME-type sniffing
        headers.XContentTypeOptions = "nosniff";
        // Restrict referrer information
        headers["Referrer-Policy"] = "no-referrer";
        // Disable features the app never uses
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
        // Cross-origin isolation
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";

        // Content Security Policy
        // Scripts are served from 'self'; page-level inline scripts must present the per-request nonce.
        var csp = string.Join("; ",
            "default-src 'self'",
            $"script-src 'self' 'nonce-{nonce}'",
            "form-action 'self'",
            "frame-ancestors 'none'",
            "object-src 'none'",
            "base-uri 'self'");

        if (!app.Environment.IsDevelopment())
        {
            // Require HTTPS for all sub-resources in production
            csp += "; upgrade-insecure-requests";
            // HSTS — nginx terminates TLS, but this header is forwarded to the browser
            headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
        }

        headers.ContentSecurityPolicy = csp;

        await next();
    });

    app.UseCors();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");

    app.UseForwardedHeaders();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapRazorPages();

    // Liveness probe — only checks that the process is alive and has disk space.
    // Safe to use as a Kubernetes livenessProbe; never causes a restart due to
    // a transient database issue.
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    // Readiness probe — checks all dependencies needed to serve traffic.
    // Use as a Kubernetes readinessProbe so the pod leaves the load-balancer
    // rotation until the database is available.
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponse,
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup or execution.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

return;

static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        duration_ms = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration_ms = e.Value.Duration.TotalMilliseconds,
            data = e.Value.Data,
            exception = e.Value.Exception?.Message
        })
    };

    return context.Response.WriteAsync(
        JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }));
}

