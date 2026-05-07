using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using DevNetSystems.Services;

namespace DevNetSystems.Pages
{
    public class SitemapModel : PageModel
    {
        private readonly SiteSettings _site;
        private readonly IWebHostEnvironment _env;

        public SitemapModel(IOptions<SiteSettings> siteOptions, IWebHostEnvironment env)
        {
            _site = siteOptions.Value;
            _env = env;
        }

        public List<SitemapEntry> Entries { get; private set; } = new();

        public void OnGet()
        {
            var baseUrl = (_site.BaseUrl ?? "https://www.devnetsystems.com").TrimEnd('/');
            var pagesDir = Path.Combine(_env.ContentRootPath, "Pages");

            // Use the underlying Razor file's last-write time so <lastmod> reflects real changes.
            // Fall back to the project's last build time if the file isn't found.
            DateTime FileMtime(string relPath)
            {
                var full = Path.Combine(pagesDir, relPath);
                return System.IO.File.Exists(full)
                    ? System.IO.File.GetLastWriteTimeUtc(full)
                    : DateTime.UtcNow;
            }

            // All public URLs are lowercase to align with the canonical normalization in _SeoMetaPartial.
            Entries = new List<SitemapEntry>
            {
                new() { Url = baseUrl + "/",         LastModified = FileMtime("Index.cshtml"),    ChangeFreq = "weekly",  Priority = 1.0 },
                new() { Url = baseUrl + "/services", LastModified = FileMtime("Services.cshtml"), ChangeFreq = "monthly", Priority = 0.9 },
                new() { Url = baseUrl + "/products", LastModified = FileMtime("Products.cshtml"), ChangeFreq = "monthly", Priority = 0.9 },
                new() { Url = baseUrl + "/about",    LastModified = FileMtime("About.cshtml"),    ChangeFreq = "monthly", Priority = 0.7 },
                new() { Url = baseUrl + "/faq",      LastModified = FileMtime("Faq.cshtml"),      ChangeFreq = "monthly", Priority = 0.6 },
                new() { Url = baseUrl + "/contact",  LastModified = FileMtime("Contact.cshtml"),  ChangeFreq = "yearly",  Priority = 0.6 },
                new() { Url = baseUrl + "/privacy",  LastModified = FileMtime("Privacy.cshtml"),  ChangeFreq = "yearly",  Priority = 0.3 },
            };

            var productPageMtime = FileMtime("ProductDetail.cshtml");
            foreach (var product in ProductItem.GetAll())
            {
                var imagePath = product.ImagePath?.StartsWith("/") == true
                    ? baseUrl + product.ImagePath
                    : baseUrl + "/" + product.ImagePath;

                Entries.Add(new SitemapEntry
                {
                    Url = $"{baseUrl}/products/{product.Slug}",
                    LastModified = productPageMtime,
                    ChangeFreq = "monthly",
                    Priority = 0.8,
                    ImageUrl = imagePath,
                    ImageTitle = product.Title,
                    ImageCaption = product.Description
                });
            }
        }
    }

    public class SitemapEntry
    {
        public string Url { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public string ChangeFreq { get; set; } = "monthly";
        public double Priority { get; set; } = 0.5;

        // Optional <image:image> child for image sitemap extension.
        public string? ImageUrl { get; set; }
        public string? ImageTitle { get; set; }
        public string? ImageCaption { get; set; }
    }
}
