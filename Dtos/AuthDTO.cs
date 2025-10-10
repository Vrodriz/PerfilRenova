namespace PerfilWeb.Api.DTOs
{
    /// <summary>
    /// DTO para requisição de login
    /// </summary>
    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para resposta de login bem-sucedido
    /// </summary>
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// DTO para resposta de erro de autenticação
    /// </summary>
    public class AuthErrorResponseDto
    {
        public string Message { get; set; } = string.Empty;
    }
}