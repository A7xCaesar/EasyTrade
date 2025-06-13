using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyTrade_Crypto.Pages
{
    public class AdminLoginModel : PageModel
    {
        [BindProperty]
        public AdminLoginInputModel LoginModel { get; set; } = new AdminLoginInputModel();

        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // Check if already authenticated as admin
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                Response.Redirect("/admin");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            
            if (LoginModel.Username == "admin" && LoginModel.Password == "admin123")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Administrator"),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("IsAdmin", "true")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                return RedirectToPage("/AdminDashboard");
            }
            else
            {
                ErrorMessage = "Invalid username or password.";
                return Page();
            }
        }
    }

    public class AdminLoginInputModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
} 