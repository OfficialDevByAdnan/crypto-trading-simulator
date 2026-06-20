using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace CryptoSim.Core.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public decimal Balance { get; set; } = 10000; // Starting balance
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Portfolio> Portfolio { get; set; } = new List<Portfolio>();
    }
}
