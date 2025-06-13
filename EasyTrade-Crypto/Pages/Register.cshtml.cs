using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DTO;
using EasyTrade_Crypto.Services;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IRegistrationService _registrationService;

        [BindProperty]
        public RegisterInputModel Input { get; set; }

        public RegisterModel(IRegistrationService registrationService, IConfiguration config)
        {
            _registrationService = registrationService;
            _config = config;
        }

        public void OnGet()
        {
            // Render page
        }

        public IActionResult OnPost()
        {
            // ModelState validation check removed as requested

            if (!_registrationService.RegisterUser(Input, out string errorMessage))
            {
                ModelState.AddModelError(string.Empty, errorMessage);
                return Page();
            }

            return RedirectToPage("/Login");
        }
    }
}
