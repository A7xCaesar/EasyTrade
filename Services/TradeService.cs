using System.Threading.Tasks;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Services
{
    public class TradeService : ITradeService
    {
        private readonly ITradeDAL _tradeDal;
        public TradeService(ITradeDAL tradeDal)
        {
            _tradeDal = tradeDal;
        }

        public async Task<(bool success, string errorMessage)> ExecuteTradeAsync(string userId, string symbol, string tradeType, decimal quantity)
        {
            string errorMessage = string.Empty;
            if (quantity <= 0)
            {
                errorMessage = "Quantity must be greater than zero.";
                return (false, errorMessage);
            }
            var ok = await _tradeDal.ExecuteTradeAsync(userId, symbol, tradeType, quantity);
            if (!ok) errorMessage = "Failed to execute trade.";
            return (ok, errorMessage);
        }
    }
} 