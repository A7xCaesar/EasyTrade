using System.Threading.Tasks;
using DTO;

namespace EasyTrade_Crypto.Interfaces
{
    
    public interface IManageCryptoService
    {
        Task<AdminCryptoResult> AddCryptocurrencyAsync(AddCryptoDTO crypto);
        Task<AdminCryptoResult> RemoveCryptocurrencyAsync(string assetId);
        Task<AdminCryptoResult> UpdateCryptocurrencyAsync(CryptoDTO crypto);
        Task<AdminCryptoResult> SetCryptocurrencyStatusAsync(string assetId, bool isActive);
    }
} 