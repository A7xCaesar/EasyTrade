using System.Threading.Tasks;
using DTO;
using EasyTrade_Crypto.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace EasyTrade_Crypto.Pages
{
    public class PortfolioModel : PageModel
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioDTO Portfolio { get; set; }

        public PortfolioModel(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            Portfolio = await _portfolioService.GetPortfolioAsync(userId);
            return Page();
        }
    }
}