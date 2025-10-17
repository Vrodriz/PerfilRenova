using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PerfilWeb.Api.DTOs;
using PerfilWeb.Api.Data;
using PerfilWeb.Api.Models;

namespace PerfilWeb.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public AuthController(IConfiguration config, ApplicationDbContext context)
        {
            _config = config;
            _context = context;
        }

        /// <summary>
        /// Cria usuário inicial do sistema (ENDPOINT TEMPORÁRIO)
        /// </summary>
        /// <remarks>
        /// Este endpoint deve ser usado apenas uma vez para criar o primeiro usuário administrador.
        ///
        /// **ATENÇÃO:** Remova este endpoint após criar o primeiro usuário por questões de segurança.
        ///
        /// **Credenciais padrão:**
        /// - Username: admin
        /// - Password: adm123
        ///
        /// **IMPORTANTE:** Troque a senha imediatamente após o primeiro login!
        /// </remarks>
        /// <response code="200">Usuário inicial criado com sucesso</response>
        /// <response code="400">Já existem usuários cadastrados no sistema</response>
        [HttpPost("seed-initial-user")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> SeedInitialUser()
        {
            // Verifica se já existe algum usuário
            if (await _context.Usuarios.AnyAsync())
            {
                return BadRequest(new { message = "Usuários já existem no sistema" });
            }

            var initialUser = new Usuario
            {
                Username = "admin",
                PasswordHash = Usuario.HashPassword("adm123"),
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Usuarios.Add(initialUser);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "✅ Usuário inicial criado com sucesso!",
                username = "admin",
                temporaryPassword = "adm123",
                warning = "⚠️ Troque esta senha imediatamente após o primeiro login!"
            });
        }

        /// <summary>
        /// Autentica um usuário e retorna um token JWT
        /// </summary>
        /// <remarks>
        /// Realiza a autenticação do usuário verificando as credenciais fornecidas.
        ///
        /// **Exemplo de requisição:**
        /// ```json
        /// {
        ///   "username": "admin",
        ///   "password": "sua_senha_segura"
        /// }
        /// ```
        ///
        /// **Resposta de sucesso:**
        /// ```json
        /// {
        ///   "token": "eyJhbGciOiJIUzI1NiIs...",
        ///   "username": "admin",
        ///   "role": "Admin",
        ///   "expiresAt": "2025-01-15T14:30:00Z"
        /// }
        /// ```
        ///
        /// O token retornado deve ser incluído no header Authorization das próximas requisições:
        /// ```
        /// Authorization: Bearer {token}
        /// ```
        /// </remarks>
        /// <param name="request">Credenciais de login (username e password)</param>
        /// <response code="200">Login realizado com sucesso, retorna o token JWT</response>
        /// <response code="401">Credenciais inválidas ou usuário inativo</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthErrorResponseDto), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            // Busca usuário no banco
            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            // Verifica se existe e se a senha está correta
            if (user == null || !user.VerifyPassword(request.Password))
            {
                return Unauthorized(new AuthErrorResponseDto 
                { 
                    Message = "Usuário ou senha inválidos" 
                });
            }

            // Atualiza último login
            user.UpdateLastLogin();
            await _context.SaveChangesAsync();

            // Gera token
            var (token, expiresAt) = GenerateJwtToken(user);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                ExpiresAt = expiresAt
            });
        }

        private (string Token, DateTime ExpiresAt) GenerateJwtToken(Usuario user)
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
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
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
}