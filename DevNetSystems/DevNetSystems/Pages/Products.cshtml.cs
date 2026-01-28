using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevNetSystems.Pages
{
    public class ProductsModel : PageModel
    {
        public List<ProductItem> Items = new List<ProductItem>();
        public void OnGet()
        {
            Items.Add(new ProductItem() { Title = "RouteKnots", Description = "RouteKnots is an intelligent route planning application that allows users to create optimized routes from point A to point B with multiple customizable stops in between. The app automatically calculates the most efficient route based on the starting and ending locations. Users can view these optimized routes directly on Google Maps and save them for future reference through route history tracking.", ImagePath = "img/site-images/routeknots.jpg", SitePath = "https://routeknots.com/" });
            Items.Add(new ProductItem() { Title = "Optaxia", Description = "Optaxia is a geo-enabled image capture and management application designed for accurate location-based documentation. Users can capture or import images embedded with precise geographic coordinates, including latitude and longitude. All images are securely stored on the cloud, making it ideal for professionals who require verified location data with every photograph.", SitePath = "https://optaxia.com", ImagePath = "img/site-images/optaxia.jpeg" });
            Items.Add(new ProductItem() { Title = "Aythen", Description = "Aythen is an AI-powered conversational platform that helps businesses automatically capture and engage leads from Facebook in real time. It uses intelligent question flows and eligibility logic to qualify prospects instantly. By automating conversations and lead screening, Aythen enables faster responses, improved engagement, and higher conversion rates with minimal manual effort.", SitePath = "https://aythen.com", ImagePath = "img/site-images/aythen.jpg" });
            Items.Add(new ProductItem() { Title = "PasLogic", Description = "PasLogic is a cutting-edge software solution built specifically for ECO providers and Green Deal installers to streamline and optimize their business operations. Developed in line with industry regulations and governing body principles, it enhances workflow efficiency and operational performance. With a focus on innovation and future-ready technology, PasLogic continuously evolves to meet changing market demands and industry needs.", SitePath = "https://paslogic.com/", ImagePath = "img/site-images/paslogic.jpg" });
            Items.Add(new ProductItem() { Title = "Gripoints", Description = "Gripoints is a task-based reward management platform designed to help parents guide their children's growth through positive reinforcement. Parents can assign meaningful tasks, track progress, and reward achievements transparently within a single platform. The system encourages responsibility, discipline, and healthy habits while strengthening parent–child engagement.", SitePath = "https://gripoints.com", ImagePath = "img/site-images/childrengrowth.jpg" });
            Items.Add(new ProductItem() { Title = "RetroKnots", Description = "RetroKnots is a comprehensive retrofit management platform designed to automate and streamline retrofit services from planning to execution. It simplifies survey checklists, retrofit coordination, and real-time documentation, enabling teams to manage retrofit projects efficiently. By centralizing workflows and ensuring accurate runtime records, RetroKnots improves compliance, and visibility.", SitePath = "https://retroknots.com/", ImagePath = "img/site-images/retroknots.jpg" });
            Items.Add(new ProductItem() { Title = "MLR360", Description = "MLR360 is an advanced medico-legal documentation platform that automates detailed medical reporting for road traffic and workplace injuries. It generates structured reports covering accident scenarios, injury details, clinical findings, and prognosis. These court-ready reports help doctors, legal professionals, and insurers clearly understand patient conditions with complete analysis and context.", SitePath = "https://www.mlr360.com/", ImagePath = "img/site-images/mlr.jpg" });
        }
        
    }
    public class ProductItem 
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string SitePath { get; set; } = string.Empty;
    }
}
