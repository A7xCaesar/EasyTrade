using System.Collections.Generic;
using System.Threading.Tasks;
using DTO;
namespace EasyTrade_Crypto.Interfaces
{
    public interface IPortfolioDAL
    {
        Task<List<AssetBalanceDTO>> GetAssetBalancesAsync(string userId);
        Task<decimal> GetLatestPriceAsync(string assetId);
        Task<List<TradeDTO>> GetTradeHistoryAsync(string userId);
        Task<decimal> GetTotalPortfolioValueAsync(string userId);
    }
} 