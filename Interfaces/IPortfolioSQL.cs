using DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyTrade_Crypto.Interfaces
{
    public interface IPortfolioSQL
    {
        Task<List<AssetBalanceDTO>> GetAssetBalancesAsync(string userId);
        Task<decimal> GetTotalPortfolioValueAsync(string userId);
        Task<decimal> GetAvailableCashBalanceAsync(string userId);
        Task<List<TradeDTO>> GetTradeHistoryAsync(string userId);
        Task<List<ProfitLossDTO>> GetProfitLossAsync(string userId);
        Task<decimal> GetLatestPriceAsync(string assetId);
        Task<decimal?> GetBalanceAsync(string userId, string assetId);
        Task UpsertBalanceAsync(string userId, string assetId, decimal amount);
        Task CleanupZeroBalancesAsync(string userId);
    }
}