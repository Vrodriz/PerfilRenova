namespace PerfilWeb.Api.Models
{
    public class Client
    {
        // Chave primária numérica
        public int Id { get; set; }
        
        // Campos do banco
        public string CNPJCPF { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataValidade { get; set; }
        public bool Bloqueado { get; set; }
        public string? Mensagem { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Propriedades calculadas (não vão pro banco)
        public bool IsValid => DataValidade >= DateTime.Now && !Bloqueado;
        
        // Aliases para compatibilidade com código antigo
        public string Document => CNPJCPF;
        public string Description => Descricao;
        public DateTime ExpirationDate => DataValidade;
        public bool IsBlocked => Bloqueado;
        
        public bool IsPending => 
            Mensagem?.Contains("Aguardando", StringComparison.OrdinalIgnoreCase) == true || 
            Mensagem?.Contains("em análise", StringComparison.OrdinalIgnoreCase) == true;
        
        public ClientMessage Message => ParseMessageFromString(Mensagem);

        // Construtor vazio para EF Core
        public Client() { }

        public void Block(ClientMessage reason = ClientMessage.ManuallyBlocked)
        {
            Bloqueado = true;
            Mensagem = GetMessageText(reason, true, DataValidade);
        }

        public void Renew(DateTime newExpirationDate)
        {
            if (newExpirationDate <= DateTime.Now)
                throw new ArgumentException("Data de validade deve ser futura");

            DataValidade = newExpirationDate;
            Bloqueado = false;
            Mensagem = $"Assinatura renovada até {newExpirationDate:dd/MM/yyyy}";
        }

        public void Unblock()
        {
            if (DateTime.Now > DataValidade)
                throw new InvalidOperationException("Não é possível desbloquear cliente com assinatura vencida");

            Bloqueado = false;
            Mensagem = "Cliente desbloqueado";
        }

        public void SetPending(bool isPending, ClientMessage? message = null)
        {
            if (isPending && message.HasValue)
                Mensagem = GetMessageText(message.Value, Bloqueado, DataValidade);
        }

        public void Update(bool? isBlocked = null, DateTime? expirationDate = null, bool? isPending = null)
        {
            if (isBlocked.HasValue)
            {
                Bloqueado = isBlocked.Value;
                if (isBlocked.Value)
                    Mensagem = "Bloqueado por falta de pagamento";
            }

            if (expirationDate.HasValue)
            {
                if (expirationDate.Value <= DateTime.Now)
                    throw new ArgumentException("Data de validade deve ser futura");
                
                DataValidade = expirationDate.Value;
                Mensagem = $"Assinatura renovada até {expirationDate.Value:dd/MM/yyyy}";
            }

            if (isPending.HasValue && isPending.Value)
            {
                Mensagem = "Aguardando confirmação de pagamento";
            }

            if (isBlocked.HasValue && !isBlocked.Value && expirationDate.HasValue)
            {
                Mensagem = $"Assinatura renovada até {expirationDate.Value:dd/MM/yyyy}";
            }
        }

        private static ClientMessage ParseMessageFromString(string? mensagem)
        {
            if (string.IsNullOrEmpty(mensagem))
                return ClientMessage.ActivePlan;

            var lower = mensagem.ToLower();
            
            if (lower.Contains("renovada")) return ClientMessage.Renewed;
            if (lower.Contains("desbloqueado")) return ClientMessage.Unblocked;
            if (lower.Contains("falta de pagamento")) return ClientMessage.ManuallyBlocked;
            if (lower.Contains("expirada") || lower.Contains("vencida")) return ClientMessage.ExpiredSubscription;
            if (lower.Contains("aguardando") && lower.Contains("pagamento")) return ClientMessage.PendingPayment;
            if (lower.Contains("aguardando") && lower.Contains("documentos")) return ClientMessage.PendingDocuments;
            if (lower.Contains("em análise")) return ClientMessage.RenewalUnderReview;
            
            return ClientMessage.ActivePlan;
        }

        private static string GetMessageText(ClientMessage message, bool isBlocked, DateTime expirationDate)
        {
            if (expirationDate < DateTime.Now && !isBlocked)
                return "Assinatura expirada";

            if (expirationDate < DateTime.Now && isBlocked)
                return "Assinatura vencida e bloqueada";

            return message switch
            {
                ClientMessage.ActivePlan => "Assinatura ativa",
                ClientMessage.Renewed => $"Assinatura renovada até {expirationDate:dd/MM/yyyy}",
                ClientMessage.Unblocked => "Cliente desbloqueado",
                ClientMessage.ManuallyBlocked => "Bloqueado por falta de pagamento",
                ClientMessage.ExpiredSubscription => "Assinatura vencida",
                ClientMessage.PendingPayment => "Aguardando confirmação de pagamento",
                ClientMessage.PendingDocuments => "Aguardando documentos",
                ClientMessage.RenewalUnderReview => "Renovação em análise",
                _ => "Status desconhecido"
            };
        }
    }
}