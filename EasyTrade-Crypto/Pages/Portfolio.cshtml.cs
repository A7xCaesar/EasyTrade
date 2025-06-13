using System.Threading.Tasks;
using DTO;
using EasyTrade_Crypto.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace EasyTrade_Crypto.Pages
{
    [Authorize]
    public class PortfolioModel : PageModel
    {
        private readonly IPortfolioService _portfolioService;
        private readonly ILogger<PortfolioModel> _logger;

        public PortfolioDTO Portfolio { get; set; }

        public PortfolioModel(IPortfolioService portfolioService, ILogger<PortfolioModel> logger)
        {
            _portfolioService = portfolioService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Authenticated request missing user identifier claim.");
                return Unauthorized();
            }

            Portfolio = await _portfolioService.GetPortfolioAsync(userId);
            return Page();
        }
    }
}