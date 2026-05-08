using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevNetSystems.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int ErrorStatusCode { get; set; } = 500;

        public string Heading => ErrorStatusCode switch
        {
            404 => "Page not found",
            403 => "Access forbidden",
            410 => "This page is gone",
            _ => "Something went wrong"
        };

        public string SubHeading => ErrorStatusCode switch
        {
            404 => "We couldn't find the page you were looking for. It may have moved or no longer exists.",
            403 => "You don't have permission to view this page.",
            410 => "This resource has been permanently removed.",
            _ => "An error occurred while processing your request. Our team has been notified."
        };

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int? code = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            if (code.HasValue && code.Value >= 400 && code.Value < 600)
            {
                ErrorStatusCode = code.Value;
                Response.StatusCode = code.Value;
            }
            // Belt-and-braces: send noindex via HTTP header too. The meta-robots tag covers
            // crawlers that render HTML; this covers anything that only inspects headers and
            // any accidental direct hit on /error or /error/{code}.
            if (!Response.Headers.ContainsKey("X-Robots-Tag"))
            {
                Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            }
        }
    }
}
