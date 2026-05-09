using DevNetSystems.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddScoped<DevNetSystems.Services.IEmailService, DevNetSystems.Services.EmailService>();
builder.Services.Configure<SiteSettings>(builder.Configuration.GetSection("Site"));

// Generate lowercase URLs from every link helper (asp-page, asp-controller, etc.) so internal
// links never hit the lowercase 301 redirect — eliminates the "Page with redirect" Search Console
// flag for internal navigation and removes a pile of soft-404 / redirect chains.
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = false; // query values are case-sensitive (e.g. tracking codes)
    options.AppendTrailingSlash = false;
});

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
    options.HttpsPort = 443;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Re-execute /Error/{statusCode} so 404 and 500 can be differentiated by Error.cshtml.
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Single-pass URL canonicalization — consolidates all normalization into ONE 301 hop so
// Search Console never sees multi-hop redirect chains (it de-prioritises those as "Page with
// redirect"). Rules applied in priority order:
//   - /Index, /Home, /Default (and .html/.htm/.aspx/.php variants) → "/"
//   - Strip legacy file extensions: /about.html → /about
//   - Strip trailing slash (except root)
//   - Lowercase the path
// Static asset paths (/lib, /vendor, /img, /css, /js) are skipped entirely.
var legacyDocRegex = new System.Text.RegularExpressions.Regex(
    @"^/(index|home|default)(\.(html?|aspx?|php))?$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
var extensionRegex = new System.Text.RegularExpressions.Regex(
    @"^(.+)\.(html?|aspx?|php)$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (string.IsNullOrEmpty(path) || path.Length <= 1)
    {
        await next();
        return;
    }

    if (path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/vendor/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    string normalized;

    if (legacyDocRegex.IsMatch(path))
    {
        normalized = "/";
    }
    else
    {
        normalized = path;
        // Strip trailing slash before extension so /About.html/ -> /about in one hop.
        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized[..^1];
        }
        var extMatch = extensionRegex.Match(normalized);
        if (extMatch.Success)
        {
            normalized = extMatch.Groups[1].Value;
        }
        normalized = normalized.ToLowerInvariant();
    }

    if (!string.Equals(path, normalized, StringComparison.Ordinal))
    {
        var qs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        context.Response.Redirect(normalized + qs, permanent: true);
        return;
    }

    await next();
});

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["X-XSS-Protection"] = "1; mode=block";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var fileName = ctx.File.Name.ToLowerInvariant();
        var headers = ctx.Context.Response.GetTypedHeaders();
        var query = ctx.Context.Request.Query;

        // Fingerprinted assets (requested with ?v=<hash> via asp-append-version) are content-addressed,
        // so they're safe to cache for a year as immutable. PageSpeed wants >= 1y for static assets,
        // and "immutable" stops the browser from sending revalidation requests.
        var hasVersionQuery = query.ContainsKey("v");

        var isStaticAsset = fileName.EndsWith(".css") || fileName.EndsWith(".js")
            || fileName.EndsWith(".woff") || fileName.EndsWith(".woff2") || fileName.EndsWith(".ttf") || fileName.EndsWith(".otf") || fileName.EndsWith(".eot")
            || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".png")
            || fileName.EndsWith(".webp") || fileName.EndsWith(".svg") || fileName.EndsWith(".ico")
            || fileName.EndsWith(".gif") || fileName.EndsWith(".avif");

        if (isStaticAsset && hasVersionQuery)
        {
            headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(365),
                Extensions = { new Microsoft.Net.Http.Headers.NameValueHeaderValue("immutable") }
            };
        }
        else if (isStaticAsset)
        {
            // Unversioned (e.g. images referenced directly from CSS without ?v). 30 days is the
            // Lighthouse minimum that still scores green on the cache-policy audit.
            headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(30)
            };
        }
        else
        {
            headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromHours(1)
            };
        }
    }
});

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
