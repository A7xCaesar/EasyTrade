using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class TradeDTO
    {
        public string TradeId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AssetId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
