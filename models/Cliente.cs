

/// <summary>
/// Representa um cliente do sistema com controle de assinatura
/// </summary>

namespace PerfilWeb.Api.Models
{
    public class Client
    {

        public int Id { get; init; }
        public string Document { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DateTime ExpirationDate { get; private set; }
        public bool IsBlocked { get; private set; }

        public bool IsPending { get; private set; }
        public ClientMessage Message { get; private set; } = ClientMessage.ActivePlan;

        public bool IsValid => ExpirationDate >= DateTime.Now && !IsBlocked;

        private Client(string document, string description, DateTime expirationDate)
        {
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

        public static Client Create(string document, string description, DateTime expirationDate, bool allowPast = false)
        {
            Validate(document, description, expirationDate, allowPast);
            return new Client(document, description, expirationDate);
        }

        /// <summary>
        /// Valida os dados do cliente antes da criação
        /// </summary>

        private static void Validate(string document, string description, DateTime expirationDate, bool allowPast)
        {
            if (string.IsNullOrWhiteSpace(document))
                throw new ArgumentException("Document is required", nameof(document));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required", nameof(description));

            if (!allowPast && expirationDate < DateTime.Now)
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

        public void Update(bool? isBlocked = null, DateTime? expirationDate = null, bool? isPending = null)
        {
            // Atualizar bloqueio
            if (isBlocked.HasValue)
            {
                IsBlocked = isBlocked.Value;
            }

            // Atualizar data (com validação)
            if (expirationDate.HasValue)
            {
                if (expirationDate.Value <= DateTime.Now)
                    throw new ArgumentException("Expiration date must be in the future");

                ExpirationDate = expirationDate.Value;
            }

            // Atualizar pendente
            if (isPending.HasValue)
            {
                IsPending = isPending.Value;
            }

            // Atualizar mensagem COM BASE NO ESTADO FINAL
            UpdateMessage();
        }

        /// <summary>
        /// Atualiza a mensagem com base no estado atual do cliente
        /// </summary>
        private void UpdateMessage()
        {
            if (IsBlocked)
            {
                Message = ClientMessage.ManuallyBlocked;
            }
            else if (DateTime.Now > ExpirationDate)
            {
                Message = ClientMessage.ExpiredSubscription;
            }
            else if (IsPending)
            {
                Message = ClientMessage.PendingPayment; // ou outro tipo de pendência
            }
            else
            {
                Message = ClientMessage.ActivePlan;
            }
        }
    }
}
