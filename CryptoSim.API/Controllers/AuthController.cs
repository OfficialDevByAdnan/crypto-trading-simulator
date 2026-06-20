using CryptoSim.Core.Entities;
using CryptoSim.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CryptoSim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null)
                    return BadRequest("Email already registered");

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = Convert.ToBase64String(BCrypt.Generate(
                        Encoding.UTF8.GetBytes(request.Password),
                        BCrypt.PasswordToByteArray(request.Password.ToCharArray()),
                        10)),
                    Balance = 10000m, // Starting balance
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepository.CreateAsync(user);

                var token = GenerateJwtToken(user);
                return Ok(new { token, user.Username, user.Email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                return StatusCode(500, "An error occurred during registration");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                    return Unauthorized("Invalid credentials");

                // Verify password
                var passwordBytes = Encoding.UTF8.GetBytes(request.Password);
                var salt = BCrypt.PasswordToByteArray(request.Password.ToCharArray());
                var hash = Convert.ToBase64String(BCrypt.Generate(passwordBytes, salt, 10));

                if (!hash.Equals(user.PasswordHash))
                    return Unauthorized("Invalid credentials");

                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                var token = GenerateJwtToken(user);
                return Ok(new { token, user.Username, user.Email, user.Balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", request.Email);
                return StatusCode(500, "An error occurred during login");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("Balance", user.Balance.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
