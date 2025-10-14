using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PerfilWeb.Api.DTOs;

namespace PerfilWeb.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        // Mock de usuários - em produção viria de um banco de dados
        private static readonly List<User> _users = new()
        {
            new User { Username = "admin", Password = "adm123", Role = "Admin" },
            new User { Username = "admin1", Password = "adm123", Role = "Admin" }
        };

        /// <summary>
        /// Autentica um usuário e retorna um token JWT
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public ActionResult<LoginResponseDto> Login([FromBody] LoginRequestDto request)
        {
            var user = _users.FirstOrDefault(u => 
                u.Username == request.Username && 
                u.Password == request.Password);

            if (user == null)
                return Unauthorized(new AuthErrorResponseDto 
                { 
                    Message = "Invalid username or password" 
                });

            var (token, expiresAt) = GenerateJwtToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                ExpiresAt = expiresAt
            });
        }

        /// <summary>
        /// Gera um token JWT para o usuário autenticado
        /// </summary>
        private (string Token, DateTime ExpiresAt) GenerateJwtToken(User user)
        {
            var secretKey = _config["Jwt:Key"] 
                ?? throw new InvalidOperationException("Jwt:Key not configured");
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            var expiresAt = DateTime.UtcNow.AddHours(2);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = expiresAt,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return (tokenString, expiresAt);
        }
    }

    /// <summary>
    /// Representa um usuário do sistema
    /// </summary>
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }
}