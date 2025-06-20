using System;
using System.Threading.Tasks;
using EasyTrade_Crypto.Interfaces;

namespace EasyTrade_Crypto.Services
{
    public class TradeService : ITradeService
    {
        private readonly ITradeDAL _tradeDal;
        
        public TradeService(ITradeDAL tradeDal)
        {
            _tradeDal = tradeDal ?? throw new ArgumentNullException(nameof(tradeDal));
        }

        public async Task<(bool success, string errorMessage)> ExecuteTradeAsync(string userId, string symbol, string tradeType, decimal quantity)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(userId))
                    return (false, "User ID is required.");
                
                if (string.IsNullOrWhiteSpace(symbol))
                    return (false, "Asset symbol is required.");
                
                if (string.IsNullOrWhiteSpace(tradeType))
                    return (false, "Trade type is required.");
                
                if (quantity <= 0)
                    return (false, "Quantity must be greater than zero.");
                
                // Validate trade type
                var normalizedTradeType = tradeType.ToLower();
                if (normalizedTradeType != "buy" && normalizedTradeType != "sell")
                    return (false, "Trade type must be 'buy' or 'sell'.");
                
                // Execute the trade
                var success = await _tradeDal.ExecuteTradeAsync(userId, symbol, normalizedTradeType, quantity);
                
                if (success)
                {
                    return (true, string.Empty);
                }
                else
                {
                    return (false, "Trade execution failed. Please check your balance and try again.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                System.Diagnostics.Debug.WriteLine($"TradeService.ExecuteTradeAsync failed: {ex.Message}");
                
                // Return the specific error message from the exception
                return (false, ex.Message);
            }
        }
    }
} 