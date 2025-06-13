using System.Collections.Generic;
using System.Threading.Tasks;
using DTO;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Services
{
    public class AdminCryptoService : IReadOnlyCryptoService, IManageCryptoService
    {
        private readonly IAdminCryptoDAL _adminCryptoDAL;

        public AdminCryptoService(IAdminCryptoDAL adminCryptoDAL)
        {
            _adminCryptoDAL = adminCryptoDAL;
        }

        public async Task<List<CryptoDTO>> GetAllCryptocurrenciesAsync()
        {
            return await _adminCryptoDAL.GetAllCryptocurrenciesAsync();
        }

        public async Task<AdminCryptoResult> AddCryptocurrencyAsync(AddCryptoDTO crypto)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(crypto.Symbol))
            {
                return AdminCryptoResult.CreateError("Symbol is required.");
            }

            if (string.IsNullOrWhiteSpace(crypto.Name))
            {
                return AdminCryptoResult.CreateError("Name is required.");
            }

            if (crypto.InitialPrice <= 0)
            {
                return AdminCryptoResult.CreateError("Initial price must be greater than zero.");
            }

            return await _adminCryptoDAL.AddCryptocurrencyAsync(crypto);
        }

        public async Task<AdminCryptoResult> RemoveCryptocurrencyAsync(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return AdminCryptoResult.CreateError("Asset ID is required.");
            }

            return await _adminCryptoDAL.RemoveCryptocurrencyAsync(assetId);
        }

        public async Task<AdminCryptoResult> UpdateCryptocurrencyAsync(CryptoDTO crypto)
        {
            if (string.IsNullOrWhiteSpace(crypto.AssetId))
            {
                return AdminCryptoResult.CreateError("Asset ID is required.");
            }

            if (string.IsNullOrWhiteSpace(crypto.Symbol))
            {
                return AdminCryptoResult.CreateError("Symbol is required.");
            }

            if (string.IsNullOrWhiteSpace(crypto.Name))
            {
                return AdminCryptoResult.CreateError("Name is required.");
            }

            return await _adminCryptoDAL.UpdateCryptocurrencyAsync(crypto);
        }

        public async Task<CryptoDTO> GetCryptocurrencyByIdAsync(string assetId)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return null;
            }

            return await _adminCryptoDAL.GetCryptocurrencyByIdAsync(assetId);
        }

        public async Task<AdminCryptoResult> SetCryptocurrencyStatusAsync(string assetId, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return AdminCryptoResult.CreateError("Asset ID is required.");
            }

            return await _adminCryptoDAL.SetCryptocurrencyStatusAsync(assetId, isActive);
        }

        public async Task<bool> CryptocurrencyExistsAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return false;
            }
            return await _adminCryptoDAL.CryptocurrencyExistsAsync(symbol);
        }
    }
} 