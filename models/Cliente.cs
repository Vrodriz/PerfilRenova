namespace PerfilWeb.Api.Models
{
    /// <summary>
    /// Representa um cliente do sistema com informações de assinatura
    /// </summary>
    public class Cliente
    {
        public string CNPJCPF { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public DateTime DataValidade { get; private set; }
        public bool Bloqueado { get; private set; }
        public string Mensagem { get; private set; } = string.Empty;

        /// <summary>
        /// Verifica se a assinatura está válida (não vencida e não bloqueada)
        /// </summary>
        public bool EstaValido => DataValidade >= DateTime.Now && !Bloqueado;

        private Cliente(string cnpjCpf, string descricao, DateTime dataValidade)
        {
            CNPJCPF = cnpjCpf;
            Descricao = descricao;
            DataValidade = dataValidade;
            Bloqueado = false;
            Mensagem = "Plano ativo";
        }

        public static Cliente Criar(string cnpjCpf, string descricao, DateTime dataValidade)
        {
            if (string.IsNullOrWhiteSpace(cnpjCpf))
                throw new ArgumentException("CNPJ/CPF é obrigatório", nameof(cnpjCpf));

            if (string.IsNullOrWhiteSpace(descricao))
                throw new ArgumentException("Descrição é obrigatória", nameof(descricao));

            if (dataValidade <= DateTime.Now)
                throw new ArgumentException("Data de validade deve ser futura", nameof(dataValidade));

            return new Cliente(cnpjCpf, descricao, dataValidade);
        }

        public void Bloquear(string motivo = "Assinatura bloqueada manualmente")
        {
            Bloqueado = true;
            Mensagem = motivo ?? "Bloqueio sem motivo especificado";
        }

        public void Renovar(int meses = 1)
        {
            if (meses <= 0)
                throw new ArgumentException("Meses deve ser maior que zero", nameof(meses));

            if (EstaValido)
                DataValidade = DataValidade.AddMonths(meses);
            else
                DataValidade = DateTime.Now.AddMonths(meses);

            Bloqueado = false;
            Mensagem = $"Assinatura renovada por {meses} mês(es)";
        }

        public void Desbloquear()
        {
            if (DateTime.Now > DataValidade)
                throw new InvalidOperationException("Não é possivel desbloquear cliente com assinatura vencida");

            Bloqueado = false;
            Mensagem = "Cliente desbloqueado";
        }
    }
}
