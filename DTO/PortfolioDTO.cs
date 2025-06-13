using DTO;
using System;
using System.Collections.Generic;

namespace DTO
{
    public class PortfolioDTO
    {
        public string UserId { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public List<AssetBalanceDTO> AssetBalances { get; set; } = new List<AssetBalanceDTO>();
        public List<TradeDTO> TradeHistory { get; set; } = new List<TradeDTO>();
    }



}