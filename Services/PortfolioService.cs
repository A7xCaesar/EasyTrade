using System.Collections.Generic;
using System.Threading.Tasks;
using EasyTrade_Crypto.Interfaces;
using DTO;

namespace EasyTrade_Crypto.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IPortfolioDAL _portfolioDAL;

        public PortfolioService(IPortfolioDAL portfolioDAL)
        {
            _portfolioDAL = portfolioDAL;
        }

        public async Task<PortfolioDTO> GetPortfolioAsync(string userId)
        {
            var assetBalances = await _portfolioDAL.GetAssetBalancesAsync(userId);
            var tradeHistory = await _portfolioDAL.GetTradeHistoryAsync(userId);
            var totalValue = await _portfolioDAL.GetTotalPortfolioValueAsync(userId);

            var portfolio = new PortfolioDTO
            {
                UserId = userId,
                AssetBalances = assetBalances,
                TradeHistory = tradeHistory,
                TotalValue = totalValue
            };
            return portfolio;
        }

        public async Task<List<TradeDTO>> GetTradeHistoryAsync(string userId)
        {
            return await _portfolioDAL.GetTradeHistoryAsync(userId);
        }
    }
} 