using Microsoft.EntityFrameworkCore;
using PerfilWeb.Api.Models;

namespace PerfilWeb.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> RenovaServico { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da tabela Clientes
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("RenovaServico");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CNPJCPF)
                    .HasMaxLength(14)
                    .IsRequired();

                entity.HasIndex(e => e.CNPJCPF)
                    .IsUnique();

                entity.Property(e => e.Descricao)
                    .HasMaxLength(60)
                    .IsRequired();

                entity.Property(e => e.DataValidade)
                    .HasColumnType("date")
                    .IsRequired();

                entity.Property(e => e.Bloqueado)
                    .IsRequired();

                entity.Property(e => e.Mensagem)
                    .HasMaxLength(200);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                // Ignorar propriedades calculadas
                entity.Ignore(e => e.IsValid);
                entity.Ignore(e => e.Document);
                entity.Ignore(e => e.Description);
                entity.Ignore(e => e.ExpirationDate);
                entity.Ignore(e => e.IsBlocked);
                entity.Ignore(e => e.IsPending);
                entity.Ignore(e => e.Message);
            });

            // Configuração da tabela Usuarios
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Username)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasIndex(e => e.Username)
                    .IsUnique();

                entity.Property(e => e.PasswordHash)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Role)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.LastLogin);
            });
        }
    }
}