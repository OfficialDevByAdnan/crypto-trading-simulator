using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoSim.Core.Entities
{
    public class CryptoPrice
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty; // BTC, ETH, etc.
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Change24h { get; set; }
        public decimal Volume24h { get; set; }
        public long MarketCap { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
