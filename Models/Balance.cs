using System;

namespace EasyTrade_Crypto.Models
{
    public class Balance
    {
        public string UserId { get; set; } = string.Empty;
        public decimal AvailableBalance { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
