namespace PerfilWeb.Api.Models
{
    /// <summary>
    /// Representa um cliente do sistema com controle de assinatura
    /// </summary>
    public class Client
    {
        
        public int Id { get; init; }
        
        public string Document { get; init; } = string.Empty; 
        public string Description { get; init; } = string.Empty;
        public DateTime ExpirationDate { get; private set; }
        public bool IsBlocked { get; private set; }
        public bool IsPending { get; private set; } 
        public ClientMessage Message { get; private set; }

        
        public bool IsValid => ExpirationDate >= DateTime.Now && !IsBlocked;

      
        private Client(int id, string document, string description, DateTime expirationDate)
        {
            Id = id;
            Document = document;
            Description = description;
            ExpirationDate = expirationDate;
            IsBlocked = false;
            IsPending = false;
            Message = ClientMessage.ActivePlan;
        }

        /// <summary>
        /// Factory method para criar um novo cliente com validação
        /// </summary>
        public static Client Create(int id, string document, string description, DateTime expirationDate, bool allowPastDate = false)
        {
            Validate(document, description, expirationDate, allowPastDate);
            return new Client(id, document, description, expirationDate);
        }

        /// <summary>
        /// Valida os dados do cliente antes da criação
        /// </summary>
        private static void Validate(string document, string description, DateTime expirationDate, bool allowPastDate)
        {
            if (string.IsNullOrWhiteSpace(document))
                throw new ArgumentException("Document is required", nameof(document));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required", nameof(description));

            if (!allowPastDate && expirationDate < DateTime.Now)
                throw new ArgumentException("Expiration date must be in the future", nameof(expirationDate));
        }

        /// <summary>
        /// Bloqueia o cliente com um motivo específico
        /// </summary>
        public void Block(ClientMessage reason = ClientMessage.ManuallyBlocked)
        {
            IsBlocked = true;
            IsPending = false;
            Message = reason;
        }

        /// <summary>
        /// Renova a assinatura do cliente até uma data específica
        /// </summary>
        public void Renew(DateTime newExpirationDate)
        {
            if (newExpirationDate <= DateTime.Now)
                throw new ArgumentException("Expiration date must be in the future", nameof(newExpirationDate));

            ExpirationDate = newExpirationDate;
            IsBlocked = false;
            IsPending = false;
            Message = ClientMessage.Renewed;
        }

        /// <summary>
        /// Desbloqueia o cliente se a assinatura ainda estiver válida
        /// </summary>
        public void Unblock()
        {
            if (DateTime.Now > ExpirationDate)
                throw new InvalidOperationException("Cannot unblock client with expired subscription");

            IsBlocked = false;
            IsPending = false;
            Message = ClientMessage.Unblocked;
        }

        /// <summary>
        /// Marca o cliente como pendente (aguardando documentos/pagamento)
        /// </summary>
        public void SetPending(bool isPending, ClientMessage? message = null)
        {
            IsPending = isPending;
            if (message.HasValue)
                Message = message.Value;
        }

        /// <summary>
        /// Atualiza múltiplos campos do cliente de uma vez
        /// </summary>
        public void Update(bool? isBlocked = null, DateTime? expirationDate = null, bool? isPending = null)
        {
            if (isBlocked.HasValue)
            {
                IsBlocked = isBlocked.Value;
                if (isBlocked.Value)
                    Message = ClientMessage.ManuallyBlocked;
            }

            if (expirationDate.HasValue)
            {
                if (expirationDate.Value <= DateTime.Now)
                    throw new ArgumentException("Expiration date must be in the future");
                
                ExpirationDate = expirationDate.Value;
                Message = ClientMessage.Renewed;
            }

            if (isPending.HasValue)
                IsPending = isPending.Value;

            // Se desbloqueou e renovou, considera ativo
            if (isBlocked.HasValue && !isBlocked.Value && expirationDate.HasValue)
            {
                Message = ClientMessage.Renewed;
            }
        }
    }

    /// <summary>
    /// Enum para as mensagens de status do cliente
    /// </summary>
    public enum ClientMessage
    {
        ActivePlan,
        Renewed,
        Unblocked,
        ManuallyBlocked,
        ExpiredSubscription,
        BlockedWithoutReason,
        PendingPayment,
        PendingDocuments,
        RenewalUnderReview
    }
}