namespace DevNetSystems.Services
{
    public class SiteSettings
    {
        public string BaseUrl { get; set; } = "https://www.devnetsystems.com";
        public string Name { get; set; } = "DEVNET SYSTEMS";
        public string DefaultOgImage { get; set; } = "/img/site-images/computer-laptop.jpeg";
        public string TwitterHandle { get; set; } = string.Empty;
        public string Locale { get; set; } = "en_GB";

        // Search Console / Bing Webmaster verification tokens. Populate in appsettings once issued.
        public string GoogleSiteVerification { get; set; } = string.Empty;
        public string BingSiteVerification { get; set; } = string.Empty;
    }
}
