using System.Collections.Generic;
using System.Threading.Tasks;
using DTO;

namespace EasyTrade_Crypto.Interfaces
{
    
    public interface IReadOnlyCryptoService
    {
        Task<List<CryptoDTO>> GetAllCryptocurrenciesAsync();
        Task<CryptoDTO> GetCryptocurrencyByIdAsync(string assetId);
        Task<bool> CryptocurrencyExistsAsync(string symbol);
    }
} 