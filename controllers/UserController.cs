using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PerfilWeb.Api.DTOs;
using PerfilWeb.Api.Data;
using PerfilWeb.Api.Models;

namespace PerfilWeb.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém o perfil do usuário autenticado
        /// </summary>
        /// <response code="200">Perfil retornado com sucesso</response>
        /// <response code="401">Não autenticado</response>
        /// <response code="404">Usuário não encontrado</response>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(AuthErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Usuarios.FindAsync(userId);

            if (user == null)
                return NotFound(new AuthErrorResponseDto { Message = "Usuário não encontrado" });

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            });
        }

        /// <summary>
        /// Altera a senha do usuário autenticado
        /// </summary>
        /// <remarks>
        /// Permite que o usuário altere sua própria senha fornecendo a senha atual e a nova senha.
        ///
        /// **Exemplo de requisição:**
        /// ```json
        /// {
        ///   "currentPassword": "senha_antiga",
        ///   "newPassword": "Nova@Senha123",
        ///   "confirmNewPassword": "Nova@Senha123"
        /// }
        /// ```
        ///
        /// **Requisitos da nova senha:**
        /// - Mínimo de 6 caracteres
        /// - Deve ser diferente da senha atual
        /// - Nova senha e confirmação devem ser iguais
        /// </remarks>
        /// <param name="request">Dados para alteração de senha</param>
        /// <response code="200">Senha alterada com sucesso</response>
        /// <response code="400">Dados inválidos ou senha atual incorreta</response>
        /// <response code="401">Não autenticado</response>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SuccessResponseDto>> ChangePassword(
            [FromBody] ChangePasswordRequestDto request)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return BadRequest(new AuthErrorResponseDto { Message = "Senha atual é obrigatória" });

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new AuthErrorResponseDto { Message = "Nova senha é obrigatória" });

            if (request.NewPassword.Length < 6)
                return BadRequest(new AuthErrorResponseDto { Message = "A nova senha deve ter no mínimo 6 caracteres" });

            if (request.NewPassword != request.ConfirmNewPassword)
                return BadRequest(new AuthErrorResponseDto { Message = "Nova senha e confirmação não conferem" });

            if (request.CurrentPassword == request.NewPassword)
                return BadRequest(new AuthErrorResponseDto { Message = "A nova senha deve ser diferente da senha atual" });

            // Busca usuário
            var userId = GetCurrentUserId();
            var user = await _context.Usuarios.FindAsync(userId);

            if (user == null)
                return NotFound(new AuthErrorResponseDto { Message = "Usuário não encontrado" });

            // Verifica senha atual
            if (!user.VerifyPassword(request.CurrentPassword))
                return BadRequest(new AuthErrorResponseDto { Message = "Senha atual incorreta" });

            // Atualiza senha
            user.PasswordHash = Usuario.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new SuccessResponseDto { Message = "Senha alterada com sucesso" });
        }

        /// <summary>
        /// Altera o username do usuário autenticado
        /// </summary>
        /// <remarks>
        /// Permite que o usuário altere seu username. Por segurança, é necessário fornecer a senha atual.
        ///
        /// **Exemplo de requisição:**
        /// ```json
        /// {
        ///   "newUsername": "novo_usuario",
        ///   "password": "senha_atual"
        /// }
        /// ```
        ///
        /// **Requisitos do novo username:**
        /// - Mínimo de 3 caracteres
        /// - Máximo de 50 caracteres
        /// - Deve ser único (não pode existir outro usuário com o mesmo username)
        /// - Deve ser diferente do username atual
        /// </remarks>
        /// <param name="request">Dados para alteração de username</param>
        /// <response code="200">Username alterado com sucesso</response>
        /// <response code="400">Dados inválidos ou username já existe</response>
        /// <response code="401">Não autenticado ou senha incorreta</response>
        [HttpPost("change-username")]
        [ProducesResponseType(typeof(SuccessResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthErrorResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SuccessResponseDto>> ChangeUsername(
            [FromBody] ChangeUsernameRequestDto request)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(request.NewUsername))
                return BadRequest(new AuthErrorResponseDto { Message = "Novo username é obrigatório" });

            if (request.NewUsername.Length < 3)
                return BadRequest(new AuthErrorResponseDto { Message = "O username deve ter no mínimo 3 caracteres" });

            if (request.NewUsername.Length > 50)
                return BadRequest(new AuthErrorResponseDto { Message = "O username deve ter no máximo 50 caracteres" });

            if (string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new AuthErrorResponseDto { Message = "Senha é obrigatória para confirmar a alteração" });

            // Busca usuário
            var userId = GetCurrentUserId();
            var user = await _context.Usuarios.FindAsync(userId);

            if (user == null)
                return NotFound(new AuthErrorResponseDto { Message = "Usuário não encontrado" });

            // Verifica senha
            if (!user.VerifyPassword(request.Password))
                return BadRequest(new AuthErrorResponseDto { Message = "Senha incorreta" });

            // Verifica se o novo username é diferente do atual
            if (user.Username.Equals(request.NewUsername, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new AuthErrorResponseDto { Message = "O novo username deve ser diferente do atual" });

            // Verifica se o username já existe
            var usernameExists = await _context.Usuarios
                .AnyAsync(u => u.Username.ToLower() == request.NewUsername.ToLower() && u.Id != userId);

            if (usernameExists)
                return BadRequest(new AuthErrorResponseDto { Message = "Este username já está em uso" });

            // Atualiza username
            user.Username = request.NewUsername;
            await _context.SaveChangesAsync();

            return Ok(new SuccessResponseDto { Message = "Username alterado com sucesso" });
        }

        /// <summary>
        /// Obtém o ID do usuário autenticado a partir do token JWT
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }
    }
}