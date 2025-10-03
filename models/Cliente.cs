namespace PerfilWeb.Api.Models
{
    public class Cliente
    {
        public string CNPJCPF { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public DateTime DataValidade { get; private set; }
        public bool Bloqueado { get; private set; }
        public string Mensagem { get; private set; } = string.Empty;

        public bool EstaValido => DataValidade >= DateTime.Now && !Bloqueado;

        private Cliente(string cnpjCpf, string descricao, DateTime dataValidade)
        {
            CNPJCPF = cnpjCpf;
            Descricao = descricao;
            DataValidade = dataValidade;
            Bloqueado = false;
            Mensagem = "Plano ativo";
        }

        public static Cliente Criar(string cnpjCpf, string descricao, DateTime dataValidade, bool permitirPassado = false)
        {
            if (string.IsNullOrWhiteSpace(cnpjCpf))
                throw new ArgumentException("CNPJ/CPF é obrigatório", nameof(cnpjCpf));

            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("Descrição é obrigatória", nameof(descricao));

            if (!permitirPassado && dataValidade < DateTime.Now)
                throw new ArgumentException("Data de validade deve ser futura", nameof(dataValidade));

            return new Cliente(cnpjCpf, descricao, dataValidade);
        }

        public void Bloquear(string motivo = "Assinatura bloqueada manualmente")
        {
            Bloqueado = true;
            Mensagem = motivo ?? "Bloqueio sem motivo especificado";
        }

        /// <summary>
        /// Renova a assinatura até uma data específica
        /// </summary>
        public void Renovar(DateTime novaDataValidade)
        {
            if (novaDataValidade <= DateTime.Now)
                throw new ArgumentException("Data de validade deve ser futura", nameof(novaDataValidade));

            DataValidade = novaDataValidade;
            Bloqueado = false;
            Mensagem = $"Assinatura renovada até {novaDataValidade:dd/MM/yyyy}";
        }

        public void Desbloquear()
        {
            if (DateTime.Now > DataValidade)
                throw new InvalidOperationException("Não é possível desbloquear cliente com assinatura vencida");

            Bloqueado = false;
            Mensagem = "Cliente desbloqueado";
        }
    }
}