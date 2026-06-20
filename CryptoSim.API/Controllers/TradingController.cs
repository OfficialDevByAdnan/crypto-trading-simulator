using CryptoSim.Core.Entities;
using CryptoSim.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CryptoSim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TradingController : ControllerBase
    {
        private readonly ICryptoService _cryptoService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<TradingController> _logger;

        public TradingController(
            ICryptoService cryptoService,
            IUserRepository userRepository,
            ILogger<TradingController> logger)
        {
            _cryptoService = cryptoService;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet("prices")]
        public async Task<IActionResult> GetPrices()
        {
            var prices = await _cryptoService.GetAllPricesAsync();
            return Ok(prices);
        }

        [HttpGet("price/{symbol}")]
        public async Task<IActionResult> GetPrice(string symbol)
        {
            var price = await _cryptoService.GetPriceAsync(symbol);
            if (price == null)
                return NotFound($"No data found for {symbol}");

            return Ok(price);
        }

        [HttpPost("trade")]
        public async Task<IActionResult> ExecuteTrade([FromBody] TradeRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var result = await _cryptoService.ExecuteTradeAsync(
                    userId,
                    request.Symbol,
                    request.Amount,
                    request.Type);

                if (result)
                {
                    var updatedUser = await _userRepository.GetByIdAsync(userId);
                    return Ok(new
                    {
                        Success = true,
                        Message = $"Trade executed successfully",
                        NewBalance = updatedUser?.Balance
                    });
                }

                return BadRequest("Trade execution failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trade execution failed");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var transactions = await _cryptoService.GetUserTransactionsAsync(userId);
            return Ok(transactions);
        }

        [HttpGet("portfolio")]
        public async Task<IActionResult> GetPortfolio()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            await _cryptoService.GetUserPortfolioAsync(userId);
            return Ok();
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _userRepository.GetByIdAsync(userId);
            return Ok(new { Balance = user?.Balance ?? 0 });
        }
    }

    public class TradeRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
    }

}
