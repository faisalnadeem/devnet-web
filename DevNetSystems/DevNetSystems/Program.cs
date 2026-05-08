using Microsoft.AspNetCore.Rewrite;
using DevNetSystems.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddScoped<DevNetSystems.Services.IEmailService, DevNetSystems.Services.EmailService>();
builder.Services.Configure<SiteSettings>(builder.Configuration.GetSection("Site"));

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

// SEO normalization rules:
//   1. Strip trailing slash (except root): /about/ -> /about (301)
//   2. Lowercase the path: /About -> /about (301) so we don't have two URLs for the same page.
//      Skip query strings and asset paths to avoid breaking case-sensitive deployments.
var rewriteOptions = new RewriteOptions()
    .AddRedirect(@"^(.+)/$", "$1", (int)System.Net.HttpStatusCode.MovedPermanently);
app.UseRewriter(rewriteOptions);

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (!string.IsNullOrEmpty(path) && path.Length > 1)
    {
        var lower = path.ToLowerInvariant();
        if (!string.Equals(path, lower, StringComparison.Ordinal)
            && !path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/vendor/", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase))
        {
            var qs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
            context.Response.Redirect(lower + qs, permanent: true);
            return;
        }
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
