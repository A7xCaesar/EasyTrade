using System.Threading.Tasks;
using System.Collections.Generic;
using DTO;

namespace EasyTrade_Crypto.Interfaces
{
    public interface IPortfolioService
    {
        Task<PortfolioDTO> GetPortfolioAsync(string userId);
        Task<List<TradeDTO>> GetTradeHistoryAsync(string userId);                                
    }
} 