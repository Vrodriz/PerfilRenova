using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PerfilWeb.Api.Models;
using PerfilWeb.Api.Data;
using PerfilRenovaWeb.api.Dtos;

namespace PerfilWeb.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedClientsResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedClientsResponseDto>> GetClientes(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Clientes.AsQueryable();

            // Filtro de busca
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower().Trim();
                query = query.Where(c =>
                    c.CNPJCPF.ToLower().Contains(searchLower) ||
                    c.Descricao.ToLower().Contains(searchLower));
            }

            // Filtro de status
            if (!string.IsNullOrWhiteSpace(status))
            {
                var now = DateTime.Now;
                query = status.ToLower() switch
                {
                    "ativos" => query.Where(c => !c.Bloqueado && c.DataValidade >= now),
                    "bloqueado" => query.Where(c => c.Bloqueado),
                    "vencidos" => query.Where(c => c.DataValidade < now && !c.Bloqueado),
                    "pendentes" => query.Where(c => 
                        c.Mensagem != null && 
                        (c.Mensagem.Contains("Aguardando") || c.Mensagem.Contains("em análise"))),
                    "próximos a vencer" => query.Where(c =>
                        !c.Bloqueado &&
                        c.DataValidade >= now &&
                        c.DataValidade <= now.AddDays(15)),
                    _ => query
                };
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var clients = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PaginatedClientsResponseDto
            {
                Data = clients.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        [Authorize]
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ClientResponseDtos), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientResponseDtos>> UpdateClient(
            int id, 
            [FromBody] UpdatesClientesRequestDTO request)
        {
            var client = await _context.Clientes.FindAsync(id);

            if (client == null)
                return NotFound(new ErrorResponseDto { Message = "Cliente não encontrado" });

            try
            {
                bool? isBlocked = request.Bloqueado switch
                {
                    "S" => true,
                    "N" => false,
                    _ => null
                };

                DateTime? expirationDate = null;
                if (!string.IsNullOrWhiteSpace(request.DataValidade))
                {
                    if (DateTime.TryParse(request.DataValidade, out var parsedDate))
                        expirationDate = parsedDate;
                }

                client.Update(isBlocked, expirationDate, request.Pendente);
                
                await _context.SaveChangesAsync();

                return Ok(MapToDto(client));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Dados inválidos",
                    Details = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPatch("bulk")]
        [ProducesResponseType(typeof(BulkUpdateResponseDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<BulkUpdateResponseDto>> BulkUpdateClientes(
            [FromBody] BulkUpdateRequestDto request)
        {
            if (request.ClientIds == null || !request.ClientIds.Any())
                return BadRequest(new ErrorResponseDto { Message = "Nenhum cliente fornecido" });

            var updatedClients = new List<ClientResponseDtos>();
            var errors = new List<string>();

            var clients = await _context.Clientes
                .Where(c => request.ClientIds.Contains(c.Id))
                .ToListAsync();

            foreach (var client in clients)
            {
                try
                {
                    bool? isBlocked = request.Bloqueado switch
                    {
                        "S" => true,
                        "N" => false,
                        _ => null
                    };

                    DateTime? expirationDate = null;
                    if (!string.IsNullOrWhiteSpace(request.DataValidade))
                    {
                        if (DateTime.TryParse(request.DataValidade, out var parsedDate))
                            expirationDate = parsedDate;
                    }

                    client.Update(isBlocked, expirationDate, request.Pendente);
                    updatedClients.Add(MapToDto(client));
                }
                catch (Exception ex)
                {
                    errors.Add($"Cliente {client.Id}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new BulkUpdateResponseDto
            {
                UpdatedClients = updatedClients,
                UpdatedCount = updatedClients.Count,
                Errors = errors
            });
        }

        [Authorize]
        [HttpPost("{id}/block")]
        [ProducesResponseType(typeof(ClientResponseDtos), StatusCodes.Status200OK)]
        public async Task<ActionResult<ClientResponseDtos>> BlockClient(
            int id, 
            [FromQuery] string? reason = null)
        {
            var client = await _context.Clientes.FindAsync(id);

            if (client == null)
                return NotFound(new ErrorResponseDto { Message = "Cliente não encontrado" });

            var blockReason = reason?.ToLower() switch
            {
                "expired" => ClientMessage.ExpiredSubscription,
                "payment" => ClientMessage.PendingPayment,
                _ => ClientMessage.ManuallyBlocked
            };

            client.Block(blockReason);
            await _context.SaveChangesAsync();
            
            return Ok(MapToDto(client));
        }

        [Authorize]
        [HttpPost("{id}/renew")]
        [ProducesResponseType(typeof(ClientResponseDtos), StatusCodes.Status200OK)]
        public async Task<ActionResult<ClientResponseDtos>> RenewSubscription(
            int id, 
            [FromBody] UpdatesClientesRequestDTO request)
        {
            var client = await _context.Clientes.FindAsync(id);

            if (client == null)
                return NotFound(new ErrorResponseDto { Message = "Cliente não encontrado" });

            try
            {
                if (string.IsNullOrWhiteSpace(request.DataValidade))
                    return BadRequest(new ErrorResponseDto { Message = "Data de validade é obrigatória" });

                if (!DateTime.TryParse(request.DataValidade, out var expirationDate))
                    return BadRequest(new ErrorResponseDto { Message = "Formato de data inválido" });

                client.Renew(expirationDate);
                await _context.SaveChangesAsync();
                
                return Ok(MapToDto(client));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Data de validade inválida",
                    Details = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("{id}/unblock")]
        [ProducesResponseType(typeof(ClientResponseDtos), StatusCodes.Status200OK)]
        public async Task<ActionResult<ClientResponseDtos>> UnblockClient(int id)
        {
            var client = await _context.Clientes.FindAsync(id);

            if (client == null)
                return NotFound(new ErrorResponseDto { Message = "Cliente não encontrado" });

            try
            {
                client.Unblock();
                await _context.SaveChangesAsync();
                
                return Ok(MapToDto(client));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = "Não é possível desbloquear cliente",
                    Details = ex.Message
                });
            }
        }

        private static ClientResponseDtos MapToDto(Client client)
        {
            return new ClientResponseDtos
            {
                Id = client.Id,
                Descricao = client.Descricao,
                Cnpjcpf = client.CNPJCPF,
                DataValidade = client.DataValidade.ToString("yyyy-MM-dd"),
                Bloqueado = client.Bloqueado ? "S" : "N",
                Pendente = client.IsPending,
                Mensagem = GetMessageText(client)
            };
        }

        private static string GetMessageText(Client client)
        {
            if (!string.IsNullOrEmpty(client.Mensagem))
                return client.Mensagem;

            if (client.DataValidade < DateTime.Now && !client.Bloqueado)
                return "Assinatura expirada";

            if (client.DataValidade < DateTime.Now && client.Bloqueado)
                return "Assinatura vencida e bloqueada";

            if (client.Bloqueado)
                return "Bloqueado por falta de pagamento";

            return "Assinatura ativa";
        }
    }
}