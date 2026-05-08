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
//   - bare-domain → www.devnetsystems.com (fixes the "Page with redirect" flag for the apex)
//   - /Index, /Home, /Default (and .html/.htm/.aspx/.php variants) → "/"
//   - Strip legacy file extensions: /about.html → /about
//   - Strip trailing slash (except root)
//   - Lowercase the path
//   - Strip non-allowlisted query parameters (kills the WordPress-shape junk like
//     ?webteck_header=, ?p=1, ?feed=comments-rss2 that Search Console listed as
//     "Crawled but not indexed" / "Duplicate without canonical" / "Excluded by noindex")
// Static asset paths (/lib, /vendor, /img, /css, /js) are skipped entirely.
var legacyDocRegex = new System.Text.RegularExpressions.Regex(
    @"^/(index|home|default)(\.(html?|aspx?|php))?$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
var extensionRegex = new System.Text.RegularExpressions.Regex(
    @"^(.+)\.(html?|aspx?|php)$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

// Tracking + attribution params that legitimately need to ride through. Anything outside this
// set is stripped via 301 — that's how we collapse the WordPress-shape spam URLs Search Console
// was flagging. UTM and click-IDs stay because GA4 reads them client-side from location.search.
var allowedQueryParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "utm_source", "utm_medium", "utm_campaign", "utm_content", "utm_term", "utm_id",
    "gclid", "gbraid", "wbraid", "dclid",   // Google Ads
    "fbclid",                                // Facebook
    "msclkid",                               // Microsoft Ads
    "ttclid",                                // TikTok
    "twclid",                                // X / Twitter
    "li_fat_id",                             // LinkedIn
    "epik",                                  // Pinterest
    "_ga", "_gl",                            // Google Analytics cross-domain
    "ref"                                    // generic referrer marker we may use ourselves
};

// Hosts we should leave alone — anything inside this set is treated as already-canonical.
// Any other host that isn't www.devnetsystems.com gets 301'd to www.devnetsystems.com so
// the apex (devnetsystems.com) and any IP-style or alternate hostnames consolidate to one.
const string CanonicalHost = "www.devnetsystems.com";
var hostsToLeaveAlone = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "localhost",
    "127.0.0.1",
    "::1"
};

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "/";

    // 1) Asset paths: bypass everything to keep the cache-busted ?v= query intact.
    if (path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/vendor/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    // 2) Compute the canonical path.
    string normalizedPath;
    if (path.Length <= 1)
    {
        normalizedPath = "/";
    }
    else if (legacyDocRegex.IsMatch(path))
    {
        normalizedPath = "/";
    }
    else
    {
        normalizedPath = path;
        if (normalizedPath.Length > 1 && normalizedPath.EndsWith('/'))
        {
            normalizedPath = normalizedPath[..^1];
        }
        var extMatch = extensionRegex.Match(normalizedPath);
        if (extMatch.Success)
        {
            normalizedPath = extMatch.Groups[1].Value;
        }
        normalizedPath = normalizedPath.ToLowerInvariant();
    }

    // 3) Compute the canonical query string — keep only allowlisted params, in the order
    //    they appeared, with values intact. Everything else is stripped.
    var originalQs = context.Request.QueryString.HasValue ? context.Request.QueryString.Value! : string.Empty;
    string normalizedQs;
    if (string.IsNullOrEmpty(originalQs))
    {
        normalizedQs = string.Empty;
    }
    else
    {
        var kept = new List<string>();
        foreach (var pair in context.Request.Query)
        {
            if (allowedQueryParams.Contains(pair.Key))
            {
                foreach (var v in pair.Value)
                {
                    kept.Add(Uri.EscapeDataString(pair.Key) + (v is null ? "" : "=" + Uri.EscapeDataString(v)));
                }
            }
        }
        normalizedQs = kept.Count == 0 ? string.Empty : "?" + string.Join("&", kept);
    }

    // 4) Compute the canonical host. In dev (localhost/127.0.0.1/etc.) we never rewrite the
    //    host — that would break local testing. In prod, anything other than CanonicalHost
    //    redirects to CanonicalHost (this is what fixes the bare-domain "Page with redirect"
    //    flag — devnetsystems.com -> www.devnetsystems.com).
    var requestHost = context.Request.Host;
    var hostName = requestHost.Host;
    var needsHostRewrite = !hostsToLeaveAlone.Contains(hostName)
                           && !string.Equals(hostName, CanonicalHost, StringComparison.OrdinalIgnoreCase);

    // 5) Decide whether to redirect. Combining all changes into ONE 301 keeps Google's "single
    //    redirect hop" expectation satisfied no matter how many normalization rules fire.
    var pathChanged = !string.Equals(path, normalizedPath, StringComparison.Ordinal);
    var queryChanged = !string.Equals(originalQs, normalizedQs, StringComparison.Ordinal);

    if (pathChanged || queryChanged || needsHostRewrite)
    {
        string target;
        if (needsHostRewrite)
        {
            // Always go straight to https on the canonical host — even if the inbound was http,
            // we'd otherwise force two hops (host fix, then https upgrade).
            target = "https://" + CanonicalHost + normalizedPath + normalizedQs;
        }
        else
        {
            target = normalizedPath + normalizedQs;
        }
        context.Response.Redirect(target, permanent: true);
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
