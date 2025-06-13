using System.Threading.Tasks;

namespace EasyTrade_Crypto.Interfaces
{
    public interface ITradeDAL
    {
        Task<bool> ExecuteTradeAsync(string userId, string symbol, string tradeType, decimal quantity);
    }
} 