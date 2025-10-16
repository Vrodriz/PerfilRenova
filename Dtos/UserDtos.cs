namespace PerfilWeb.Api.DTOs
{
    /// <summary>
    /// DTO para requisição de alteração de senha
    /// </summary>
    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para requisição de alteração de username
    /// </summary>
    public class ChangeUsernameRequestDto
    {
        public string NewUsername { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // Confirmar com senha por segurança
    }

    /// <summary>
    /// DTO para resposta de sucesso genérica
    /// </summary>
    public class SuccessResponseDto
    {
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para obter perfil do usuário
    /// </summary>
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}