using Microsoft.AspNetCore.Mvc.RazorPages;
using DTO;
using EasyTrade_Crypto.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace EasyTrade_Crypto.Pages
{
    public class TradingViewModel : PageModel
    {
        private readonly IReadOnlyCryptoService _cryptoService;
        private readonly ITradeService _tradeService;

        [BindProperty]
        public string SelectedSymbol { get; set; } = string.Empty;

        [BindProperty]
        public decimal Quantity { get; set; }

        [BindProperty]
        public string TradeType { get; set; } = "buy";

        public List<CryptoDTO> Cryptos { get; private set; } = new();

        public TradingViewModel(IReadOnlyCryptoService cryptoService, ITradeService tradeService)
        {
            _cryptoService = cryptoService;
            _tradeService = tradeService;
        }

        public async Task OnGetAsync()
        {
            // Load all active cryptocurrencies to display in the dashboard
            var all = await _cryptoService.GetAllCryptocurrenciesAsync();
            Cryptos = all.Where(c => c.IsActive).OrderBy(c => c.Symbol).ToList();
        }

        public async Task<IActionResult> OnPostTradeAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Login");
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(SelectedSymbol))
            {
                ModelState.AddModelError(string.Empty, "Please select an asset to trade.");
                await OnGetAsync();
                return Page();
            }

            if (Quantity <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please enter a valid quantity greater than zero.");
                await OnGetAsync();
                return Page();
            }

            var (success, error) = await _tradeService.ExecuteTradeAsync(userId, SelectedSymbol, TradeType, Quantity);
            
            if (success)
            {
                // Clear form and show success message
                TempData["SuccessMessage"] = $"✅ Successfully {TradeType.ToLower()} {Quantity:F8} {SelectedSymbol.ToUpper()}!";
                SelectedSymbol = string.Empty;
                Quantity = 0;
                TradeType = "buy";
            }
            else
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await OnGetAsync(); // reload list
            return Page();
        }
    }
}
