using System;

namespace EasyTrade_Crypto.Models
{
    public class Trade
    {
        public string TradeId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public TradeType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalValue => Quantity * Price;
        public DateTime Timestamp { get; set; }
    }

    public enum TradeType
    {
        Buy,
        Sell
    }
}
