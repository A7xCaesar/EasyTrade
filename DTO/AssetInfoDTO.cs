using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class AssetInfoDTO
    {
        public int AssetId { get; set; }
        public required string Symbol { get; set; }
        public required string Name { get; set; }
        public double Price { get; set; }
    }
}
