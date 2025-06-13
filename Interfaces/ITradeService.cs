using System.Threading.Tasks;

namespace EasyTrade_Crypto.Interfaces
{
    public interface ITradeService
    {
        Task<(bool success, string errorMessage)> ExecuteTradeAsync(string userId, string symbol, string tradeType, decimal quantity);
    }
} 