using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoSim.Core.Entities
{
    public class Portfolio
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Symbol { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
