using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DTO;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardModel : PageModel
    {
        private readonly IAdminCryptoService _adminCryptoService;

        public AdminDashboardModel(IAdminCryptoService adminCryptoService)
        {
            _adminCryptoService = adminCryptoService;
        }

        [BindProperty]
        public AddCryptoDTO NewCrypto { get; set; } = new AddCryptoDTO();

        [BindProperty]
        public CryptoDTO EditCrypto { get; set; } = new CryptoDTO();

        public List<CryptoDTO> Cryptocurrencies { get; set; } = new List<CryptoDTO>();
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCryptocurrenciesAsync();
        }

        public async Task<IActionResult> OnPostAddCryptoAsync()
        {
            // Debug information
            Message = $"POST received. NewCrypto is {(NewCrypto == null ? "NULL" : "not null")}";
            if (NewCrypto != null)
            {
                Message += $" - Symbol: '{NewCrypto.Symbol}', Name: '{NewCrypto.Name}', Price: {NewCrypto.InitialPrice}";
            }
            
            try
            {
                Message += " - Calling service...";
                var result = await _adminCryptoService.AddCryptocurrencyAsync(NewCrypto);
                
                if (result.Success)
                {
                    Message = $"SUCCESS: Cryptocurrency '{NewCrypto.Symbol}' has been added successfully.";
                    IsSuccess = true;
                    NewCrypto = new AddCryptoDTO(); // Reset form
                }
                else
                {
                    Message = $"SERVICE ERROR: {result.ErrorMessage}";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                Message = $"EXCEPTION: {ex.Message} - StackTrace: {ex.StackTrace}";
                IsSuccess = false;
            }

            await LoadCryptocurrenciesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveCryptoAsync(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                Message = "Invalid cryptocurrency selected.";
                IsSuccess = false;
                await LoadCryptocurrenciesAsync();
                return Page();
            }

            // Get crypto info for the message
            var crypto = await _adminCryptoService.GetCryptocurrencyByIdAsync(assetId);
            var cryptoName = crypto?.Symbol ?? "Unknown";

            var result = await _adminCryptoService.RemoveCryptocurrencyAsync(assetId);
            
            if (result.Success)
            {
                Message = $"Cryptocurrency '{cryptoName}' has been removed successfully.";
                IsSuccess = true;
            }
            else
            {
                Message = result.ErrorMessage;
                IsSuccess = false;
            }

            await LoadCryptocurrenciesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateCryptoAsync()
        {
            // Debug information
            Message = $"UPDATE POST received. EditCrypto is {(EditCrypto == null ? "NULL" : "not null")}";
            if (EditCrypto != null)
            {
                Message += $" - AssetId: '{EditCrypto.AssetId}', Symbol: '{EditCrypto.Symbol}', Name: '{EditCrypto.Name}', Price: {EditCrypto.CurrentPrice}";
            }

            try
            {
                Message += " - Calling update service...";
                var result = await _adminCryptoService.UpdateCryptocurrencyAsync(EditCrypto);
                
                if (result.Success)
                {
                    Message = $"SUCCESS: Cryptocurrency '{EditCrypto.Symbol}' has been updated successfully.";
                    IsSuccess = true;
                }
                else
                {
                    Message = $"UPDATE SERVICE ERROR: {result.ErrorMessage}";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                Message = $"UPDATE EXCEPTION: {ex.Message} - StackTrace: {ex.StackTrace}";
                IsSuccess = false;
            }

            await LoadCryptocurrenciesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRefreshListAsync()
        {
            await LoadCryptocurrenciesAsync();
            Message = "Cryptocurrency list has been refreshed.";
            IsSuccess = true;
            return Page();
        }

        private async Task LoadCryptocurrenciesAsync()
        {
            try
            {
                Cryptocurrencies = await _adminCryptoService.GetAllCryptocurrenciesAsync();
            }
            catch (System.Exception ex)
            {
                Message = $"Error loading cryptocurrencies: {ex.Message}";
                IsSuccess = false;
                Cryptocurrencies = new List<CryptoDTO>();
            }
        }
    }
} 