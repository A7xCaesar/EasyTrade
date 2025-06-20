using DTO;
using EasyTrade_Crypto.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyTrade_Crypto.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IAccountService accountService, ILogger<LoginModel> logger)
        {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty]
        public required InputModel Input { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public required string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public required string Password { get; set; }
        }

        public void OnGet()
        {
            // Renders the page.
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // Ensure returnUrl is a local path to prevent open-redirect attacks.
            returnUrl ??= Url.Content("~/TradingView");

            // Early return for invalid model state (e.g., missing required fields)
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login attempt with invalid model state.");
                ErrorMessage = "Invalid login attempt.";
                return Page();
            }

            try
            {
                if (Input == null)
                {
                    throw new ArgumentNullException(nameof(Input), "Input model cannot be null.");
                }

                var emailForLog = Input.Email;
                _logger.LogInformation("Login attempt for user: {Email}", emailForLog);

                // Use 'out var' to declare variables inline, preventing CS0136.
                if (_accountService.ValidateUserLogin(Input.Email, Input.Password, out UserLoginInfoDTO userInfo, out string errorMessage))
                {
                    _logger.LogInformation("User {Username} ({UserId}) logged in successfully.", userInfo.Username, userInfo.UserId);

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
                        new Claim(ClaimTypes.Name, userInfo.Username),
                        new Claim(ClaimTypes.Email, userInfo.Email),
                        new Claim(ClaimTypes.Role, userInfo.Role)
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);

                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid non-local returnUrl '{ReturnUrl}' provided. Redirecting to default.", returnUrl);
                        return RedirectToPage("/TradingView");
                    }
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for user: {Email}. Reason: {Reason}", Input.Email, errorMessage);
                    ErrorMessage = errorMessage;
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during login for user: {Email}", Input?.Email ?? "(null)");
                ErrorMessage = "An unexpected error occurred. Please try again.";
                return Page();
            }
        }
    }
}