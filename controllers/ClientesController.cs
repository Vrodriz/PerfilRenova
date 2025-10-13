using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PerfilWeb.Api.Models;
using PerfilRenovaWeb.api.Dtos;

namespace PerfilWeb.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private static readonly List<Client> _clients = CreateMockClients();

        private static List<Client> CreateMockClients()
        {
            var list = new List<Client>
            {
                Client.Create(1, "12.345.678/0001-90", "Empresa Tech Solutions Ltda", DateTime.Parse("2026-12-31")),
                Client.Create(3, "11.222.333/0001-44", "Indústria Metal Forte S.A", DateTime.Parse("2024-03-20"), true),
                Client.Create(6, "33.444.555/0001-66", "Construtora Alicerce", DateTime.Parse("2026-01-05")),
                Client.Create(8, "66.777.888/0001-99", "Clínica Médica Saúde Total", DateTime.Parse("2023-12-15"), true),
                Client.Create(9, "77.888.999/0001-22", "Supermercado Economia Ltda", DateTime.Parse("2026-06-30")),
                Client.Create(12, "10.111.222/0001-55", "Advocacia Justiça & Direito", DateTime.Parse("2024-01-10"), true),
                Client.Create(13, "20.222.333/0001-66", "Padaria Pão Quente", DateTime.Parse("2026-08-15")),
                Client.Create(16, "20.222.333/0001-66", "Padaria Pão Quente", DateTime.Parse("2026-08-15")),
                Client.Create(17, "20.222.333/0001-66", "Padaria Pão Quente", DateTime.Parse("2026-08-15"))
            };

            var client2 = Client.Create(2, "98.765.432/0001-10", "Comércio Digital Brasil", DateTime.Parse("2025-11-15"));
            client2.Block(Models.ClientMessage.ManuallyBlocked);
            list.Add(client2);

            // Clientes pendentes
            var client4 = Client.Create(4, "55.666.777/0001-88", "Serviços Administrativos XYZ", DateTime.Parse("2025-11-05"));
            client4.SetPending(true, Models.ClientMessage.PendingPayment);
            list.Add(client4);

            var client7 = Client.Create(7, "44.555.666/0001-77", "Restaurante Sabor & Arte", DateTime.Parse("2025-11-01"));
            client7.SetPending(true, Models.ClientMessage.RenewalUnderReview);
            list.Add(client7);

            var client11 = Client.Create(11, "99.000.111/0001-44", "Escola de Idiomas Global", DateTime.Parse("2025-12-25"));
            client11.SetPending(true, Models.ClientMessage.PendingDocuments);
            list.Add(client11);

            var client14 = Client.Create(14, "30.333.444/0001-77", "Farmácia Saúde & Vida", DateTime.Parse("2025-10-30"));
            client14.SetPending(true, Models.ClientMessage.PendingDocuments);
            list.Add(client14);

            // Cliente vencido e bloqueado
            var client5 = Client.Create(5, "22.333.444/0001-55", "Transportadora Nacional", DateTime.Parse("2024-08-10"), true);
            client5.Block(Models.ClientMessage.ExpiredSubscription);
            list.Add(client5);

            // Cliente bloqueado
            var client10 = Client.Create(10, "88.999.000/0001-33", "Academia Corpo em Forma", DateTime.Parse("2025-10-20"));
            client10.Block(Models.ClientMessage.ManuallyBlocked);
            list.Add(client10);

            // Cliente inadimplente
            var client15 = Client.Create(15, "40.444.555/0001-88", "Oficina Mecânica Roda Viva", DateTime.Parse("2024-05-20"), true);
            client15.Block(Models.ClientMessage.ManuallyBlocked);
            list.Add(client15);

            return list.OrderBy(c => c.Id).ToList();
        }

        /// <summary>
        /// Retorna todos os clientes com filtros e paginação
        /// Compatível com as expectativas do frontend
        /// </summary>
        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedClientsResponseDto), StatusCodes.Status200OK)]
        public ActionResult<PaginatedClientsResponseDto> GetClientes(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _clients.AsQueryable();

            // Filtro de busca (CNPJ ou Razão Social)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower().Trim();
                query = query.Where(c =>
                    c.Document.ToLower().Contains(searchLower) ||
                    c.Description.ToLower().Contains(searchLower));
            }

            // Filtro de status
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = status.ToLower() switch
                {
                    "ativos" => query.Where(c => !c.IsBlocked && c.ExpirationDate >= DateTime.Now),
                    "bloqueado" => query.Where(c => c.IsBlocked),
                    "vencidos" => query.Where(c => c.ExpirationDate < DateTime.Now && !c.IsBlocked),
                    "pendentes" => query.Where(c => c.IsPending),
                    "próximos a vencer" => query.Where(c =>
                        !c.IsBlocked &&
                        c.ExpirationDate >= DateTime.Now &&
                        c.ExpirationDate <= DateTime.Now.AddDays(15)),
                    _ => query
                };
            }

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Paginação
            var clients = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDto)
                .ToList();

            return Ok(new PaginatedClientsResponseDto
            {
                Data = clients,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Atualiza múltiplos clientes de uma vez (Bulk Update)
        /// </summary>
        [Authorize]
        [HttpPatch("bulk")]
        [ProducesResponseType(typeof(BulkUpdateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public ActionResult<BulkUpdateResponseDto> BulkUpdateClients([FromBody] BulkUpdateRequestDto request)
        {
            if (request.ClientIds == null || !request.ClientIds.Any())
                return BadRequest(new ErrorResponseDto { Message = "No client IDs provided" });

            var updatedClients = new List<ClientResponseDtos>();
            var errors = new List<string>();

            foreach (var id in request.ClientIds)
            {
                var client = _clients.FirstOrDefault(c => c.Id == id);

                if (client == null)
                {
                    errors.Add($"Client {id} not found");
                    continue;
                }

                try
                {
                    // Converte "S"/"N" para boolean
                    bool? isBlocked = request.Bloqueado switch
                    {
                        "S" => true,
                        "N" => false,
                        _ => null
                    };

                    // Converte string ISO date para DateTime
                    DateTime? expirationDate = null;
                    if (!string.IsNullOrWhiteSpace(request.DataValidade))
                    {
                        if (DateTime.TryParse(request.DataValidade, out var parsedDate))
                            expirationDate = parsedDate;
                    }

                    // Atualiza o cliente usando o método do modelo
                    client.Update(isBlocked, expirationDate, request.Pendente);
                    updatedClients.Add(MapToDto(client));
                }
                catch (Exception ex)
                {
                    errors.Add($"Client {id}: {ex.Message}");
                }
            }

            return Ok(new BulkUpdateResponseDto
            {
                UpdatedClients = updatedClients,
                UpdatedCount = updatedClients.Count,
                Errors = errors
            });
        }

        /// <summary>
        /// Atualiza um cliente pelo ID (COMPATÍVEL COM FRONTEND)
        /// Frontend chama: PATCH /api/clientes/{id}
        /// </summary>
        [Authorize]
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ClientResponseDtos), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public ActionResult<ClientResponseDtos> UpdateClient(int id, [FromBody] UpdatesClientesRequestDTO request)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);

            if (client == null)
                return NotFound(new ErrorResponseDto { Message = "Client not found" });

            try
            {
                // Converte "S"/"N" para boolean
                bool? isBlocked = request.Bloqueado switch
                {
                    "S" => true,
                    "N" => false,
                    _ => null
                };

                // Converte string ISO date para DateTime
                DateTime? expirationDate = null;
                if (!string.IsNullOrWhiteSpace(request.DataValidade))
                {
                    if (DateTime.TryParse(request.DataValidade, out var parsedDate))
                        expirationDate = parsedDate;
                }

                // Atualiza o cliente usando o método do modelo
                client.Update(isBlocked, expirationDate, request.Pendente);

                return Ok(MapToDto(client));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Invalid data",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Bloqueia um cliente específico (rota adicional)
        /// </summary>
        [Authorize]
        [HttpPost("{id}/block")]
        [ProducesResponseType(typeof(ClientResponseDtos), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public ActionResult<ClientResponseDtos> BlockClient(int id, [FromQuery] string? reason = null)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);

            if (client == null)
                return NotFound(new ErrorResponseDto { Message = "Client not found" });

            var blockReason = reason?.ToLower() switch
            {
                "expired" => Models.ClientMessage.ExpiredSubscription,
                "payment" => Models.ClientMessage.PendingPayment,
                _ => Models.ClientMessage.ManuallyBlocked
            };

            client.Block(blockReason);
            return Ok(MapToDto(client));
        }

        /// <summary>
        /// Renova a assinatura de um cliente (rota adicional)
        /// </summary>
        [Authorize]
        [HttpPost("{id}/renew")]
        [ProducesResponseType(typeof(ClientResponseDtos), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public ActionResult<ClientResponseDtos> RenewSubscription(int id, [FromBody] UpdatesClientesRequestDTO request)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);

            if (client == null)
                return NotFound(new ErrorResponseDto { Message = "Client not found" });

            try
            {
                if (string.IsNullOrWhiteSpace(request.DataValidade))
                    return BadRequest(new ErrorResponseDto { Message = "Expiration date is required" });

                if (!DateTime.TryParse(request.DataValidade, out var expirationDate))
                    return BadRequest(new ErrorResponseDto { Message = "Invalid date format" });

                client.Renew(expirationDate);
                return Ok(MapToDto(client));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Invalid expiration date",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Desbloqueia um cliente (rota adicional)
        /// </summary>
        [Authorize]
        [HttpPost("{id}/unblock")]
        [ProducesResponseType(typeof(ClientResponseDtos), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public ActionResult<ClientResponseDtos> UnblockClient(int id)
        {
            var client = _clients.FirstOrDefault(c => c.Id == id);

            if (client == null)
                return NotFound(new ErrorResponseDto { Message = "Client not found" });

            try
            {
                client.Unblock();
                return Ok(MapToDto(client));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Cannot unblock client",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// Mapeia o modelo Client para o DTO compatível com o frontend
        /// </summary>
        private static ClientResponseDtos MapToDto(Client client)
        {
            return new ClientResponseDtos
            {
                Id = client.Id,
                Descricao = client.Description,
                Cnpjcpf = client.Document,
                DataValidade = client.ExpirationDate.ToString("yyyy-MM-dd"),
                Bloqueado = client.IsBlocked ? "S" : "N",
                Pendente = client.IsPending,
                Mensagem = GetMessageText(client.Message, client.IsBlocked, client.ExpirationDate)
            };
        }

        /// <summary>
        /// Converte o enum ClientMessage para texto legível em português
        /// </summary>
        private static string GetMessageText(Models.ClientMessage message, bool isBlocked, DateTime expirationDate)
        {
            // Verifica se está vencido
            if (expirationDate < DateTime.Now && !isBlocked)
                return "Assinatura expirada";

            if (expirationDate < DateTime.Now && isBlocked)
                return "Assinatura vencida e bloqueada";

            return message switch
            {
                Models.ClientMessage.ActivePlan => "Assinatura ativa",
                Models.ClientMessage.Renewed => $"Assinatura renovada até {expirationDate:dd/MM/yyyy}",
                Models.ClientMessage.Unblocked => "Cliente desbloqueado",
                Models.ClientMessage.ManuallyBlocked => "Bloqueado por falta de pagamento",
                Models.ClientMessage.ExpiredSubscription => "Assinatura vencida",
                Models.ClientMessage.PendingPayment => "Aguardando confirmação de pagamento",
                Models.ClientMessage.PendingDocuments => "Aguardando documentos",
                Models.ClientMessage.RenewalUnderReview => "Renovação em análise",
                _ => "Status desconhecido"
            };
        }
    }
}