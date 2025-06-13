using System.Collections.Generic;
using System.Threading.Tasks;
using DTO;

namespace EasyTrade_Crypto.Interfaces
{
    public interface IAdminCryptoDAL
    {
        Task<List<CryptoDTO>> GetAllCryptocurrenciesAsync();
        Task<AdminCryptoResult> AddCryptocurrencyAsync(AddCryptoDTO crypto);
        Task<AdminCryptoResult> RemoveCryptocurrencyAsync(string assetId);
        Task<AdminCryptoResult> UpdateCryptocurrencyAsync(CryptoDTO crypto);
        Task<CryptoDTO> GetCryptocurrencyByIdAsync(string assetId);
        Task<AdminCryptoResult> SetCryptocurrencyStatusAsync(string assetId, bool isActive);
        Task<bool> CryptocurrencyExistsAsync(string symbol);
    }
} 