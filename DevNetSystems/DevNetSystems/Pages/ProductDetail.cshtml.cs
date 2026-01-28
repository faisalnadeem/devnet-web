using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;

namespace DevNetSystems.Pages
{
    public class ProductDetailModel : PageModel
    {
        public ProductItem? Product { get; private set; }

        public IActionResult OnGet(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            Product = ProductItem
                .GetAll()
                .FirstOrDefault(p =>
                    p != null &&
                    !string.IsNullOrEmpty(p.Slug) &&
                    string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

            if (Product is null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
