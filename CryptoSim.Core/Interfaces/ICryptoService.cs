using CryptoSim.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoSim.Core.Interfaces
{
    public interface ICryptoService
    {
        Task<CryptoPrice> GetPriceAsync(string symbol);
        Task<IEnumerable<CryptoPrice>> GetAllPricesAsync();
        Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime date);
        Task<IEnumerable<CryptoPrice>> GetTopCryptosAsync(int limit = 10);
        Task<bool> ExecuteTradeAsync(Guid userId, string symbol, decimal amount, TransactionType type);
        Task<IEnumerable<Transaction>> GetUserTransactionsAsync(Guid userId);
        Task GetUserPortfolioAsync(Guid userId);
        Task<CancellationToken> GetPortfolioAsync(Guid guid);
    }
}
