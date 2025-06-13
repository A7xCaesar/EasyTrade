using System;
using System.Collections.Generic;

namespace EasyTrade_Crypto.Models
{
    public class Portfolio
    {
        public string UserId { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public decimal AvailableBalance { get; set; }
        public List<PortfolioAsset> Assets { get; set; } = new List<PortfolioAsset>();
        public List<Trade> TradeHistory { get; set; } = new List<Trade>();
    }

    public class PortfolioAsset
    {
        public string AssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalValue => Quantity * CurrentPrice;
        public decimal ProfitLoss => (CurrentPrice - AverageBuyPrice) * Quantity;
        public decimal ProfitLossPercentage => AverageBuyPrice != 0 ? ((CurrentPrice - AverageBuyPrice) / AverageBuyPrice) * 100 : 0;
    }
} 