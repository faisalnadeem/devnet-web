using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DevNetSystems.Services;

namespace DevNetSystems.Pages
{
    public class ContactModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public ContactModel(IEmailService emailService, IConfiguration configuration)
        {
            _emailService = emailService;
            _configuration = configuration;
        }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Subject { get; set; } = string.Empty;

        [BindProperty]
        public string Message { get; set; } = string.Empty;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, message = "Invalid form data." });
            }

            try
            {
                var toEmail = _configuration["EmailSettings:ToEmail"] ?? "devnetsystemz@gmail.com";
                
                var emailBody = $@"
                    <html>
                    <body>
                        <h2>New Contact Form Submission</h2>
                        <p><strong>Name:</strong> {Name}</p>
                        <p><strong>Email:</strong> {Email}</p>
                        <p><strong>Subject:</strong> {Subject}</p>
                        <p><strong>Message:</strong></p>
                        <p>{Message.Replace("\n", "<br>")}</p>
                    </body>
                    </html>";

                var emailSubject = $"Contact Form: {Subject}";

                var result = await _emailService.SendEmailAsync(
                    toEmail,
                    emailSubject,
                    emailBody,
                    Name,
                    Email
                );

                if (result)
                {
                    return Content("OK");
                }
                else
                {
                    return Content("Failed to send email. Please try again later.");
                }
            }
            catch (Exception ex)
            {
                return Content($"An error occurred: {ex.Message}");
            }
        }
    }
}
