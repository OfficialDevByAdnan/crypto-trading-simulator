using CryptoSim.Core.Entities;
using CryptoSim.Core.Interfaces;
using CryptoSim.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CryptoSim.Infrastructure.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cache;
        private readonly ILogger<CryptoService> _logger;

        public CryptoService(
            HttpClient httpClient,
            ApplicationDbContext context,
            ICacheService cache,
            ILogger<CryptoService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<CryptoPrice> GetPriceAsync(string symbol)
        {
            return await _cache.GetOrSetAsync(
                $"crypto:price:{symbol}",
                async () =>
                {
                    try
                    {
                        // Using CoinGecko API
                        var response = await _httpClient.GetFromJsonAsync<CoinGeckoResponse>(
                            $"https://api.coingecko.com/api/v3/simple/price?ids={symbol}&vs_currencies=usd&include_24hr_change=true&include_24hr_vol=true&include_market_cap=true");

                        if (response != null)
                        {
                            var price = new CryptoPrice
                            {
                                Symbol = symbol.ToUpper(),
                                Price = response.Usd,
                                Change24h = response.Usd_24h_change ?? 0,
                                Volume24h = response.Usd_24h_vol ?? 0,
                                MarketCap = response.Usd_market_cap ?? 0,
                                LastUpdated = DateTime.UtcNow
                            };

                            // Update database
                            var existing = await _context.CryptoPrices
                                .FirstOrDefaultAsync(c => c.Symbol == symbol.ToUpper());

                            if (existing != null)
                            {
                                existing.Price = price.Price;
                                existing.Change24h = price.Change24h;
                                existing.Volume24h = price.Volume24h;
                                existing.MarketCap = price.MarketCap;
                                existing.LastUpdated = DateTime.UtcNow;
                            }
                            else
                            {
                                await _context.CryptoPrices.AddAsync(price);
                            }

                            await _context.SaveChangesAsync();
                            return price;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching crypto price for {Symbol}", symbol);
                    }

                    return null;
                },
                TimeSpan.FromSeconds(30) // Cache for 30 seconds
            );
        }

        public async Task<IEnumerable<CryptoPrice>> GetAllPricesAsync()
        {
            return await _cache.GetOrSetAsync(
                "crypto:prices:all",
                async () =>
                {
                    return await _context.CryptoPrices
                        .OrderByDescending(c => c.MarketCap)
                        .Take(20)
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(5)
            );
        }

        public async Task<bool> ExecuteTradeAsync(Guid userId, string symbol, decimal amount, TransactionType type)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                var price = await GetPriceAsync(symbol);
                if (price == null)
                    return false;

                var total = amount * price.Price;

                if (type == TransactionType.Buy)
                {
                    if (user.Balance < total)
                        throw new InvalidOperationException("Insufficient balance");

                    user.Balance -= total;

                    // Update portfolio
                    var portfolio = await _context.Portfolios
                        .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == symbol);

                    if (portfolio != null)
                    {
                        var totalCost = (portfolio.Quantity * portfolio.AverageBuyPrice) + total;
                        portfolio.Quantity += amount;
                        portfolio.AverageBuyPrice = totalCost / portfolio.Quantity;
                        portfolio.LastUpdated = DateTime.UtcNow;
                    }
                    else
                    {
                        await _context.Portfolios.AddAsync(new Portfolio
                        {
                            UserId = userId,
                            Symbol = symbol,
                            Quantity = amount,
                            AverageBuyPrice = price.Price,
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                }
                else // Sell
                {
                    var portfolio = await _context.Portfolios
                        .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == symbol);

                    if (portfolio == null || portfolio.Quantity < amount)
                        throw new InvalidOperationException("Insufficient holdings");

                    portfolio.Quantity -= amount;
                    user.Balance += total;

                    if (portfolio.Quantity == 0)
                        _context.Portfolios.Remove(portfolio);
                    else
                        portfolio.LastUpdated = DateTime.UtcNow;
                }

                // Record transaction
                var trade = new Transaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Symbol = symbol,
                    Amount = amount,
                    Price = price.Price,
                    Total = total,
                    Type = type,
                    Timestamp = DateTime.UtcNow,
                    Status = TransactionStatus.Completed
                };

                await _context.Transactions.AddAsync(trade);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate cache
                await _cache.RemoveAsync($"user:portfolio:{userId}");
                await _cache.RemoveAsync($"user:transactions:{userId}");

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<Transaction>> GetUserTransactionsAsync(Guid userId)
        {
            return await _cache.GetOrSetAsync(
                $"user:transactions:{userId}",
                async () =>
                {
                    return await _context.Transactions
                        .Where(t => t.UserId == userId)
                        .OrderByDescending(t => t.Timestamp)
                        .Take(100)
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(2)
            );
        }

        public Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime date)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CryptoPrice>> GetTopCryptosAsync(int limit = 10)
        {
            throw new NotImplementedException();
        }

        public Task GetUserPortfolioAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<CancellationToken> GetPortfolioAsync(Guid guid)
        {
            throw new NotImplementedException();
        }

        private class CoinGeckoResponse
        {
            public decimal Usd { get; set; }
            [JsonPropertyName("usd_24h_change")]
            public decimal? Usd_24h_change { get; set; }
            [JsonPropertyName("usd_24h_vol")]
            public decimal? Usd_24h_vol { get; set; }
            [JsonPropertyName("usd_market_cap")]
            public long? Usd_market_cap { get; set; }
        }
    }
}
