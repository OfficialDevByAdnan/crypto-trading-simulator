using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoSim.Core.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Symbol { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
        public TransactionType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    }

    public enum TransactionType
    {
        Buy,
        Sell
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }
}
