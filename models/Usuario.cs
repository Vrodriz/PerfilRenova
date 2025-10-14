namespace PerfilWeb.Api.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        // Construtor vazio para EF Core
        public Usuario() { }

        /// <summary>
        /// Verifica se a senha está correta
        /// </summary>
        public bool VerifyPassword(string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
        }

        /// <summary>
        /// Cria hash seguro da senha
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Atualiza o último login
        /// </summary>
        public void UpdateLastLogin()
        {
            LastLogin = DateTime.UtcNow;
        }
    }
}