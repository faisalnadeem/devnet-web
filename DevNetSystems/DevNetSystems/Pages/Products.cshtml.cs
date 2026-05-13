using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevNetSystems.Pages
{
    public class ProductsModel : PageModel
    {
        public List<ProductItem> Items { get; private set; } = new List<ProductItem>();

        public void OnGet()
        {
            Items = ProductItem.GetAll().ToList();
        }
    }

    public class ProductItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string SitePath { get; set; } = string.Empty;


        public string Slug => Title.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant();

        public static IEnumerable<ProductItem> GetAll()
        {
            return new List<ProductItem>
            {
                new ProductItem
                {
                    Title = "RouteKnots",
                    Description = "RouteKnots is an intelligent route planning application that allows users to create optimized routes from point A to point B with multiple customizable stops in between. The app automatically calculates the most efficient route based on the starting and ending locations. Users can view these optimized routes directly on Google Maps and save them for future reference through route history tracking.",
                    ImagePath = "img/site-images/routeknots.jpg",
                    SitePath = "https://routeknots.com/"
                },
                new ProductItem
                {
                    Title = "Optaxia",
                    Description = "Optaxia is a geo-enabled image capture and management application designed for accurate location-based documentation. Users can capture or import images embedded with precise geographic coordinates, including latitude and longitude. All images are securely stored on the cloud, making it ideal for professionals who require verified location data with every photograph.",
                    ImagePath = "img/site-images/optaxia.jpeg",
                    SitePath = "https://optaxia.com"
                },
                new ProductItem
                {
                    Title = "Aythn",
                    Description = "Aythn is an AI-powered conversational platform that helps businesses automatically capture and engage leads from Facebook in real time. It uses intelligent question flows and eligibility logic to qualify prospects instantly. By automating conversations and lead screening, Aythen enables faster responses, improved engagement, and higher conversion rates with minimal manual effort.",
                    ImagePath = "img/site-images/aythen.jpg",
                    SitePath = "https://aythn.com"
                },
                new ProductItem
                {
                    Title = "PasLogic",
                    Description = "PasLogic is a cutting-edge software solution built specifically for ECO providers and Green Deal installers to streamline and optimize their business operations. Developed in line with industry regulations and governing body principles, it enhances workflow efficiency and operational performance. With a focus on innovation and future-ready technology, PasLogic continuously evolves to meet changing market demands and industry needs.",
                    ImagePath = "img/site-images/paslogic.jpg",
                    SitePath = "https://paslogic.com/"
                },
                new ProductItem
                {
                    Title = "Gripoints",
                    Description = "Gripoints is a task-based reward management platform designed to help parents guide their children's growth through positive reinforcement. Parents can assign meaningful tasks, track progress, and reward achievements transparently within a single platform. The system encourages responsibility, discipline, and healthy habits while strengthening parent?child engagement.",
                    ImagePath = "img/site-images/childrengrowth.jpg",
                    SitePath = "https://gripoints.com"
                },
                new ProductItem
                {
                    Title = "RetroKnots",
                    Description = "RetroKnots is a comprehensive retrofit management platform designed to automate and streamline retrofit services from planning to execution. It simplifies survey checklists, retrofit coordination, and real-time documentation, enabling teams to manage retrofit projects efficiently. By centralizing workflows and ensuring accurate runtime records, RetroKnots improves compliance, and visibility.",
                    ImagePath = "img/site-images/retroknots.jpg",
                    SitePath = "https://retroknots.com/"
                },
                new ProductItem
                {
                    Title = "MLR360",
                    Description = "MLR360 is an advanced medico-legal documentation platform that automates detailed medical reporting for road traffic and workplace injuries. It generates structured reports covering accident scenarios, injury details, clinical findings, and prognosis. These court-ready reports help doctors, legal professionals, and insurers clearly understand patient conditions with complete analysis and context.",
                    ImagePath = "img/site-images/mlr.jpg",
                    SitePath = "https://www.mlr360.com/"
                },
                new ProductItem
                {
                    Title = "PassKnots",
                    Description = "PassKnots Vault is a secure password manager web app where each user’s entries are encrypted with their own master key. Users can store site credentials, organize their vault, and share entries with others by email, with optional invites for recipients who do not yet have an account.",
                    ImagePath = "img/site-images/PassKnots.png",
                    SitePath = "https://www.passknots.com/"
                }
            };
        }
    }
}
