using CryptoSim.Core.Entities;
using CryptoSim.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CryptoSim.API.Hubs
{
    public class TradingHub : Hub
    {
        private readonly ICryptoService _cryptoService;
        private readonly ILogger<TradingHub> _logger;
        private static readonly Dictionary<string, string> _userConnections = new();

        public TradingHub(ICryptoService cryptoService, ILogger<TradingHub> logger)
        {
            _cryptoService = cryptoService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                _userConnections[Context.ConnectionId] = userId;
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                // Send initial portfolio data
                var portfolio = await _cryptoService.GetPortfolioAsync(Guid.Parse(userId));
                await Clients.Caller.SendAsync("PortfolioUpdate", portfolio);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _userConnections.Remove(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeToPriceUpdates(string symbol)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"price_{symbol}");
            _logger.LogInformation("User {User} subscribed to {Symbol}",
                Context.User?.Identity?.Name, symbol);
        }

        public async Task UnsubscribeFromPriceUpdates(string symbol)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"price_{symbol}");
        }

        public async Task ExecuteTrade(string symbol, decimal amount, string type)
        {
            try
            {
                var userId = Guid.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var transactionType = Enum.Parse<TransactionType>(type, true);

                var result = await _cryptoService.ExecuteTradeAsync(userId, symbol, amount, transactionType);

                if (result)
                {
                    await Clients.Caller.SendAsync("TradeExecuted", new
                    {
                        Symbol = symbol,
                        Amount = amount,
                        Type = type,
                        Status = "Success",
                        Timestamp = DateTime.UtcNow
                    });

                    // Notify all clients about price change
                    await Clients.Group($"price_{symbol}").SendAsync("PriceUpdate",
                        await _cryptoService.GetPriceAsync(symbol));
                }
                else
                {
                    await Clients.Caller.SendAsync("TradeError", "Trade execution failed");
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("TradeError", ex.Message);
            }
        }

        public async Task GetPrice(string symbol)
        {
            var price = await _cryptoService.GetPriceAsync(symbol);
            await Clients.Caller.SendAsync("PriceUpdate", price);
        }
    }
}
